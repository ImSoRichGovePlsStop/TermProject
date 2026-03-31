using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public enum RoomType { None, Spawn, Battle, Boss, Shop, Merge, Heal, Upgrade }

public static class Cell
{
    public const byte Empty = 0;
    public const byte Corridor = 1;
    public const byte Room = 2;
}

[System.Serializable]
public class RoomNode
{
    public RoomType Type;
    public Vector2Int MatrixOrigin;   // top-left cell in the matrix
    public Vector2Int MatrixCenter;   // center cell (always exact for odd sizes)
    public Vector2Int Size;           // stamped size (always odd)
    public Vector3 WorldPosition;  // world-space center (1 cell = 1 world unit)
    public GameObject RoomObject;
    public List<RoomNode> Neighbors = new();
}

public class MapGenerator : MonoBehaviour
{
    // ── Prefab arrays ────────────────────────────────────────────────────────
    [Header("Room Prefabs (randomly selected per room)")]
    public GameObject[] spawnRoomPrefabs;
    public GameObject[] battleRoomPrefabs;
    public GameObject[] bossRoomPrefabs;
    public GameObject[] shopRoomPrefabs;
    public GameObject[] healRoomPrefabs;
    public GameObject[] upgradeRoomPrefabs;
    public GameObject[] mergeRoomPrefabs;

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

    [Header("Materials")]
    public Material corridorMat;
    public Material wallMat;
    public Material boundaryMaterial;

    // ── Generation parameters ────────────────────────────────────────────────
    [Header("Matrix")]
    public int matrixSize = 150;
    public int minRoomSize = 5;
    public int maxRoomSize = 15;

    [Header("Generation")]
    [Range(3, 10)] public int minBattleRooms = 3;
    [Range(3, 10)] public int maxBattleRooms = 7;
    [Range(0f, 1f)] public float branchChance = 0.4f;
    public int maxPlacementAttempts = 50;

    [Header("Corridors")]
    public int corridorWidth = 2;

    [Header("Walls")]
    public float wallHeight = 2f;
    public float wallThickness = 0.1f;

    [Header("Trigger")]
    public float triggerHeight = 3f;

    // ── Internal state ───────────────────────────────────────────────────────
    private byte[,] _matrix;
    private List<RoomNode> _rooms = new();
    private RoomNode _spawnRoom;
    private HashSet<(Vector2Int, Vector2Int)> _connections = new();

    // ── Entry point ──────────────────────────────────────────────────────────
    void Start() => GenerateMap();

    void GenerateMap()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _rooms.Clear();
        _connections.Clear();

        _spawnRoom = PlaceRoom(RoomType.Spawn, matrixSize / 2, matrixSize / 2, forced: true);
        if (_spawnRoom == null) { Debug.LogError("[MapGen] Failed to place spawn room."); return; }

        var mainPath = BuildMainPath(_spawnRoom, Random.Range(minBattleRooms, maxBattleRooms + 1));
        if (mainPath.Count == 0) { Debug.LogWarning("[MapGen] Main path empty."); return; }

        PlaceBoss(mainPath[mainPath.Count - 1]);
        AddBranches(mainPath);
        CarveAllCorridors();
        SpawnAllRooms();
        SpawnCorridorGeometry();
        SpawnAllWalls();

        // ── Minimap (commented out for testing) ──
        // var minimap = FindFirstObjectByType<MinimapManager>();
        // minimap?.BuildMinimapFromMatrix(_matrix, matrixSize, _rooms);
    }

    // ── Room placement ───────────────────────────────────────────────────────

    RoomNode PlaceRoom(RoomType type, int hintX, int hintZ, bool forced = false)
    {
        GameObject[] arr = PrefabArrayFor(type);

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            var prefab = arr[Random.Range(0, arr.Length)];
            var preset = prefab.GetComponent<RoomPreset>();

            int sx = preset != null ? OddClamp(Mathf.RoundToInt(preset.roomSize.x)) : RandomOdd();
            int sz = preset != null ? OddClamp(Mathf.RoundToInt(preset.roomSize.y)) : RandomOdd();

            int scatter = (forced && attempt == 0) ? 0 : Random.Range(5, 30);
            int cx = hintX + Random.Range(-scatter, scatter + 1);
            int cz = hintZ + Random.Range(-scatter, scatter + 1);

            // Origin from center, using (size-1)/2 so center cell is exact integer
            int ox = cx - (sx - 1) / 2;
            int oz = cz - (sz - 1) / 2;

            if (ox < 1 || oz < 1 || ox + sx >= matrixSize - 1 || oz + sz >= matrixSize - 1)
                continue;

            if (!forced && !AreaFree(ox - 1, oz - 1, sx + 2, sz + 2))
                continue;

            StampRoom(ox, oz, sx, sz);

            int mcx = ox + (sx - 1) / 2;
            int mcz = oz + (sz - 1) / 2;

            var node = new RoomNode
            {
                Type = type,
                MatrixOrigin = new Vector2Int(ox, oz),
                MatrixCenter = new Vector2Int(mcx, mcz),
                Size = new Vector2Int(sx, sz),
                WorldPosition = new Vector3(mcx + 0.5f, 0f, mcz + 0.5f)
            };
            _rooms.Add(node);
            return node;
        }

        Debug.LogWarning($"[MapGen] Could not place {type} after {maxPlacementAttempts} attempts.");
        return null;
    }

    bool AreaFree(int ox, int oz, int sx, int sz)
    {
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                if (x < 0 || z < 0 || x >= matrixSize || z >= matrixSize) return false;
                if (_matrix[x, z] != Cell.Empty) return false;
            }
        return true;
    }

    void StampRoom(int ox, int oz, int sx, int sz)
    {
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
                _matrix[x, z] = Cell.Room;
    }

    // ── Path / branch building ───────────────────────────────────────────────

    List<RoomNode> BuildMainPath(RoomNode start, int count)
    {
        var path = new List<RoomNode>();
        var current = start;

        for (int i = 0; i < count; i++)
        {
            var hint = Clamped(current.MatrixCenter + RandomCardinalOffset(15, 25));
            var next = PlaceRoom(RoomType.Battle, hint.x, hint.y);
            if (next == null) break;
            AddConnection(current, next);
            path.Add(next);
            current = next;
        }
        return path;
    }

    void PlaceBoss(RoomNode last)
    {
        Vector2Int away = last.MatrixCenter - _spawnRoom.MatrixCenter;
        Vector2Int hint = Clamped(last.MatrixCenter +
            (away.sqrMagnitude > 0
                ? Vector2Int.RoundToInt(((Vector2)away).normalized * 20f)
                : RandomCardinalOffset(15, 25)));

        var boss = PlaceRoom(RoomType.Boss, hint.x, hint.y);
        if (boss == null) { Debug.LogWarning("[MapGen] Could not place boss room."); return; }
        AddConnection(last, boss);
    }

    void AddBranches(List<RoomNode> mainPath)
    {
        var eventTypes = new[] { RoomType.Shop, RoomType.Merge, RoomType.Heal, RoomType.Upgrade };

        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;

            RoomType type = Random.value < 0.25f
                ? eventTypes[Random.Range(0, eventTypes.Length)]
                : RoomType.Battle;

            var hint = Clamped(mainPath[i].MatrixCenter + RandomCardinalOffset(10, 20));
            var branch = PlaceRoom(type, hint.x, hint.y);
            if (branch == null) continue;
            AddConnection(mainPath[i], branch);

            if (Random.value < 0.3f)
            {
                var hint2 = Clamped(branch.MatrixCenter + RandomCardinalOffset(10, 20));
                var branch2 = PlaceRoom(eventTypes[Random.Range(0, eventTypes.Length)], hint2.x, hint2.y);
                if (branch2 != null) AddConnection(branch, branch2);
            }
        }
    }

    // ── Connections ──────────────────────────────────────────────────────────

    void AddConnection(RoomNode a, RoomNode b)
    {
        _connections.Add(MakeKey(a.MatrixCenter, b.MatrixCenter));
        if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
        if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
    }

    (Vector2Int, Vector2Int) MakeKey(Vector2Int a, Vector2Int b) =>
        a.x * 10000 + a.y < b.x * 10000 + b.y ? (a, b) : (b, a);

    // ── Corridor carving ─────────────────────────────────────────────────────

    void CarveAllCorridors()
    {
        foreach (var (a, b) in _connections)
        {
            var nodeA = _rooms.Find(r => r.MatrixCenter == a);
            var nodeB = _rooms.Find(r => r.MatrixCenter == b);
            if (nodeA == null || nodeB == null) continue;
            CarveL(nodeA.MatrixCenter, nodeB.MatrixCenter);
        }
    }

    // L-shaped carve from exitA to exitB, both of which sit on room edges.
    // The bend happens in open space between the two rooms.
    void CarveL(Vector2Int a, Vector2Int b)
    {
        Vector2Int corner = Random.value > 0.5f
            ? new Vector2Int(b.x, a.y)
            : new Vector2Int(a.x, b.y);

        CarveSegment(a, corner);
        CarveSegment(corner, b);
    }

    void CarveSegment(Vector2Int from, Vector2Int to)
    {
        bool horizontal = from.y == to.y;

        int x0 = Mathf.Min(from.x, to.x);
        int x1 = Mathf.Max(from.x, to.x);
        int z0 = Mathf.Min(from.y, to.y);
        int z1 = Mathf.Max(from.y, to.y);

        int half = corridorWidth / 2;

        for (int x = x0; x <= x1; x++)
        {
            for (int z = z0; z <= z1; z++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int cx, cz;
                    if (horizontal)
                    {
                        // Travelling along X → thicken on Z
                        cx = x;
                        cz = z - half + w;
                    }
                    else
                    {
                        // Travelling along Z → thicken on X
                        cx = x - half + w;
                        cz = z;
                    }

                    if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                    if (_matrix[cx, cz] == Cell.Empty)
                        _matrix[cx, cz] = Cell.Corridor;
                }
            }
        }
    }

    // ── Spawn rooms ──────────────────────────────────────────────────────────

    void SpawnAllRooms()
    {
        foreach (var node in _rooms)
            node.RoomObject = SpawnRoom(node);
    }

    GameObject SpawnRoom(RoomNode node) => node.Type switch
    {
        RoomType.Spawn => SpawnSpawnRoom(node),
        RoomType.Battle => SpawnBattleRoom(node),
        RoomType.Boss => SpawnBossRoom(node),
        RoomType.Heal => SpawnEventRoom(node, healRoomPrefabs, AddHealRoom),
        RoomType.Shop => SpawnEventRoom(node, shopRoomPrefabs, AddShopRoom),
        RoomType.Upgrade => SpawnEventRoom(node, upgradeRoomPrefabs, AddUpgradeRoom),
        RoomType.Merge => SpawnEventRoom(node, mergeRoomPrefabs, AddMergeRoom),
        _ => null
    };

    GameObject SpawnSpawnRoom(RoomNode node)
    {
        var obj = Instantiate(Pick(spawnRoomPrefabs), node.WorldPosition, Quaternion.identity);
        obj.name = "SpawnRoom";
        obj.AddComponent<SpawnRoom>();
        return obj;
    }

    GameObject SpawnBattleRoom(RoomNode node)
    {
        var obj = Instantiate(Pick(battleRoomPrefabs), node.WorldPosition, Quaternion.identity);
        obj.name = "BattleRoom";
        var preset = obj.GetComponent<RoomPreset>();
        Vector2 sz = preset != null ? preset.roomSize : new Vector2(node.Size.x, node.Size.y);

        var room = obj.AddComponent<BattleRoom>();
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyCount = Random.Range(1, 4) + (RunManager.Instance?.CurrentFloor ?? 1);
        room.enemyPrefabs = new[] { enemiesPrefab[Random.Range(0, enemiesPrefab.Length)] };

        Vector3 vol = new Vector3(sz.x, triggerHeight, sz.y);
        room.SetRoomSize(vol);
        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = vol;
        col.center = new Vector3(0, triggerHeight / 2f, 0);
        return obj;
    }

    GameObject SpawnBossRoom(RoomNode node)
    {
        var obj = Instantiate(Pick(bossRoomPrefabs), node.WorldPosition, Quaternion.identity);
        obj.name = "BossRoom";
        var preset = obj.GetComponent<RoomPreset>();
        Vector2 sz = preset != null ? preset.roomSize : new Vector2(node.Size.x, node.Size.y);

        var room = obj.AddComponent<BossRoom>();
        room.bossPrefab = bossPrefab;
        room.lootPrefab = lootPrefab;
        room.portalPrefab = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;

        Vector3 vol = new Vector3(sz.x, triggerHeight, sz.y);
        room.SetRoomSize(vol);
        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = vol;
        col.center = new Vector3(0, triggerHeight / 2f, 0);
        return obj;
    }

    GameObject SpawnEventRoom(RoomNode node, GameObject[] arr,
                              System.Action<GameObject, Transform> setup)
    {
        if (arr == null || arr.Length == 0)
        {
            Debug.LogWarning($"[MapGen] Missing prefabs for {node.Type}");
            return null;
        }
        var obj = Instantiate(Pick(arr), node.WorldPosition, Quaternion.identity);
        obj.name = node.Type + "Room";
        var preset = obj.GetComponent<RoomPreset>();
        var pt = preset?.interactableSpawnPoint != null
            ? preset.interactableSpawnPoint : obj.transform;
        setup(obj, pt);
        return obj;
    }

    void AddHealRoom(GameObject o, Transform p) { var r = o.AddComponent<HealRoom>(); r.healStationPrefab = healStationPrefab; r.Init(p); }
    void AddShopRoom(GameObject o, Transform p) { var r = o.AddComponent<ShopRoom>(); r.shopStationPrefab = shopStationPrefab; r.Init(p); }
    void AddUpgradeRoom(GameObject o, Transform p) { var r = o.AddComponent<UpgradeRoom>(); r.upgradeStationPrefab = upgradeStationPrefab; r.Init(p); }
    void AddMergeRoom(GameObject o, Transform p) { var r = o.AddComponent<MergeRoom>(); r.mergeStationPrefab = mergeStationPrefab; r.Init(p); }

    // ── Corridor geometry (1 flat quad per corridor cell) ────────────────────

    void SpawnCorridorGeometry()
    {
        var parent = new GameObject("Corridors").transform;
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_matrix[x, z] == Cell.Corridor)
                    SpawnFloorQuad(parent, x, z);
    }

    void SpawnFloorQuad(Transform parent, int x, int z)
    {
        var go = new GameObject($"C_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);

        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[]
        {
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, -0.5f),
        });
        poly.extrude = 0.01f;
        poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh();
        pb.Refresh();

        if (corridorMat != null)
            pb.GetComponent<Renderer>().material = corridorMat;
    }

    // ── Matrix-driven walls ──────────────────────────────────────────────────

    void SpawnAllWalls()
    {
        var parent = new GameObject("Walls").transform;
        var dirs = new[]
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1),
        };

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                if (_matrix[x, z] == Cell.Empty) continue;
                foreach (var d in dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    bool empty = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize
                                 || _matrix[nx, nz] == Cell.Empty;
                    if (empty) SpawnWallQuad(parent, x, z, d);
                }
            }
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing)
    {
        Vector3 cellCenter = new Vector3(x + 0.5f, 0f, z + 0.5f);
        Vector3 faceOffset = new Vector3(facing.x * 0.5f, wallHeight / 2f, facing.y * 0.5f);
        Quaternion rot = Quaternion.LookRotation(new Vector3(-facing.x, 0, -facing.y));

        var go = new GameObject($"W_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = cellCenter + faceOffset;
        go.transform.rotation = rot;

        float hh = wallHeight / 2f;
        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[]
        {
            new Vector3(-0.5f, -hh, 0f),
            new Vector3( 0.5f, -hh, 0f),
            new Vector3( 0.5f,  hh, 0f),
            new Vector3(-0.5f,  hh, 0f),
        });
        poly.extrude = wallThickness;
        poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh();
        pb.Refresh();

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        if (wallMat != null)
            pb.GetComponent<Renderer>().material = wallMat;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    GameObject[] PrefabArrayFor(RoomType t) => t switch
    {
        RoomType.Spawn => spawnRoomPrefabs,
        RoomType.Battle => battleRoomPrefabs,
        RoomType.Boss => bossRoomPrefabs,
        RoomType.Shop => shopRoomPrefabs,
        RoomType.Heal => healRoomPrefabs,
        RoomType.Upgrade => upgradeRoomPrefabs,
        RoomType.Merge => mergeRoomPrefabs,
        _ => battleRoomPrefabs
    };

    GameObject Pick(GameObject[] arr) => arr[Random.Range(0, arr.Length)];

    int OddClamp(int v)
    {
        v = Mathf.Clamp(v, minRoomSize, maxRoomSize);
        return v % 2 == 0 ? v - 1 : v;
    }

    int RandomOdd()
    {
        int v = Random.Range(minRoomSize, maxRoomSize + 1);
        return v % 2 == 0 ? v - 1 : v;
    }

    Vector2Int RandomCardinalOffset(int minD, int maxD)
    {
        int dist = Random.Range(minD, maxD + 1);
        return Random.Range(0, 4) switch
        {
            0 => new Vector2Int(dist, 0),
            1 => new Vector2Int(-dist, 0),
            2 => new Vector2Int(0, dist),
            _ => new Vector2Int(0, -dist),
        };
    }

    Vector2Int Clamped(Vector2Int v) => new Vector2Int(
        Mathf.Clamp(v.x, 10, matrixSize - 10),
        Mathf.Clamp(v.y, 10, matrixSize - 10));

    // ── Gizmos ───────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (_rooms == null) return;
        foreach (var node in _rooms)
        {
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
            Gizmos.DrawWireCube(
                node.WorldPosition + Vector3.up,
                new Vector3(node.Size.x, 2f, node.Size.y));
        }


    }
}