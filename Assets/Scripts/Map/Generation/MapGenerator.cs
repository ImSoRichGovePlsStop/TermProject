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

    [Header("Portals")]
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

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
    [Range(1, 5)] public int minBossDistance = 3;

    [Header("Corridors")]
    public float corridorWidth = 2f;

    // N=+Z  E=+X  S=-Z  W=-X
    private static readonly Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left
    };

    private RoomNode[,] _grid;
    private Vector2Int _spawnCoord;
    private HashSet<(Vector2Int, Vector2Int)> _connections = new();

    void Start() => GenerateMap();

    // ── entry point ──────────────────────────────────────────────────────────

    void GenerateMap()
    {
        _connections.Clear();
        InitGrid();

        _spawnCoord = new Vector2Int(gridWidth / 2, gridHeight / 2);
        SetNode(_spawnCoord, RoomType.Spawn);

        var mainPath = BuildMainPath(_spawnCoord, Random.Range(minBattleRooms, maxBattleRooms + 1));
        if (mainPath.Count == 0) { Debug.LogWarning("[MapGen] Main path empty!"); return; }

        PlaceBossAdjacent(mainPath[mainPath.Count - 1]);
        AddBranches(mainPath);
        AssignEventRooms(mainPath);

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
            AddConnection(current, next);
            current = next;
        }
        return path;
    }

    void PlaceBossAdjacent(RoomNode last)
    {
        var candidates = new List<Vector2Int>();
        foreach (var d in Dirs)
        {
            int nx = last.GridCoord.x + d.x;
            int ny = last.GridCoord.y + d.y;
            if (!InBounds(nx, ny)) continue;
            if (_grid[nx, ny].Type != RoomType.None) continue;
            float dist = Vector2Int.Distance(new Vector2Int(nx, ny), _spawnCoord);
            if (dist < minBossDistance) continue;
            candidates.Add(new Vector2Int(nx, ny));
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[MapGen] No valid boss position far enough from spawn — placing on last battle node.");
            last.Type = RoomType.Boss;
            return;
        }

        var coord = candidates[Random.Range(0, candidates.Count)];
        SetNode(coord, RoomType.Boss);
        AddConnection(last.GridCoord, coord);
    }

    void AddBranches(List<RoomNode> mainPath)
    {
        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;
            var coord = PickFreeNeighbor(mainPath[i].GridCoord);
            if (coord == mainPath[i].GridCoord) continue;
            SetNode(coord, RoomType.Battle);
            AddConnection(mainPath[i].GridCoord, coord);
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
                {
                    node.Type = eventTypes[Random.Range(0, eventTypes.Length)];
                    RunManager.Instance?.OnEventRoomEntered();
                }
            }
    }

    // ── connections ──────────────────────────────────────────────────────────

    void AddConnection(Vector2Int a, Vector2Int b)
    {
        var pair = a.x * 1000 + a.y < b.x * 1000 + b.y ? (a, b) : (b, a);
        _connections.Add(pair);

        var nodeA = _grid[a.x, a.y];
        var nodeB = _grid[b.x, b.y];
        if (!nodeA.Neighbors.Contains(nodeB)) nodeA.Neighbors.Add(nodeB);
        if (!nodeB.Neighbors.Contains(nodeA)) nodeB.Neighbors.Add(nodeA);
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
            case RoomType.Battle: return SpawnBattleRoom(node);
            case RoomType.Boss: return SpawnBossRoom(node);
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

    GameObject SpawnBattleRoom(RoomNode node)
    {
        var obj = Instantiate(battleRoomPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "BattleRoom";

        var preset = obj.GetComponent<RoomPreset>();
        Vector2 size = preset != null ? preset.roomSize : new Vector2(10f, 10f);

        var room = obj.AddComponent<BattleRoom>();
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyCount = Random.Range(1, 4);
        room.enemyPrefabs = new[] { enemiesPrefab[Random.Range(0, enemiesPrefab.Length)] };

        Vector3 roomVol = new Vector3(size.x, triggerHeight, size.y);
        room.SetRoomSize(roomVol);

        var trigger = obj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = roomVol;
        trigger.center = new Vector3(0, triggerHeight / 2f, 0);

        return obj;
    }

    GameObject SpawnBossRoom(RoomNode node)
    {
        var obj = Instantiate(bossRoomPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "BossRoom";

        var preset = obj.GetComponent<RoomPreset>();
        Vector2 size = preset != null ? preset.roomSize : new Vector2(14f, 14f);

        var room = obj.AddComponent<BossRoom>();
        room.bossPrefab = bossPrefab;
        room.lootPrefab = lootPrefab;
        room.portalPrefab = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;

        Vector3 roomVol = new Vector3(size.x, triggerHeight, size.y);
        room.SetRoomSize(roomVol);

        var trigger = obj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = roomVol;
        trigger.center = new Vector3(0, triggerHeight / 2f, 0);

        return obj;
    }

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
        var spawnPt = preset != null && preset.interactableSpawnPoint != null
            ? preset.interactableSpawnPoint
            : obj.transform;

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
        foreach (var (a, b) in _connections)
            SpawnCorridor(_grid[a.x, a.y], _grid[b.x, b.y]);
    }

    void SpawnCorridor(RoomNode a, RoomNode b)
    {
        Vector3 posA = a.WorldPosition;
        Vector3 posB = b.WorldPosition;
        Vector3 center = (posA + posB) / 2f;
        Vector3 diff = posB - posA;
        bool isNS = Mathf.Abs(diff.z) > Mathf.Abs(diff.x);

        float halfA = GetRoomHalfExtent(a, isNS);
        float halfB = GetRoomHalfExtent(b, isNS);
        float gap = diff.magnitude - halfA - halfB;
        if (gap <= 0f) return;

        var corridor = new GameObject($"Corridor_{a.GridCoord}->{b.GridCoord}");
        corridor.transform.position = center;

        // floor
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(corridor.transform);
        floor.transform.localPosition = Vector3.zero;
        floor.transform.localScale = isNS
            ? new Vector3(corridorWidth, 0.1f, gap)
            : new Vector3(gap, 0.1f, corridorWidth);
        Destroy(floor.GetComponent<Collider>());
        if (boundaryMaterial != null)
            floor.GetComponent<Renderer>().material = boundaryMaterial;

        // walls
        SpawnCorridorWall(corridor.transform, isNS, gap, -1);
        SpawnCorridorWall(corridor.transform, isNS, gap, 1);
    }

    void SpawnCorridorWall(Transform parent, bool isNS, float length, int side)
    {
        float halfCW = corridorWidth / 2f;

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.SetParent(parent);
        wall.transform.localPosition = isNS
            ? new Vector3(side * halfCW, triggerHeight / 2f, 0f)
            : new Vector3(0f, triggerHeight / 2f, side * halfCW);
        wall.transform.localScale = isNS
            ? new Vector3(0.1f, triggerHeight, length)
            : new Vector3(length, triggerHeight, 0.1f);

        if (boundaryMaterial != null)
            wall.GetComponent<Renderer>().material = boundaryMaterial;
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    float GetRoomHalfExtent(RoomNode node, bool isNS)
    {
        if (node.RoomObject == null) return 5f;
        var preset = node.RoomObject.GetComponent<RoomPreset>();
        if (preset == null) return 5f;
        return isNS ? preset.roomSize.y / 2f : preset.roomSize.x / 2f;
    }

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