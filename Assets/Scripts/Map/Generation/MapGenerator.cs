using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Unity.AI.Navigation;

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
    public Vector2Int MatrixOrigin;
    public Vector2Int MatrixCenter;
    public Vector2Int Size;
    public Vector3 WorldPosition;
    public GameObject ChosenPrefab;  
    public GameObject RoomObject;
    public List<RoomNode> Neighbors = new();
}

public class MapGenerator : MonoBehaviour
{
    
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
    [Tooltip("Normal enemies — floor 1 picks index 0, floor 4 picks last index")]
    public GameObject[] normalEnemyPrefabs;
    [Tooltip("One boss per floor — index 0 = floor 1, index 1 = floor 2, etc.")]
    public GameObject[] bossPrefabs;
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
    public float floorThickness = 0.01f;

    [Header("Trigger")]
    public float triggerHeight = 3f;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

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

        var minimap = FindFirstObjectByType<MinimapManager>();
        minimap?.BuildMinimapFromMatrix(_matrix, matrixSize, _rooms);

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
        else
            Debug.LogWarning("[MapGen] NavMeshSurface not assigned — skipping navmesh bake.");
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

            if (!forced && !AreaFree(ox - 3, oz - 3, sx + 6, sz + 6))
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
                WorldPosition = new Vector3(mcx + 0.5f, 0f, mcz + 0.5f),
                ChosenPrefab = prefab   // lock in the prefab used for size calculation
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
            // Place the first battle room close to spawn so the player is
            // guaranteed to enter it before reaching any other room.
            // Subsequent rooms use the normal wider offset.
            int minOffset = i == 0 ? 8 : 15;
            int maxOffset = i == 0 ? 12 : 25;

            var hint = Clamped(current.MatrixCenter + RandomCardinalOffset(minOffset, maxOffset));
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
        // Base weights — Heal most common, Shop second, Upgrade third, Merge least
        var eventWeights = new Dictionary<RoomType, float>
        {
            { RoomType.Heal,    4f },
            { RoomType.Shop,    3f },
            { RoomType.Upgrade, 2f },
            { RoomType.Merge,   1f },
        };

        // Boost weight of any event type that did not appear on the previous floor
        foreach (var key in new List<RoomType>(eventWeights.Keys))
            if (RunManager.Instance != null && RunManager.Instance.WasMissingLastFloor(key))
                eventWeights[key] *= 2f;

        // Track which event types have already been placed this map — no duplicates
        var usedEventTypes = new HashSet<RoomType>();

        int battleCount = mainPath.Count;

        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;

            bool canPlaceBattle = battleCount < maxBattleRooms;
            RoomType type;

            if (canPlaceBattle && Random.value >= 0.25f)
            {
                type = RoomType.Battle;
            }
            else
            {
                type = PickUnusedEventType(eventWeights, usedEventTypes);
                if (type == RoomType.None) type = RoomType.Battle;
            }

            var hint = Clamped(mainPath[i].MatrixCenter + RandomCardinalOffset(10, 20));
            var branch = PlaceRoom(type, hint.x, hint.y);
            if (branch == null) continue;
            AddConnection(mainPath[i], branch);

            if (type == RoomType.Battle)
            {
                battleCount++;
            }
            else
            {
                usedEventTypes.Add(type);
                RunManager.Instance?.RegisterEventRoomPlaced(type);
            }

            if (Random.value < 0.3f)
            {
                RoomType type2 = PickUnusedEventType(eventWeights, usedEventTypes);
                if (type2 != RoomType.None)
                {
                    var hint2 = Clamped(branch.MatrixCenter + RandomCardinalOffset(10, 20));
                    var branch2 = PlaceRoom(type2, hint2.x, hint2.y);
                    if (branch2 != null)
                    {
                        AddConnection(branch, branch2);
                        usedEventTypes.Add(type2);
                        RunManager.Instance?.RegisterEventRoomPlaced(type2);
                    }
                }
            }
        }
    }

    // Returns a weighted random event type not yet used this map.
    // Returns RoomType.None if all event types are exhausted.
    RoomType PickUnusedEventType(Dictionary<RoomType, float> weights, HashSet<RoomType> used)
    {
        var available = new List<(RoomType t, float w)>();
        foreach (var kvp in weights)
            if (!used.Contains(kvp.Key))
                available.Add((kvp.Key, kvp.Value));

        if (available.Count == 0) return RoomType.None;

        float total = 0f;
        foreach (var (_, w) in available) total += w;

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var (t, w) in available)
        {
            cumulative += w;
            if (roll <= cumulative) return t;
        }
        return available[available.Count - 1].t;
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

    // ── Corridor carving (A*) ─────────────────────────────────────────────────

    void CarveAllCorridors()
    {
        // Carve the spawn → first battle room corridor first so it always
        // gets a direct unobstructed path through empty matrix space.
        // All other corridors are carved afterward through whatever remains.
        var spawnConnections = new List<(Vector2Int, Vector2Int)>();
        var otherConnections = new List<(Vector2Int, Vector2Int)>();

        foreach (var conn in _connections)
        {
            var nodeA = _rooms.Find(r => r.MatrixCenter == conn.Item1);
            var nodeB = _rooms.Find(r => r.MatrixCenter == conn.Item2);
            if (nodeA == null || nodeB == null) continue;

            bool involvesSpawn = nodeA.Type == RoomType.Spawn || nodeB.Type == RoomType.Spawn;
            if (involvesSpawn)
                spawnConnections.Add(conn);
            else
                otherConnections.Add(conn);
        }

        foreach (var (a, b) in spawnConnections)
        {
            var nodeA = _rooms.Find(r => r.MatrixCenter == a);
            var nodeB = _rooms.Find(r => r.MatrixCenter == b);
            if (nodeA != null && nodeB != null)
                CarveAStar(nodeA.MatrixCenter, nodeB.MatrixCenter);
        }

        foreach (var (a, b) in otherConnections)
        {
            var nodeA = _rooms.Find(r => r.MatrixCenter == a);
            var nodeB = _rooms.Find(r => r.MatrixCenter == b);
            if (nodeA != null && nodeB != null)
                CarveAStar(nodeA.MatrixCenter, nodeB.MatrixCenter);
        }
    }

    void CarveAStar(Vector2Int start, Vector2Int goal)
    {
        var openSet = new SortedList<int, Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, int>();
        var inOpen = new HashSet<Vector2Int>();

        gScore[start] = 0;
        int startF = Heuristic(start, goal);
        openSet.Add(startF, start);
        inOpen.Add(start);

        var cardinals = new Vector2Int[]
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1),
        };

        while (openSet.Count > 0)
        {
            // Pop lowest f-score node
            var current = openSet.Values[0];
            openSet.RemoveAt(0);
            inOpen.Remove(current);

            if (current == goal)
            {
                // Reconstruct path and carve it
                CarvePath(ReconstructPath(cameFrom, current));
                return;
            }

            foreach (var dir in cardinals)
            {
                var neighbor = current + dir;

                if (neighbor.x < 0 || neighbor.y < 0 ||
                    neighbor.x >= matrixSize || neighbor.y >= matrixSize)
                    continue;

                // Already-carved corridor cells cost 0 — A* will actively
                // route through existing corridors rather than carving new
                // parallel ones, reducing redundant intersections.
                // Empty cells cost 1, room cells cost 50 (last resort).
                byte cell = _matrix[neighbor.x, neighbor.y];
                int stepCost = cell switch
                {
                    Cell.Corridor => 0,
                    Cell.Room => 50,
                    _ => 1
                };
                if (cell != Cell.Room && cell != Cell.Corridor && IsAdjacentToRoom(neighbor))
                    stepCost += 30;

                int tentativeG = gScore[current] + stepCost;

                if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    int f = tentativeG + Heuristic(neighbor, goal);

                    // SortedList requires unique keys — use a tiebreaker
                    while (inOpen.Contains(neighbor) == false && openSet.ContainsKey(f))
                        f++;

                    if (!inOpen.Contains(neighbor))
                    {
                        openSet.Add(f, neighbor);
                        inOpen.Add(neighbor);
                    }
                }
            }
        }

        Debug.LogWarning($"[MapGen] A* could not find path from {start} to {goal}, falling back to L-shape.");
        CarveFallbackL(start, goal);
    }

    int Heuristic(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    // Stamp corridorWidth cells around each step on the path
    void CarvePath(List<Vector2Int> path)
    {
        int half = corridorWidth / 2;
        foreach (var cell in path)
        {
            // Check if this path cell is adjacent to any room cell.
            // If so, only stamp the single center cell (width = 1) to keep
            // doorways exactly one cell wide at room edges.
            bool nearRoom = IsAdjacentToRoom(cell) || _matrix[cell.x, cell.y] == Cell.Room;

            if (nearRoom)
            {
                // Single cell doorway — no width expansion
                if (cell.x < 0 || cell.y < 0 || cell.x >= matrixSize || cell.y >= matrixSize) continue;
                if (_matrix[cell.x, cell.y] == Cell.Empty)
                    _matrix[cell.x, cell.y] = Cell.Corridor;
            }
            else
            {
                // Full width stamp in open space
                for (int wx = -half; wx < corridorWidth - half; wx++)
                    for (int wz = -half; wz < corridorWidth - half; wz++)
                    {
                        int cx = cell.x + wx;
                        int cz = cell.y + wz;
                        if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                        if (_matrix[cx, cz] == Cell.Empty)
                            _matrix[cx, cz] = Cell.Corridor;
                    }
            }
        }
    }

    // Fallback simple L-shape used if A* fails
    void CarveFallbackL(Vector2Int a, Vector2Int b)
    {
        Vector2Int corner = new Vector2Int(b.x, a.y);
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
            for (int z = z0; z <= z1; z++)
                for (int w = 0; w < corridorWidth; w++)
                {
                    int cx = horizontal ? x : x - half + w;
                    int cz = horizontal ? z - half + w : z;
                    if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                    if (_matrix[cx, cz] == Cell.Empty)
                        _matrix[cx, cz] = Cell.Corridor;
                }
    }

    int ManhattanDist(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    // Returns true if any room cell exists within minCorridorRoomSpacing cells
    // of this cell. Used to penalize corridor cells that run too close to
    // room walls they are not connected to.
    bool IsAdjacentToRoom(Vector2Int cell)
    {
        int spacing = 3;
        for (int dx = -spacing; dx <= spacing; dx++)
            for (int dz = -spacing; dz <= spacing; dz++)
            {
                int nx = cell.x + dx;
                int nz = cell.y + dz;
                if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                if (_matrix[nx, nz] == Cell.Room) return true;
            }
        return false;
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
        var obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "SpawnRoom";
        var sr = obj.AddComponent<SpawnRoom>();
        sr.node = node;
        return obj;
    }

    GameObject SpawnBattleRoom(RoomNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "BattleRoom";

        // Always use node.Size — this is what was stamped into the matrix
        // and what SpawnAllWalls uses. preset.roomSize is intentionally ignored
        // here to keep walls, trigger, and prefab footprint all in sync.
        Vector3 vol = new Vector3(node.Size.x, triggerHeight, node.Size.y);

        var room = obj.AddComponent<BattleRoom>();
        room.node = node;
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyCount = Random.Range(1, 4) + (RunManager.Instance?.CurrentFloor ?? 1);
        room.enemyPrefabs = PickFloorWeightedEnemyPrefabs();
        room.SetRoomSize(vol);

        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = vol;
        col.center = new Vector3(0, triggerHeight / 2f, 0);
        return obj;
    }

    GameObject SpawnBossRoom(RoomNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "BossRoom";

        Vector3 vol = new Vector3(node.Size.x, triggerHeight, node.Size.y);

        var room = obj.AddComponent<BossRoom>();
        room.node = node;
        // Pick boss by floor index — floor 1 → index 0, floor 2 → index 1, etc.
        int floorIndex = Mathf.Clamp((RunManager.Instance?.CurrentFloor ?? 1) - 1, 0, bossPrefabs.Length - 1);
        room.bossPrefab = bossPrefabs.Length > 0 ? bossPrefabs[floorIndex] : null;
        room.lootPrefab = lootPrefab;
        room.portalPrefab = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.SetRoomSize(vol);

        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = vol;
        col.center = new Vector3(0, triggerHeight / 2f, 0);
        return obj;
    }

    GameObject SpawnEventRoom(RoomNode node, GameObject[] arr,
                              System.Action<GameObject, Transform, RoomNode> setup)
    {
        if (arr == null || arr.Length == 0)
        {
            Debug.LogWarning($"[MapGen] Missing prefabs for {node.Type}");
            return null;
        }
        var obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = node.Type + "Room";
        var preset = obj.GetComponent<RoomPreset>();
        var pt = preset?.interactableSpawnPoint != null
            ? preset.interactableSpawnPoint : obj.transform;

        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(node.Size.x, triggerHeight, node.Size.y);
        col.center = new Vector3(0, triggerHeight / 2f, 0);

        setup(obj, pt, node);
        return obj;
    }

    void AddHealRoom(GameObject o, Transform p, RoomNode n) { var r = o.AddComponent<HealRoom>(); r.node = n; r.healStationPrefab = healStationPrefab; r.Init(p); }
    void AddShopRoom(GameObject o, Transform p, RoomNode n) { var r = o.AddComponent<ShopRoom>(); r.node = n; r.shopStationPrefab = shopStationPrefab; r.Init(p); }
    void AddUpgradeRoom(GameObject o, Transform p, RoomNode n) { var r = o.AddComponent<UpgradeRoom>(); r.node = n; r.upgradeStationPrefab = upgradeStationPrefab; r.Init(p); }
    void AddMergeRoom(GameObject o, Transform p, RoomNode n) { var r = o.AddComponent<MergeRoom>(); r.node = n; r.mergeStationPrefab = mergeStationPrefab; r.Init(p); }

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
        poly.extrude = floorThickness;
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
        go.layer = LayerMask.NameToLayer("Wall");   // must match wallLayerName in WallVisibility

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

    // Returns a subset of normalEnemyPrefabs available for the current floor.
    // Floor 1: indices 0-1  Floor 2: indices 0-1  Floor 3: indices 0-2  Floor 4: indices 0-3
    // BattleRoom.SpawnEnemies picks randomly from this array each spawn.
    GameObject[] PickFloorWeightedEnemyPrefabs()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
            return new GameObject[0];

        int floor = Mathf.Clamp(RunManager.Instance?.CurrentFloor ?? 1, 1, 4);

        // How many types are available this floor
        int availableCount = floor switch
        {
            1 => 2,
            2 => 2,
            3 => Mathf.Min(3, normalEnemyPrefabs.Length),
            _ => Mathf.Min(4, normalEnemyPrefabs.Length)
        };

        // Build the available pool for this floor
        var pool = new GameObject[availableCount];
        for (int i = 0; i < availableCount; i++)
            pool[i] = normalEnemyPrefabs[i];

        return pool;
    }

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