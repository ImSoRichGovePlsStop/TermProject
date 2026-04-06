using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Random = UnityEngine.Random;

// ── Shared map types ─────────────────────────────────────────────────────────

public enum RoomType { None, Spawn, Battle, Boss, Shop, Merge, Heal, Upgrade }

public static class Cell
{
    public const byte Empty    = 0;
    public const byte Corridor = 1;
    public const byte Room     = 2;
    public const byte Occupied = 3;   // room border
}

public class RoomPort
{
    public RoomNode    Owner;
    public Vector2Int  Direction;
    public Vector2Int  ExitCell;
    public bool        IsUsed;
}

[System.Serializable]
public class RoomNode
{
    public RoomType   Type;
    public Vector2Int MatrixOrigin;
    public Vector2Int MatrixCenter;
    public Vector2Int Size;
    public Vector3    WorldPosition;
    public GameObject ChosenPrefab;
    public GameObject RoomObject;
    [NonSerialized] public List<RoomNode> Neighbors = new();
    [NonSerialized] public RoomPort[]     Ports;
}

// ── MapGeometry ───────────────────────────────────────────────────────────────
// Responsible for:
//   • Building the matrix (room placement, ports, MST, A* corridor carving)
//   • Spawning ProBuilder floor and wall geometry
//   • Baking the NavMesh
//   • Firing OnMapReady so MapPopulator can populate rooms
//
// Does NOT know about enemy prefabs, interactables, or room scripts.

public class MapGeometry : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Room Prefabs (randomly selected per room)")]
    public GameObject[] spawnRoomPrefabs;
    public GameObject[] battleRoomPrefabs;
    public GameObject[] bossRoomPrefabs;
    public GameObject[] shopRoomPrefabs;
    public GameObject[] healRoomPrefabs;
    public GameObject[] upgradeRoomPrefabs;
    public GameObject[] mergeRoomPrefabs;

    [Header("Matrix")]
    public int matrixSize      = 150;
    public int minRoomSize     = 5;
    public int maxRoomSize     = 15;

    [Header("Generation")]
    [Range(3, 10)] public int minBattleRooms     = 3;
    [Range(3, 10)] public int maxBattleRooms     = 7;
    public int   maxRoomDistance                 = 15;
    [Range(0f, 1f)] public float branchChance    = 0.4f;
    public int   maxPlacementAttempts            = 50;

    [Header("Corridors")]
    public int corridorWidth   = 1;
    public int exitStubLength  = 1;

    [Header("Walls")]
    public float wallHeight    = 2f;
    public float wallThickness = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material corridorMat;
    public Material wallMat;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    // ── Public state (read by MapPopulator after OnMapReady) ──────────────────

    /// <summary>All placed rooms in map order.</summary>
    public IReadOnlyList<RoomNode> Rooms => _rooms;

    /// <summary>The matrix cell grid (Cell.* constants).</summary>
    public byte[,] Matrix => _matrix;

    public int MatrixSize => matrixSize;

    /// <summary>Fired after geometry is fully built and navmesh baked.</summary>
    public event Action<IReadOnlyList<RoomNode>> OnMapReady;

    // ── Internal state ────────────────────────────────────────────────────────

    private byte[,]        _matrix;
    private List<RoomNode> _rooms        = new();
    private RoomNode       _spawnRoom;
    private List<(RoomPort portA, RoomPort portB)> _corridorPairs = new();

    // ── Entry point ───────────────────────────────────────────────────────────

    void Start() => GenerateMap();

    public void GenerateMap()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _rooms.Clear();
        _corridorPairs.Clear();

        _spawnRoom = PlaceRoom(RoomType.Spawn, matrixSize / 2, matrixSize / 2, forced: true);
        if (_spawnRoom == null) { Debug.LogError("[MapGeometry] Failed to place spawn room."); return; }

        var mainPath = BuildMainPath(_spawnRoom, Random.Range(minBattleRooms, maxBattleRooms + 1));
        if (mainPath.Count == 0) { Debug.LogWarning("[MapGeometry] Main path empty."); return; }

        PlaceBoss(mainPath[mainPath.Count - 1]);
        AddBranches(mainPath);

        BuildMSTCorridors();

        SpawnCorridorGeometry();
        SpawnAllWalls();

        var minimap = FindFirstObjectByType<MinimapManager>();
        minimap?.BuildMinimapFromMatrix(_matrix, matrixSize, _rooms);

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();
        else
            Debug.LogWarning("[MapGeometry] NavMeshSurface not assigned — skipping navmesh bake.");

        OnMapReady?.Invoke(_rooms);
    }

    // ── Room placement ────────────────────────────────────────────────────────

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

            int ox = cx - (sx - 1) / 2;
            int oz = cz - (sz - 1) / 2;

            if (ox < 1 || oz < 1 || ox + sx >= matrixSize - 1 || oz + sz >= matrixSize - 1)
                continue;

            if (!forced && !AreaFree(ox - 3, oz - 3, sx + 6, sz + 6))
                continue;

            if (!forced && _rooms.Count > 0)
            {
                int mcxCheck = ox + (sx - 1) / 2;
                int mczCheck = oz + (sz - 1) / 2;
                var checkCenter = new Vector2Int(mcxCheck, mczCheck);
                bool withinRange = false;
                foreach (var r in _rooms)
                {
                    int centerDist = ManhattanDist(checkCenter, r.MatrixCenter);
                    int halfSelf   = (sx + sz) / 4;
                    int halfOther  = (r.Size.x + r.Size.y) / 4;
                    int edgeDist   = Mathf.Max(0, centerDist - halfSelf - halfOther);
                    if (edgeDist <= maxRoomDistance) { withinRange = true; break; }
                }
                if (!withinRange) continue;
            }

            StampRoom(ox, oz, sx, sz);
            StampOccupied(ox, oz, sx, sz);

            int mcx = ox + (sx - 1) / 2;
            int mcz = oz + (sz - 1) / 2;

            var node = new RoomNode
            {
                Type          = type,
                MatrixOrigin  = new Vector2Int(ox, oz),
                MatrixCenter  = new Vector2Int(mcx, mcz),
                Size          = new Vector2Int(sx, sz),
                WorldPosition = new Vector3(mcx + 0.5f, 0f, mcz + 0.5f),
                ChosenPrefab  = prefab,
                Ports         = BuildPorts(ox, oz, sx, sz, mcx, mcz)
            };

            _rooms.Add(node);
            return node;
        }

        Debug.LogWarning($"[MapGeometry] Could not place {type} after {maxPlacementAttempts} attempts.");
        return null;
    }

    RoomPort[] BuildPorts(int ox, int oz, int sx, int sz, int mcx, int mcz)
    {
        var cardinals = new Vector2Int[]
        {
            new Vector2Int( 1,  0),
            new Vector2Int(-1,  0),
            new Vector2Int( 0,  1),
            new Vector2Int( 0, -1),
        };

        var ports = new RoomPort[4];
        for (int i = 0; i < 4; i++)
        {
            var dir       = cardinals[i];
            var outerEdge = new Vector2Int(
                mcx + dir.x * ((sx - 1) / 2),
                mcz + dir.y * ((sz - 1) / 2));

            ports[i] = new RoomPort
            {
                Direction = dir,
                ExitCell  = outerEdge + dir,
                IsUsed    = false
            };
        }
        return ports;
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

    void StampOccupied(int ox, int oz, int sx, int sz)
    {
        for (int x = ox - 1; x <= ox + sx; x++)
            for (int z = oz - 1; z <= oz + sz; z++)
            {
                if (x < 0 || z < 0 || x >= matrixSize || z >= matrixSize) continue;
                if (_matrix[x, z] == Cell.Empty)
                    _matrix[x, z] = Cell.Occupied;
            }
    }

    // ── Graph building ────────────────────────────────────────────────────────

    List<RoomNode> BuildMainPath(RoomNode start, int count)
    {
        var path    = new List<RoomNode>();
        var current = start;

        for (int i = 0; i < count; i++)
        {
            int minOffset = i == 0 ? 8 : 15;
            int maxOffset = i == 0 ? 12 : 25;

            var hint = Clamped(current.MatrixCenter + RandomCardinalOffset(minOffset, maxOffset));
            var next = PlaceRoom(RoomType.Battle, hint.x, hint.y);
            if (next == null) break;

            LinkRooms(current, next);
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
        if (boss == null) { Debug.LogWarning("[MapGeometry] Could not place boss room."); return; }
        LinkRooms(last, boss);
    }

    void AddBranches(List<RoomNode> mainPath)
    {
        var eventWeights = new Dictionary<RoomType, float>
        {
            { RoomType.Heal,    4f },
            { RoomType.Shop,    3f },
            { RoomType.Upgrade, 2f },
            { RoomType.Merge,   1f },
        };

        foreach (var key in new List<RoomType>(eventWeights.Keys))
            if (RunManager.Instance != null && RunManager.Instance.WasMissingLastFloor(key))
                eventWeights[key] *= 2f;

        var usedEventTypes = new HashSet<RoomType>();
        int battleCount    = mainPath.Count;

        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;

            bool     canPlaceBattle = battleCount < maxBattleRooms;
            RoomType type;

            if (canPlaceBattle && Random.value >= 0.25f)
                type = RoomType.Battle;
            else
            {
                type = PickUnusedEventType(eventWeights, usedEventTypes);
                if (type == RoomType.None) type = RoomType.Battle;
            }

            var anchor = ClosestRoom(mainPath[i], excludeSpawn: true);
            var hint   = Clamped(anchor.MatrixCenter + RandomCardinalOffset(10, 20));
            var branch = PlaceRoom(type, hint.x, hint.y);
            if (branch == null) continue;

            LinkRooms(anchor, branch);

            if (type == RoomType.Battle)
                battleCount++;
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
                    var anchor2 = ClosestRoom(branch, excludeSpawn: true);
                    var hint2   = Clamped(anchor2.MatrixCenter + RandomCardinalOffset(10, 20));
                    var branch2 = PlaceRoom(type2, hint2.x, hint2.y);
                    if (branch2 != null)
                    {
                        LinkRooms(anchor2, branch2);
                        usedEventTypes.Add(type2);
                        RunManager.Instance?.RegisterEventRoomPlaced(type2);
                    }
                }
            }
        }
    }

    void LinkRooms(RoomNode a, RoomNode b)
    {
        if (a.Neighbors.Contains(b)) return;
        if (a.Type == RoomType.Spawn && b.Type != RoomType.Battle) return;
        if (b.Type == RoomType.Spawn && a.Type != RoomType.Battle) return;
        a.Neighbors.Add(b);
        b.Neighbors.Add(a);
    }

    RoomNode ClosestRoom(RoomNode from, bool excludeSpawn = false)
    {
        RoomNode best     = from;
        float    bestDist = float.MaxValue;
        foreach (var r in _rooms)
        {
            if (r == from) continue;
            if (excludeSpawn && r.Type == RoomType.Spawn) continue;
            float d = Vector2Int.Distance(from.MatrixCenter, r.MatrixCenter);
            if (d < bestDist) { bestDist = d; best = r; }
        }
        return best;
    }

    RoomType PickUnusedEventType(Dictionary<RoomType, float> weights, HashSet<RoomType> used)
    {
        var available = new List<(RoomType t, float w)>();
        foreach (var kvp in weights)
            if (!used.Contains(kvp.Key))
                available.Add((kvp.Key, kvp.Value));

        if (available.Count == 0) return RoomType.None;

        float total = 0f;
        foreach (var (_, w) in available) total += w;

        float roll = Random.Range(0f, total), cumul = 0f;
        foreach (var (t, w) in available)
        {
            cumul += w;
            if (roll <= cumul) return t;
        }
        return available[available.Count - 1].t;
    }

    // ── MST corridors ─────────────────────────────────────────────────────────

    void BuildMSTCorridors()
    {
        if (_rooms.Count == 0) return;

        bool spawnPortUsed = false;
        var  inMST         = new HashSet<RoomNode>();
        var  excluded      = new HashSet<RoomNode>();
        inMST.Add(_spawnRoom);

        while (inMST.Count < _rooms.Count)
        {
            RoomNode bestA = null, bestB = null;
            int      bestDist = int.MaxValue;

            foreach (var a in inMST)
            {
                if (excluded.Contains(a) || !HasFreePort(a)) continue;

                foreach (var b in _rooms)
                {
                    if (inMST.Contains(b) || excluded.Contains(b) || !HasFreePort(b)) continue;

                    if (a.Type == RoomType.Spawn && b.Type != RoomType.Battle) continue;
                    if (b.Type == RoomType.Spawn && a.Type != RoomType.Battle) continue;
                    if ((a.Type == RoomType.Spawn || b.Type == RoomType.Spawn) && spawnPortUsed) continue;

                    int dist = ManhattanDist(a.MatrixCenter, b.MatrixCenter);
                    if (dist < bestDist) { bestDist = dist; bestA = a; bestB = b; }
                }
            }

            if (bestA == null || bestB == null)
            {
                foreach (var r in _rooms)
                    if (!inMST.Contains(r)) inMST.Add(r);
                break;
            }

            inMST.Add(bestB);

            ClaimBestPortPair(bestA, bestB, out var portA, out var portB);

            if (portA != null && portB != null)
            {
                portA.Owner = bestA;
                portB.Owner = bestB;
                _corridorPairs.Add((portA, portB));

                if (bestA.Type == RoomType.Spawn || bestB.Type == RoomType.Spawn)
                    spawnPortUsed = true;

                if (!bestA.Neighbors.Contains(bestB)) bestA.Neighbors.Add(bestB);
                if (!bestB.Neighbors.Contains(bestA)) bestB.Neighbors.Add(bestA);
            }
            else
            {
                if (portA != null) portA.IsUsed = false;
                if (portB != null) portB.IsUsed = false;
                excluded.Add(bestB);
                Debug.LogWarning($"[MapGeometry] MST: could not claim ports {bestA.Type}\u2194{bestB.Type}");
            }
        }

        // carve priority: spawn+battle first, boss second, event rooms last
        _corridorPairs.Sort((x, y) => ScorePair(x).CompareTo(ScorePair(y)));

        foreach (var (pA, pB) in _corridorPairs)
            CarvePortToPort(pA, pB);
    }

    static int ScorePair((RoomPort, RoomPort) pair)
    {
        var (a, b) = pair;
        bool aEvent = IsEventRoom(a.Owner);
        bool bEvent = IsEventRoom(b.Owner);
        bool boss   = a.Owner?.Type == RoomType.Boss || b.Owner?.Type == RoomType.Boss;
        if (!aEvent && !bEvent && !boss) return 0;
        if (boss)                        return 1;
        return 2;
    }

    static bool IsEventRoom(RoomNode node) =>
        node != null &&
        (node.Type == RoomType.Heal  || node.Type == RoomType.Shop ||
         node.Type == RoomType.Upgrade || node.Type == RoomType.Merge);

    bool HasFreePort(RoomNode node)
    {
        foreach (var p in node.Ports)
            if (!p.IsUsed) return true;
        return false;
    }

    void ClaimBestPortPair(RoomNode nodeA, RoomNode nodeB, out RoomPort portA, out RoomPort portB)
    {
        portA = null; portB = null;
        int bestScore = int.MaxValue;

        foreach (var pa in nodeA.Ports)
        {
            if (pa.IsUsed) continue;
            foreach (var pb in nodeB.Ports)
            {
                if (pb.IsUsed) continue;

                Vector2Int tipA  = pa.ExitCell + pa.Direction * exitStubLength;
                Vector2Int tipB  = pb.ExitCell + pb.Direction * exitStubLength;
                int score = TurnCost(pa.Direction, tipB - tipA) +
                            TurnCost(pb.Direction, tipA - tipB);

                if (score < bestScore) { bestScore = score; portA = pa; portB = pb; }
            }
        }

        if (portA != null) portA.IsUsed = true;
        if (portB != null) portB.IsUsed = true;
    }

    int TurnCost(Vector2Int dir, Vector2Int toTarget)
    {
        if (toTarget == Vector2Int.zero) return 0;
        bool targetH = Mathf.Abs(toTarget.x) >= Mathf.Abs(toTarget.y);
        bool dirH    = dir.x != 0;
        if (dirH == targetH) return Vector2.Dot(dir, toTarget) >= 0 ? 0 : 2;
        return 1;
    }

    // ── Corridor carving ──────────────────────────────────────────────────────

    void CarvePortToPort(RoomPort portA, RoomPort portB)
    {
        Vector2Int tipA   = CarveStub(portA.ExitCell, portA.Direction);
        Vector2Int tipB   = CarveStub(portB.ExitCell, portB.Direction);
        Vector2Int startA = ForceStep(tipA, portA.Direction);
        Vector2Int startB = ForceStep(tipB, portB.Direction);
        CarveAStar(startA, startB);
    }

    Vector2Int ForceStep(Vector2Int tip, Vector2Int dir)
    {
        Vector2Int next = tip + dir;
        if (next.x < 0 || next.y < 0 || next.x >= matrixSize || next.y >= matrixSize) return tip;
        if (_matrix[next.x, next.y] == Cell.Empty) _matrix[next.x, next.y] = Cell.Corridor;
        return next;
    }

    Vector2Int CarveStub(Vector2Int exitCell, Vector2Int dir)
    {
        Vector2Int tip = exitCell;
        for (int i = 0; i < exitStubLength; i++)
        {
            Vector2Int c = exitCell + dir * i;
            if (c.x < 0 || c.y < 0 || c.x >= matrixSize || c.y >= matrixSize) break;
            if (_matrix[c.x, c.y] != Cell.Room) _matrix[c.x, c.y] = Cell.Corridor;
            tip = c;
        }
        return tip;
    }

    void CarveAStar(Vector2Int start, Vector2Int goal)
    {
        var gScore  = new Dictionary<Vector2Int, int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var open    = new SortedDictionary<(int f, int id), Vector2Int>();
        var inOpen  = new HashSet<Vector2Int>();
        int uid     = 0;

        gScore[start] = 0;
        open[(Heuristic(start, goal), uid++)] = start;
        inOpen.Add(start);

        var dirs = new Vector2Int[]
        {
            new Vector2Int( 1, 0), new Vector2Int(-1, 0),
            new Vector2Int( 0, 1), new Vector2Int( 0,-1),
        };

        while (open.Count > 0)
        {
            var firstKey = default((int, int));
            foreach (var k in open.Keys) { firstKey = k; break; }
            var current = open[firstKey];
            open.Remove(firstKey);
            inOpen.Remove(current);

            if (current == goal)
            {
                CarvePath(ReconstructPath(cameFrom, current));
                return;
            }

            int g = gScore[current];
            foreach (var dir in dirs)
            {
                var nb = current + dir;
                if (nb.x < 0 || nb.y < 0 || nb.x >= matrixSize || nb.y >= matrixSize) continue;

                int stepCost = _matrix[nb.x, nb.y] switch
                {
                    Cell.Corridor => 1,
                    Cell.Room     => 100,
                    Cell.Occupied => 80,
                    _             => 2
                };

                int tentG = g + stepCost;
                if (gScore.TryGetValue(nb, out int existing) && tentG >= existing) continue;

                gScore[nb]   = tentG;
                cameFrom[nb] = current;

                if (!inOpen.Contains(nb))
                {
                    open[(tentG + Heuristic(nb, goal), uid++)] = nb;
                    inOpen.Add(nb);
                }
            }
        }

        Debug.LogWarning($"[MapGeometry] A* failed: {start} → {goal}");
    }

    int Heuristic(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int end)
    {
        var path = new List<Vector2Int>();
        var cur  = end;
        while (cameFrom.ContainsKey(cur)) { path.Add(cur); cur = cameFrom[cur]; }
        path.Add(cur);
        path.Reverse();
        return path;
    }

    void CarvePath(List<Vector2Int> path)
    {
        int half = corridorWidth / 2;
        foreach (var cell in path)
        {
            if (WouldTouchRoom(cell, half))
            {
                if (_matrix[cell.x, cell.y] == Cell.Empty)
                    _matrix[cell.x, cell.y] = Cell.Corridor;
                continue;
            }

            for (int dx = -half; dx < corridorWidth - half; dx++)
                for (int dz = -half; dz < corridorWidth - half; dz++)
                {
                    int cx = cell.x + dx, cz = cell.y + dz;
                    if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                    if (_matrix[cx, cz] == Cell.Empty) _matrix[cx, cz] = Cell.Corridor;
                }
        }
    }

    bool WouldTouchRoom(Vector2Int cell, int half)
    {
        for (int dx = -half; dx < corridorWidth - half; dx++)
            for (int dz = -half; dz < corridorWidth - half; dz++)
            {
                int cx = cell.x + dx, cz = cell.y + dz;
                if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;

                if (cx > 0               && IsRoomBorder(_matrix[cx - 1, cz])) return true;
                if (cx < matrixSize - 1  && IsRoomBorder(_matrix[cx + 1, cz])) return true;
                if (cz > 0               && IsRoomBorder(_matrix[cx, cz - 1])) return true;
                if (cz < matrixSize - 1  && IsRoomBorder(_matrix[cx, cz + 1])) return true;
            }
        return false;
    }

    static bool IsRoomBorder(byte cell) => cell == Cell.Room || cell == Cell.Occupied;

    // ── ProBuilder geometry ───────────────────────────────────────────────────

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

        var pb   = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[]
        {
            new Vector3(-0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f,  0.5f),
            new Vector3( 0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, -0.5f),
        });
        poly.extrude     = floorThickness;
        poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh(); pb.Refresh();

        if (corridorMat != null)
            pb.GetComponent<Renderer>().material = corridorMat;
    }

    void SpawnAllWalls()
    {
        var parent = new GameObject("Walls").transform;
        var dirs   = new[]
        {
            new Vector2Int( 1,  0), new Vector2Int(-1,  0),
            new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        };

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                if (_matrix[x, z] == Cell.Empty || _matrix[x, z] == Cell.Occupied) continue;
                foreach (var d in dirs)
                {
                    int  nx    = x + d.x, nz = z + d.y;
                    bool empty = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize
                                 || _matrix[nx, nz] == Cell.Empty
                                 || _matrix[nx, nz] == Cell.Occupied;
                    if (empty) SpawnWallQuad(parent, x, z, d);
                }
            }
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing)
    {
        Vector3    cellCenter  = new Vector3(x + 0.5f, 0f, z + 0.5f);
        Vector3    faceOffset  = new Vector3(facing.x * 0.5f, wallHeight / 2f, facing.y * 0.5f);
        Quaternion rot         = Quaternion.LookRotation(new Vector3(-facing.x, 0, -facing.y));

        var go = new GameObject($"W_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = cellCenter + faceOffset;
        go.transform.rotation = rot;
        go.layer = LayerMask.NameToLayer("Wall");

        float hh   = wallHeight / 2f;
        var   pb   = go.AddComponent<ProBuilderMesh>();
        var   poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[]
        {
            new Vector3(-0.5f, -hh, 0f),
            new Vector3( 0.5f, -hh, 0f),
            new Vector3( 0.5f,  hh, 0f),
            new Vector3(-0.5f,  hh, 0f),
        });
        poly.extrude     = wallThickness;
        poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh(); pb.Refresh();

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        if (wallMat != null)
            pb.GetComponent<Renderer>().material = wallMat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    int ManhattanDist(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    GameObject[] PrefabArrayFor(RoomType t) => t switch
    {
        RoomType.Spawn   => spawnRoomPrefabs,
        RoomType.Battle  => battleRoomPrefabs,
        RoomType.Boss    => bossRoomPrefabs,
        RoomType.Shop    => shopRoomPrefabs,
        RoomType.Heal    => healRoomPrefabs,
        RoomType.Upgrade => upgradeRoomPrefabs,
        RoomType.Merge   => mergeRoomPrefabs,
        _                => battleRoomPrefabs
    };

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
            0 => new Vector2Int( dist,  0),
            1 => new Vector2Int(-dist,  0),
            2 => new Vector2Int( 0,  dist),
            _ => new Vector2Int( 0, -dist),
        };
    }

    Vector2Int Clamped(Vector2Int v) => new Vector2Int(
        Mathf.Clamp(v.x, 10, matrixSize - 10),
        Mathf.Clamp(v.y, 10, matrixSize - 10));

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (_rooms == null) return;
        foreach (var node in _rooms)
        {
            Gizmos.color = node.Type switch
            {
                RoomType.Spawn   => Color.green,
                RoomType.Battle  => Color.red,
                RoomType.Boss    => Color.magenta,
                RoomType.Shop    => Color.yellow,
                RoomType.Merge   => Color.cyan,
                RoomType.Heal    => Color.white,
                RoomType.Upgrade => new Color(1f, 0.5f, 0f),
                _                => Color.grey
            };
            Gizmos.DrawWireCube(node.WorldPosition + Vector3.up, new Vector3(node.Size.x, 2f, node.Size.y));

            if (node.Ports == null) continue;
            Gizmos.color = Color.cyan;
            foreach (var port in node.Ports)
            {
                var pw  = new Vector3(port.ExitCell.x + 0.5f, 1f, port.ExitCell.y + 0.5f);
                var dir = new Vector3(port.Direction.x, 0, port.Direction.y);
                Gizmos.DrawRay(pw, dir * 2f);
            }
        }
    }
}
