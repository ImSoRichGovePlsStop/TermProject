using System.Collections.Generic;
using UnityEngine;

public enum RoomType { None, Spawn, Battle, Boss, Shop, Merge, Heal, Upgrade }

[System.Serializable]
public class RoomNode
{
    public Vector2Int GridCoord;
    public RoomType Type;
    public Vector3 WorldPosition;
    public GameObject RoomObject;
    public List<RoomNode> Neighbors = new();
}

public class MapGenerator : MonoBehaviour
{
    [Header("Room Prefabs")]
    public GameObject spawnRoomPrefab;
    public GameObject battleRoomPrefab;
    public GameObject bossRoomPrefab;
    public GameObject shopRoomPrefab;
    public GameObject healRoomPrefab;
    public GameObject upgradeRoomPrefab;
    public GameObject mergeRoomPrefab;

    [Header("Interactable Prefabs")]
    public GameObject healStationPrefab;
    public GameObject shopStationPrefab;
    public GameObject upgradeStationPrefab;
    public GameObject mergeStationPrefab;

    [Header("Enemy / Loot")]
    public GameObject[] enemiesPrefab;
    public GameObject bossPrefab;
    public GameObject lootPrefab;
    public Material boundaryMaterial;

    [Header("Layout")]
    public int gridWidth = 7;
    public int gridHeight = 7;
    public float roomSpacing = 24f;
    public float triggerHeight = 3f;

    [Header("Generation")]
    [Range(3, 10)] public int minBattleRooms = 3;
    [Range(3, 10)] public int maxBattleRooms = 7;
    [Range(0f, 1f)] public float eventRoomChance = 0.35f;
    [Range(0f, 1f)] public float branchChance = 0.4f;

    [Header("Corridors")]
    public float corridorWidth = 2f;

    // N=+Z  E=+X  S=-Z  W=-X
    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    private RoomNode[,] _grid;

    void Start() => GenerateMap();

    // ── entry point ──────────────────────────────────────────────────────────

    void GenerateMap()
    {
        InitGrid();

        var spawnCoord = new Vector2Int(gridWidth / 2, gridHeight / 2);
        SetNode(spawnCoord, RoomType.Spawn);

        var mainPath = BuildMainPath(spawnCoord, Random.Range(minBattleRooms, maxBattleRooms + 1));
        var bossNode = PlaceBossAdjacent(mainPath[mainPath.Count - 1]);

        AddBranches(mainPath);
        AssignEventRooms(mainPath);
        BuildNeighborLinks();

        SpawnAllRooms();
        SpawnAllCorridors();
    }

    // ── grid init ────────────────────────────────────────────────────────────

    void InitGrid()
    {
        _grid = new RoomNode[gridWidth, gridHeight];
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
                _grid[x, y] = new RoomNode
                {
                    GridCoord = new Vector2Int(x, y),
                    Type = RoomType.None,
                    WorldPosition = new Vector3(x * roomSpacing, 0, y * roomSpacing)
                };
    }

    // ── path building ────────────────────────────────────────────────────────

    List<RoomNode> BuildMainPath(Vector2Int start, int count)
    {
        var path = new List<RoomNode>();
        var current = start;

        for (int i = 0; i < count; i++)
        {
            var next = PickFreeNeighbor(current);
            if (next == current) break;
            SetNode(next, RoomType.Battle);
            path.Add(_grid[next.x, next.y]);
            current = next;
        }
        return path;
    }

    RoomNode PlaceBossAdjacent(RoomNode last)
    {
        var coord = PickFreeNeighbor(last.GridCoord);
        if (coord == last.GridCoord)
        {
            last.Type = RoomType.Boss;
            return last;
        }
        SetNode(coord, RoomType.Boss);
        return _grid[coord.x, coord.y];
    }

    void AddBranches(List<RoomNode> mainPath)
    {
        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;
            var coord = PickFreeNeighbor(mainPath[i].GridCoord);
            if (coord == mainPath[i].GridCoord) continue;
            SetNode(coord, RoomType.Battle);
        }
    }

    void AssignEventRooms(List<RoomNode> mainPath)
    {
        var eventTypes = new[] { RoomType.Shop, RoomType.Merge, RoomType.Heal, RoomType.Upgrade };
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var node = _grid[x, y];
                if (node.Type != RoomType.Battle) continue;
                if (mainPath.Contains(node)) continue;
                if (Random.value < eventRoomChance)
                    node.Type = eventTypes[Random.Range(0, eventTypes.Length)];
            }
    }

    // ── neighbor links ───────────────────────────────────────────────────────

    void BuildNeighborLinks()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                if (_grid[x, y].Type == RoomType.None) continue;
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, ny = y + d.y;
                    if (!InBounds(nx, ny) || _grid[nx, ny].Type == RoomType.None) continue;
                    var a = _grid[x, y];
                    var b = _grid[nx, ny];
                    if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
                    if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
                }
            }
    }

    // ── spawn rooms ──────────────────────────────────────────────────────────

    void SpawnAllRooms()
    {
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var node = _grid[x, y];
                if (node.Type == RoomType.None) continue;
                node.RoomObject = SpawnRoom(node);
            }
    }

    GameObject SpawnRoom(RoomNode node)
    {
        switch (node.Type)
        {
            case RoomType.Spawn: return SpawnSpawnRoom(node);
            case RoomType.Battle: return SpawnBattleRoom(node, bossRoom: false);
            case RoomType.Boss: return SpawnBattleRoom(node, bossRoom: true);
            case RoomType.Heal: return SpawnEventRoom(node, healRoomPrefab, AddHealRoom);
            case RoomType.Shop: return SpawnEventRoom(node, shopRoomPrefab, AddShopRoom);
            case RoomType.Upgrade: return SpawnEventRoom(node, upgradeRoomPrefab, AddUpgradeRoom);
            case RoomType.Merge: return SpawnEventRoom(node, mergeRoomPrefab, AddMergeRoom);
            default: return null;
        }
    }

    GameObject SpawnSpawnRoom(RoomNode node)
    {
        var obj = Instantiate(spawnRoomPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "SpawnRoom";
        obj.AddComponent<SpawnRoom>();
        return obj;
    }

    GameObject SpawnBattleRoom(RoomNode node, bool bossRoom)
    {
        var prefab = bossRoom ? bossRoomPrefab : battleRoomPrefab;
        var obj = Instantiate(prefab, node.WorldPosition, Quaternion.identity);
        obj.name = bossRoom ? "BossRoom" : "BattleRoom";

        var preset = obj.GetComponent<RoomPreset>();
        Vector2 size = preset != null ? preset.roomSize : new Vector2(10f, 10f);

        var room = obj.AddComponent<BattleRoom>();
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyCount = bossRoom ? 1 : Random.Range(1, 4);
        room.enemyPrefabs = bossRoom
            ? new[] { bossPrefab }
            : new[] { enemiesPrefab[Random.Range(0, enemiesPrefab.Length)] };

        Vector3 roomVol = new Vector3(size.x, triggerHeight, size.y);
        room.SetRoomSize(roomVol);

        var trigger = obj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = roomVol;
        trigger.center = new Vector3(0, triggerHeight / 2f, 0);

        return obj;
    }

    // Generic event room spawner — instantiates prefab, then calls addComponent
    // to attach and init the correct room script
    GameObject SpawnEventRoom(RoomNode node, GameObject prefab,
                              System.Action<GameObject, Transform> addComponent)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"[MapGenerator] Missing prefab for {node.Type}");
            return null;
        }

        var obj = Instantiate(prefab, node.WorldPosition, Quaternion.identity);
        obj.name = node.Type.ToString() + "Room";
        var preset = obj.GetComponent<RoomPreset>();
        var spawnPt = preset != null ? preset.interactableSpawnPoint : obj.transform;

        addComponent(obj, spawnPt);
        return obj;
    }

    void AddHealRoom(GameObject obj, Transform spawnPt)
    {
        var r = obj.AddComponent<HealRoom>();
        r.healStationPrefab = healStationPrefab;
        r.Init(spawnPt);
    }

    void AddShopRoom(GameObject obj, Transform spawnPt)
    {
        var r = obj.AddComponent<ShopRoom>();
        r.shopStationPrefab = shopStationPrefab;
        r.Init(spawnPt);
    }

    void AddUpgradeRoom(GameObject obj, Transform spawnPt)
    {
        var r = obj.AddComponent<UpgradeRoom>();
        r.upgradeStationPrefab = upgradeStationPrefab;
        r.Init(spawnPt);
    }

    void AddMergeRoom(GameObject obj, Transform spawnPt)
    {
        var r = obj.AddComponent<MergeRoom>();
        r.mergeStationPrefab = mergeStationPrefab;
        r.Init(spawnPt);
    }

    // ── corridors ────────────────────────────────────────────────────────────

    void SpawnAllCorridors()
    {
        var spawned = new HashSet<(int, int)>();

        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var node = _grid[x, y];
                if (node.Type == RoomType.None) continue;

                foreach (var neighbor in node.Neighbors)
                {
                    int k1 = node.GridCoord.x * 1000 + node.GridCoord.y;
                    int k2 = neighbor.GridCoord.x * 1000 + neighbor.GridCoord.y;
                    var pair = k1 < k2 ? (k1, k2) : (k2, k1);
                    if (spawned.Contains(pair)) continue;
                    spawned.Add(pair);
                    SpawnCorridor(node, neighbor);
                }
            }
    }

    void SpawnCorridor(RoomNode a, RoomNode b)
    {
        Vector3 posA = a.WorldPosition;
        Vector3 posB = b.WorldPosition;
        Vector3 center = (posA + posB) / 2f;
        Vector3 diff = posB - posA;
        bool isNS = Mathf.Abs(diff.z) > Mathf.Abs(diff.x);

        // gap = distance between the two room edges
        float roomHalfExtent = 5f; // half of default room size — update to match your preset
        float gap = diff.magnitude - roomHalfExtent * 2f;
        if (gap <= 0f) return;

        var corridor = new GameObject($"Corridor_{a.GridCoord}->{b.GridCoord}");
        corridor.transform.position = center;

        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(corridor.transform);
        floor.transform.localPosition = new Vector3(0, 0, 0);
        floor.transform.localScale = isNS
            ? new Vector3(corridorWidth, 0f, gap)
            : new Vector3(gap, 0f, corridorWidth);

        // walls

    }



    // ── helpers ──────────────────────────────────────────────────────────────

    void SetNode(Vector2Int c, RoomType t) => _grid[c.x, c.y].Type = t;

    bool InBounds(int x, int y) => x >= 0 && x < gridWidth && y >= 0 && y < gridHeight;

    Vector2Int PickFreeNeighbor(Vector2Int current)
    {
        var free = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            int nx = current.x + d.x, ny = current.y + d.y;
            if (InBounds(nx, ny) && _grid[nx, ny].Type == RoomType.None)
                free.Add(new Vector2Int(nx, ny));
        }
        return free.Count > 0 ? free[Random.Range(0, free.Count)] : current;
    }

    void OnDrawGizmos()
    {
        if (_grid == null) return;
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                var node = _grid[x, y];
                if (node.Type == RoomType.None) continue;
                Gizmos.color = node.Type switch
                {
                    RoomType.Spawn => Color.green,
                    RoomType.Battle => Color.red,
                    RoomType.Boss => Color.magenta,
                    RoomType.Shop => Color.yellow,
                    RoomType.Merge => Color.cyan,
                    RoomType.Heal => Color.white,
                    RoomType.Upgrade => new Color(1f, 0.5f, 0f),
                    _ => Color.grey
                };
                Gizmos.DrawWireCube(node.WorldPosition + Vector3.up, Vector3.one * 2f);
            }
    }
}