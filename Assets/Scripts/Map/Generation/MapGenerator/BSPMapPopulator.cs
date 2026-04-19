using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BSPMapGeometry))]
public class BSPMapPopulator : MonoBehaviour
{
    [Header("Interactable Prefabs")]
    public GameObject healStationPrefab;
    public GameObject shopStationPrefab;
    public GameObject costedHealPrefab;
    public GameObject costedUpgradePrefab;
    public GameObject freeUpgradeStationPrefab;
    public GameObject mergeStationPrefab;

    [Header("Portals")]
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    [Header("Doors")]
    public GameObject battleDoorPrefab;

    [Header("Enemy / Loot")]
    [Tooltip("Average elite enemies per battle room added each floor. Floor 1 = 0, floor 2 = ~1 per room, etc.")]
    public float elitePerRoomPerFloor = 1f;
    public GameObject lootPrefab;
    public GameObject rareLootPrefab;

    [Header("Props")]
    public GameObject fountainPrefab;
    public GameObject cratePrefab;
    public GameObject statuePrefab;

    [Header("Prop Spawn Chances")]
    [Tooltip("Chance per eligible cell to spawn a crate (wall-adjacent, non-door).")]
    [Range(0f, 1f)] public float crateChance  = 0.04f;
    [Tooltip("Chance per eligible cell to spawn a statue.")]
    [Range(0f, 1f)] public float statueChance = 0.02f;

    [Header("Visual")]
    public Material boundaryMaterial;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    [Header("Enemy Spawn Inset")]
    [Tooltip("Number of cells to trim from each room edge when building spawn-cell lists. " +
             "Prevents enemies spawning on the room boundary where NavMesh bleeds into adjacent rooms.")]
    public int spawnCellInset = 2;

    [Header("Trigger")]
    public float triggerHeight = 3f;

    void Awake()
    {
        GetComponent<BSPMapGeometry>().OnMapReady += PopulateRooms;
    }

    void OnDestroy()
    {
        var geo = GetComponent<BSPMapGeometry>();
        if (geo != null) geo.OnMapReady -= PopulateRooms;
    }

    void PopulateRooms(System.Collections.Generic.IReadOnlyList<MapNode> nodes)
    {
        var battleRooms = new System.Collections.Generic.List<BattleRoom>();

        foreach (var node in nodes)
        {
            node.RoomObject = SpawnRoom(node);
            if (node.Type == RoomType.Battle && node.RoomObject != null)
            {
                var br = node.RoomObject.GetComponent<BattleRoom>();
                if (br != null) battleRooms.Add(br);
            }
        }

        DistributeEliteBudget(battleRooms);
        SpawnProps();
        StartCoroutine(BakeNavMeshNextFrame());
    }

    void DistributeEliteBudget(System.Collections.Generic.List<BattleRoom> rooms)
    {
        if (rooms.Count == 0) return;
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int total = Mathf.RoundToInt(Mathf.Max(0f, floor - 1) * rooms.Count * elitePerRoomPerFloor);

        foreach (var r in rooms) r.eliteBudget = 0;
        for (int i = 0; i < total; i++)
            rooms[Random.Range(0, rooms.Count)].eliteBudget++;
    }

    System.Collections.IEnumerator BakeNavMeshNextFrame()
    {
        yield return null;
        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
    }

    GameObject SpawnRoom(MapNode node) => node.Type switch
    {
        RoomType.Spawn    => SpawnSpawnRoom(node),
        RoomType.Battle   => SpawnBattleRoom(node),
        RoomType.Boss     => SpawnBossRoom(node),
        RoomType.Heal     => SpawnEventRoom(node, healStationPrefab, SetupHeal),
        RoomType.Shop     => SpawnEventRoom(node, shopStationPrefab, SetupShop),
        RoomType.RareLoot => SpawnEventRoom(node, rareLootPrefab, SetupRareLoot),
        RoomType.Merge    => SpawnEventRoom(node, mergeStationPrefab, SetupMerge),
        RoomType.Fountain => SpawnEventRoom(node, fountainPrefab, SetupFountain),
        _                 => null
    };

    GameObject SpawnSpawnRoom(MapNode node)
    {
        var obj = MakeRoomObject(node, "SpawnRoom");
        obj.AddComponent<SpawnRoom>().node = ToLegacy(node);
        return obj;
    }

    GameObject SpawnBattleRoom(MapNode node)
    {
        var obj  = MakeRoomObject(node, "BattleRoom");
        var vol  = Volume(node);
        var room = obj.AddComponent<BattleRoom>();
        room.node                   = ToLegacy(node);
        room.lootPrefab             = lootPrefab;
        room.upgradeStationPrefab   = freeUpgradeStationPrefab;
        room.boundaryMaterial       = boundaryMaterial;
        room.enemyEntries           = EnemyPoolManager.Instance
                                          ?.GetPoolForFloor(RunManager.Instance?.CurrentFloor ?? 1)
                                          ?.ToArray() ?? System.Array.Empty<EnemyEntry>();
        room.doorPrefab             = battleDoorPrefab;
        room.SetRoomSize(vol);
        room.CalculateTotalBudget(vol);
        room.spawnCells             = CollectSpawnCells(node);
        room.doorInfos              = CollectDoorInfos(node);
        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnBossRoom(MapNode node)
    {
        var obj  = MakeRoomObject(node, "BossRoom");
        var vol  = Volume(node);
        var room = obj.AddComponent<BossRoom>();
        room.node              = ToLegacy(node);
        room.lootPrefab        = lootPrefab;
        room.portalPrefab      = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial  = boundaryMaterial;
        room.doorPrefab        = battleDoorPrefab;
        room.spawnCells        = CollectSpawnCells(node);
        room.doorInfos         = CollectDoorInfos(node);
        room.enemyEntries      = EnemyPoolManager.Instance
                                     ?.GetPoolForFloor(RunManager.Instance?.CurrentFloor ?? 1)
                                     ?.ToArray() ?? System.Array.Empty<EnemyEntry>();
        room.SetRoomSize(vol);
        room.CalculateTotalBudget(vol);
        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnEventRoom(MapNode node, GameObject stationPrefab,
                              System.Action<GameObject, Transform, MapNode> setup)
    {
        if (stationPrefab == null) return null;
        var obj    = MakeRoomObject(node, node.Type + "Room");
        var preset = obj.GetComponent<RoomPreset>();
        var pt     = (preset?.interactableSpawnPoint != null) ? preset.interactableSpawnPoint : obj.transform;
        AddTrigger(obj, Volume(node));
        setup(obj, pt, node);
        return obj;
    }

    void SetupHeal(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<HealRoom>();
        r.node = ToLegacy(n);
        r.healStationPrefab = healStationPrefab;
        r.Init(p);
    }

    void SetupShop(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<ShopRoom>();
        r.node                = ToLegacy(n);
        r.shopStationPrefab   = shopStationPrefab;
        r.costedHealPrefab    = costedHealPrefab;
        r.costedUpgradePrefab = costedUpgradePrefab;
        r.Init(p);
    }

    void SetupRareLoot(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<RareLootRoom>();
        r.node       = ToLegacy(n);
        r.lootPrefab = rareLootPrefab;
        r.Init(p);
    }

    void SetupMerge(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<MergeRoom>();
        r.node               = ToLegacy(n);
        r.mergeStationPrefab = mergeStationPrefab;
        r.Init(p);
    }

    void SetupFountain(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<FountainRoom>();
        r.node           = ToLegacy(n);
        r.fountainPrefab = fountainPrefab;
        r.Init(p);
    }

    void SpawnProps()
    {
        var geo     = GetComponent<BSPMapGeometry>();
        var matrix  = geo.Matrix;
        var roomMap = geo.RoomMapPublic;
        int size    = geo.MatrixSize;

        var dirs = new[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };

        for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
            {
                if (matrix[x, z] != Cell.Room) continue;
                var owner = roomMap[x, z];
                if (owner == null) continue;

                
                if ( owner.Type != RoomType.Unmarked) continue;

                
                //bool nearWall = false;
                //foreach (var d in dirs)
                //{
                //    int nx = x + d.x, nz = z + d.y;
                //    bool oob = nx < 0 || nz < 0 || nx >= size || nz >= size;
                //    if (oob || matrix[nx, nz] != Cell.Room) { nearWall = true; break; }
                //}
    
                //if (!nearWall) continue;


                float roll = Random.value;
                if (statuePrefab != null && roll < statueChance)
                {
                    Instantiate(statuePrefab,
                        new Vector3(x + 0.5f, 0f, z + 0.5f),
                        Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f));
                }
                else if (cratePrefab != null && roll < statueChance + crateChance)
                {
                    Instantiate(cratePrefab,
                        new Vector3(x + 0.5f, 0f, z + 0.5f),
                        Quaternion.Euler(0f, Random.Range(0, 4) * 90f, 0f));
                }
            }
    }

    GameObject MakeRoomObject(MapNode node, string name)
    {
        var prefab = node.ChosenPrefab;
        GameObject obj;
        if (prefab != null)
            obj = Object.Instantiate(prefab, node.WorldCenter, Quaternion.identity);
        else
        {
            obj = new GameObject();
            obj.transform.position = node.WorldCenter;
        }
        obj.name = name;
        return obj;
    }

    System.Collections.Generic.List<BattleRoom.DoorInfo> CollectDoorInfos(MapNode node)
    {
        var result = new System.Collections.Generic.List<BattleRoom.DoorInfo>();
        var geo     = GetComponent<BSPMapGeometry>();
        if (geo == null) return result;

        var isDoor  = geo.IsDoor;
        var roomMap = geo.RoomMapPublic;
        int size    = geo.MatrixSize;

        var dirs = new[] { new Vector2Int(1,0), new Vector2Int(-1,0), new Vector2Int(0,1), new Vector2Int(0,-1) };

        // For each cell in this room, check every face that borders a different room.
        // A face is a door if EITHER the room's cell OR the neighbour's cell is marked isDoor —
        // PunchDoor stamps whichever side it happened to pick, so we must check both.
        var byDir = new System.Collections.Generic.Dictionary<Vector2Int,
                        System.Collections.Generic.List<Vector2Int>>();

        for (int x = node.MinX; x <= node.MaxX; x++)
        for (int z = node.MinZ; z <= node.MaxZ; z++)
        {
            if (roomMap[x, z] != node) continue;

            foreach (var d in dirs)
            {
                int nx = x + d.x, nz = z + d.y;
                if (nx < 0 || nz < 0 || nx >= size || nz >= size) continue;
                var nb = roomMap[nx, nz];
                if (nb == null || nb == node) continue;

                // Face borders a different room — is it a door on either side?
                if (!isDoor[x, z] && !isDoor[nx, nz]) continue;

                if (!byDir.ContainsKey(d)) byDir[d] = new();
                byDir[d].Add(new Vector2Int(x, z));
            }
        }

        // Group consecutive cells per direction into individual doorways
        foreach (var kvp in byDir)
        {
            var d     = kvp.Key;
            var cells = kvp.Value;
            bool faceX = d.x != 0;
            cells.Sort((a, b) => faceX ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            var run = new System.Collections.Generic.List<Vector2Int> { cells[0] };
            for (int i = 1; i < cells.Count; i++)
            {
                bool adj = faceX ? cells[i].y == cells[i-1].y + 1
                                 : cells[i].x == cells[i-1].x + 1;
                if (adj) { run.Add(cells[i]); }
                else     { result.Add(RunToDoorInfo(run, d)); run = new() { cells[i] }; }
            }
            result.Add(RunToDoorInfo(run, d));
        }

        return result;
    }

    static BattleRoom.DoorInfo RunToDoorInfo(System.Collections.Generic.List<Vector2Int> run, Vector2Int dir)
    {
        float sumX = 0f, sumZ = 0f;
        foreach (var c in run) { sumX += c.x; sumZ += c.y; }
        float cx = sumX / run.Count + 0.5f + dir.x * 0.5f;
        float cz = sumZ / run.Count + 0.5f + dir.y * 0.5f;
        return new BattleRoom.DoorInfo
        {
            worldPosition = new Vector3(cx, 0f, cz),
            rotation      = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y))
        };
    }

    System.Collections.Generic.List<Vector3> CollectSpawnCells(MapNode node)
    {
        var cells   = new System.Collections.Generic.List<Vector3>();
        var roomMap = GetComponent<BSPMapGeometry>()?.RoomMapPublic;
        if (roomMap == null) return cells;

        int inset = spawnCellInset;
        int x0 = node.MinX + inset, x1 = node.MaxX - inset;
        int z0 = node.MinZ + inset, z1 = node.MaxZ - inset;

        if (x0 > x1 || z0 > z1) { x0 = node.MinX; x1 = node.MaxX; z0 = node.MinZ; z1 = node.MaxZ; }

        for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                if (roomMap[x, z] == node)
                    cells.Add(new Vector3(x + 0.5f, 0f, z + 0.5f));
        return cells;
    }

    Vector3 Volume(MapNode n) => new Vector3(n.Width, triggerHeight, n.Depth);

    void AddTrigger(GameObject obj, Vector3 vol)
    {
        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = vol;
        col.center    = new Vector3(0, triggerHeight / 2f, 0);
    }

    static RoomNode ToLegacy(MapNode n)
    {
        if (n.LegacyNode != null) return n.LegacyNode;
        return new RoomNode
        {
            Type          = n.Type,
            MatrixOrigin  = new Vector2Int(n.MinX, n.MinZ),
            MatrixCenter  = new Vector2Int(n.CenterX, n.CenterZ),
            Size          = new Vector2Int(n.Width, n.Depth),
            WorldPosition = n.WorldCenter,
            ChosenPrefab  = n.ChosenPrefab,
            RoomObject    = n.RoomObject,
        };
    }
}
