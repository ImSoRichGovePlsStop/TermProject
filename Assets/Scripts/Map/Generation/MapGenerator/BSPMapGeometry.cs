using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Random = UnityEngine.Random;

public class BSPMapGeometry : MonoBehaviour
{
    [Header("Matrix")]
    public int matrixSize = 150;

    [Header("Battle Room Sizes")]
    public int minBattleRoomSize = 8;
    public int maxBattleRoomSize = 24;

    [Header("Event Room Sizes")]
    public int minEventRoomSize = 6;
    public int maxEventRoomSize = 18;

    [Header("Empty Room Sizes")]
    public int minEmptyRoomSize = 4;
    public int maxEmptyRoomSize = 14;
    [Tooltip("Empty rooms smaller than this area (cells) will be merged with neighbours.")]
    public int mergeAreaThreshold = 20;

    [Header("Room Type Counts")]
    [Tooltip("How many Spawn rooms to place (always 1).")]
    public int spawnCount = 1;
    [Tooltip("How many Boss rooms to place (always 1).")]
    public int bossCount = 1;
    public int battleCount = 8;
    public int healCount = 1;
    public int shopCount = 1;
    public int rareLootCount = 1;
    public int mergeCount = 1;
    [Tooltip("Remaining unclaimed space becomes empty rooms.")]
    public int emptyCount = 6;

    [Header("Preset Usage Probability per Type")]
    [Tooltip("0 = always random rect, 1 = always use preset if available.")]
    [Range(0f, 1f)] public float presetChanceBattle = 0.7f;
    [Range(0f, 1f)] public float presetChanceHeal = 1f;
    [Range(0f, 1f)] public float presetChanceShop = 1f;
    [Range(0f, 1f)] public float presetChanceRareLoot = 1f;
    [Range(0f, 1f)] public float presetChanceMerge = 1f;
    [Range(0f, 1f)] public float presetChanceSpawn = 1f;
    [Range(0f, 1f)] public float presetChanceBoss = 1f;

    [Header("Room Presets")]
    public BSPRoomPreset[] roomPresets;

    [Header("Doors")]
    public int doorWidth = 3;
    [Range(0f, 1f)]
    public float extraDoorChance = 0.35f;

    [Header("Walls")]
    public float wallHeight = 2f;
    public float wallThickness = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material floorMat;
    public Material wallMat;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    // ── Public API ────────────────────────────────────────────────────────────

    public IReadOnlyList<MapNode> Nodes => _nodes;
    public IReadOnlyList<MapEdge> Edges => _edges;
    public byte[,] Matrix => _matrix;
    public int MatrixSize => matrixSize;
    public MapNode[,] RoomMapPublic => _roomMap;

    public event Action<IReadOnlyList<MapNode>> OnMapReady;

    // ── Internal ──────────────────────────────────────────────────────────────

    byte[,] _matrix;
    MapNode[,] _roomMap;
    bool[,] _isDoor;

    List<MapNode> _nodes = new();
    List<MapEdge> _edges = new();

    static readonly Vector2Int[] Dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    // ── Entry ─────────────────────────────────────────────────────────────────

    void Start() => Generate();

    public void Generate()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _roomMap = new MapNode[matrixSize, matrixSize];
        _isDoor = new bool[matrixSize, matrixSize];
        _nodes.Clear();
        _edges.Clear();

        FillMatrix();
        MergeSmallEmptyRooms();
        BuildConnectivity();
        PunchDoors();
        SpawnGeometry();

        FindFirstObjectByType<MinimapManager>()
            ?.BuildMinimapFromMatrix(_matrix, matrixSize, ToLegacyRoomNodes(), _edges);

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        OnMapReady?.Invoke(_nodes);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 1 — Fill matrix: place typed rooms in order, then fill remainder
    // ═════════════════════════════════════════════════════════════════════════

    void FillMatrix()
    {
        // Build placement queue: typed rooms in order of placement priority.
        // Spawn and Boss go into opposite corners. The rest are random.
        var queue = new List<(RoomType type, int minSize, int maxSize, float presetChance)>();

        for (int i = 0; i < spawnCount; i++) queue.Add((RoomType.Spawn, minBattleRoomSize, maxBattleRoomSize, presetChanceSpawn));
        for (int i = 0; i < bossCount; i++) queue.Add((RoomType.Boss, minBattleRoomSize, maxBattleRoomSize, presetChanceBoss));
        for (int i = 0; i < battleCount; i++) queue.Add((RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle));
        for (int i = 0; i < healCount; i++) queue.Add((RoomType.Heal, minEventRoomSize, maxEventRoomSize, presetChanceHeal));
        for (int i = 0; i < shopCount; i++) queue.Add((RoomType.Shop, minEventRoomSize, maxEventRoomSize, presetChanceShop));
        for (int i = 0; i < rareLootCount; i++) queue.Add((RoomType.RareLoot, minEventRoomSize, maxEventRoomSize, presetChanceRareLoot));
        for (int i = 0; i < mergeCount; i++) queue.Add((RoomType.Merge, minEventRoomSize, maxEventRoomSize, presetChanceMerge));

        // Shuffle everything except spawn/boss so event rooms land in random positions
        int fixedHead = spawnCount + bossCount;
        var shuffleable = queue.GetRange(fixedHead, queue.Count - fixedHead);
        Shuffle(shuffleable);
        queue.RemoveRange(fixedHead, queue.Count - fixedHead);
        queue.AddRange(shuffleable);

        // Spawn gets corner 0 (top-left). Boss gets any other corner at random.
        int spawnCorner = 0;
        int bossCorner = Random.Range(1, 4);  // 1, 2, or 3 — never same as spawn

        int cornerAssign = 0;
        foreach (var (type, minSz, maxSz, pChance) in queue)
        {
            int cIdx = -1;
            if (type == RoomType.Spawn) cIdx = spawnCorner;
            else if (type == RoomType.Boss) cIdx = bossCorner;
            PlaceTypedRoom(type, minSz, maxSz, pChance, cIdx);
        }

        // Fill remaining empty space with empty rooms then tiny filler
        FillRemaining();
    }

    void PlaceTypedRoom(RoomType type, int minSz, int maxSz, float presetChance, int cornerIdx)
    {
        // Try preset first if roll succeeds
        if (Random.value < presetChance && roomPresets != null && roomPresets.Length > 0)
        {
            var compatible = new List<BSPRoomPreset>();
            foreach (var p in roomPresets)
                if (p != null && p.AllowsType(type)) compatible.Add(p);

            if (compatible.Count > 0)
            {
                Shuffle(compatible);
                foreach (var preset in compatible)
                {
                    if (TryPlacePresetRoom(type, preset, cornerIdx)) return;
                }
            }
        }

        // Fall back to random rectangle
        TryPlaceRandomRoom(type, minSz, maxSz, cornerIdx);
    }

    bool TryPlacePresetRoom(RoomType type, BSPRoomPreset preset, int cornerIdx)
    {
        if (cornerIdx >= 0)
        {
            // Corner rooms have a fixed position — only one valid placement
            GetCornerOrigin(cornerIdx, preset.sizeX, preset.sizeZ, out int ox, out int oz);
            if (!RectEmpty(ox, oz, preset.sizeX, preset.sizeZ)) return false;
            StampRoom(ox, oz, preset.sizeX, preset.sizeZ, preset, type);
            return true;
        }

        for (int attempt = 0; attempt < 80; attempt++)
        {
            int ox = Random.Range(0, matrixSize - preset.sizeX);
            int oz = Random.Range(0, matrixSize - preset.sizeZ);
            if (!RectEmpty(ox, oz, preset.sizeX, preset.sizeZ)) continue;
            StampRoom(ox, oz, preset.sizeX, preset.sizeZ, preset, type);
            return true;
        }
        return false;
    }

    bool TryPlaceRandomRoom(RoomType type, int minSz, int maxSz, int cornerIdx)
    {
        if (cornerIdx >= 0)
        {
            // Corner rooms — try a few sizes, all anchored to the exact corner
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int sx = Random.Range(minSz, maxSz + 1);
                int sz = Random.Range(minSz, maxSz + 1);
                GetCornerOrigin(cornerIdx, sx, sz, out int ox, out int oz);
                if (!RectEmpty(ox, oz, sx, sz)) continue;
                StampRoom(ox, oz, sx, sz, null, type);
                return true;
            }
            return false;
        }

        for (int attempt = 0; attempt < 80; attempt++)
        {
            int sx = Random.Range(minSz, maxSz + 1);
            int sz = Random.Range(minSz, maxSz + 1);
            int ox = Random.Range(0, Mathf.Max(1, matrixSize - sx));
            int oz = Random.Range(0, Mathf.Max(1, matrixSize - sz));
            if (!RectEmpty(ox, oz, sx, sz)) continue;
            StampRoom(ox, oz, sx, sz, null, type);
            return true;
        }
        return false;
    }

    void GetCornerOrigin(int cornerIdx, int sx, int sz, out int ox, out int oz)
    {
        // Room corner flush with matrix corner — no margin, no scatter
        switch (cornerIdx % 4)
        {
            case 0: ox = 0; oz = 0; break;  // top-left
            case 1: ox = matrixSize - sx; oz = matrixSize - sz; break;  // bottom-right
            case 2: ox = matrixSize - sx; oz = 0; break;  // top-right
            default: ox = 0; oz = matrixSize - sz; break;  // bottom-left
        }
    }

    void FillRemaining()
    {
        // Fill every remaining empty cell with a None room.
        // We keep trying until no empty cells remain — using progressively
        // smaller minimum sizes so even tight gaps get claimed.
        for (int minSz = minEmptyRoomSize; minSz >= 1; minSz--)
        {
            for (int pass = 0; pass < matrixSize * matrixSize; pass++)
            {
                var cell = FindEmptyCell();
                if (!cell.HasValue) break;

                int ox = cell.Value.x, oz = cell.Value.y;

                int bestW = 1, bestH = 1;
                int maxW = Mathf.Min(maxEmptyRoomSize, matrixSize - ox);

                for (int w = 1; w <= maxW; w++)
                {
                    if (_matrix[ox + w - 1, oz] != Cell.Empty) break;
                    int maxH = Mathf.Min(maxEmptyRoomSize, matrixSize - oz);
                    for (int h = 1; h <= maxH; h++)
                    {
                        if (!RowEmpty(ox, oz + h - 1, w)) break;
                        bestW = w; bestH = h;
                    }
                }

                if (bestW < minSz || bestH < minSz)
                {
                    // Can't fit even minimum — mark this single cell occupied
                    // so the outer loop reduces minSz and tries again
                    if (minSz == 1) _matrix[ox, oz] = Cell.Occupied;
                    continue;
                }

                int sw = Mathf.Min(bestW, Mathf.Max(minSz, Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1)));
                int sh = Mathf.Min(bestH, Mathf.Max(minSz, Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1)));
                StampRoom(ox, oz, sw, sh, null, RoomType.None);
            }
        }
    }

    void StampRoom(int ox, int oz, int sx, int sz, BSPRoomPreset preset, RoomType type)
    {
        var node = new MapNode
        {
            Type = type,
            MinX = ox,
            MinZ = oz,
            MaxX = ox + sx - 1,
            MaxZ = oz + sz - 1,
            WorldCenter = new Vector3(ox + sx * 0.5f, 0f, oz + sz * 0.5f),
            Preset = preset,
        };
        _nodes.Add(node);

        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                bool isVoid = preset != null && preset.IsVoid(x - ox, z - oz);
                if (!isVoid)
                {
                    _matrix[x, z] = Cell.Room;
                    _roomMap[x, z] = node;
                }
            }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 2 — Merge small empty rooms into adjacent neighbours
    // ═════════════════════════════════════════════════════════════════════════

    void MergeSmallEmptyRooms()
    {
        bool anyMerged = true;
        while (anyMerged)
        {
            anyMerged = false;
            foreach (var node in new List<MapNode>(_nodes))
            {
                if (node.Type != RoomType.None) continue;

                int area = CountCells(node);
                if (area >= mergeAreaThreshold) continue;

                // Find the largest adjacent EMPTY room to merge into
                MapNode best = null;
                int bestShared = 0;

                foreach (var other in _nodes)
                {
                    if (other == node) continue;
                    if (other.Type != RoomType.None) continue;  // only merge with empty rooms
                    int shared = SharedEdgeCount(node, other);
                    if (shared > bestShared) { bestShared = shared; best = other; }
                }

                if (best == null) continue;

                // Re-assign all cells of node to best
                for (int x = node.MinX; x <= node.MaxX; x++)
                    for (int z = node.MinZ; z <= node.MaxZ; z++)
                        if (_roomMap[x, z] == node)
                        {
                            _roomMap[x, z] = best;
                            // Update best bounds
                            best.MinX = Mathf.Min(best.MinX, x);
                            best.MinZ = Mathf.Min(best.MinZ, z);
                            best.MaxX = Mathf.Max(best.MaxX, x);
                            best.MaxZ = Mathf.Max(best.MaxZ, z);
                        }

                _nodes.Remove(node);
                anyMerged = true;
                break; // restart loop after mutation
            }
        }
    }

    int CountCells(MapNode node)
    {
        int c = 0;
        for (int x = node.MinX; x <= node.MaxX; x++)
            for (int z = node.MinZ; z <= node.MaxZ; z++)
                if (_roomMap[x, z] == node) c++;
        return c;
    }

    int SharedEdgeCount(MapNode a, MapNode b)
    {
        int count = 0;
        for (int x = a.MinX; x <= a.MaxX; x++)
            for (int z = a.MinZ; z <= a.MaxZ; z++)
            {
                if (_roomMap[x, z] != a) continue;
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                    if (_roomMap[nx, nz] == b) count++;
                }
            }
        return count;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 3 — Build adjacency + MST
    // ═════════════════════════════════════════════════════════════════════════

    Dictionary<(MapNode, MapNode), List<Vector2Int>> _sharedCells;

    void BuildConnectivity()
    {
        var pairShared = new Dictionary<(MapNode, MapNode), List<Vector2Int>>();

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                var a = _roomMap[x, z];
                if (a == null) continue;
                foreach (var d in new[] { new Vector2Int(1, 0), new Vector2Int(0, 1) })
                {
                    int nx = x + d.x, nz = z + d.y;
                    if (nx >= matrixSize || nz >= matrixSize) continue;
                    var b = _roomMap[nx, nz];
                    if (b == null || b == a) continue;
                    var key = a.GetHashCode() < b.GetHashCode() ? (a, b) : (b, a);
                    if (!pairShared.ContainsKey(key)) pairShared[key] = new();
                    pairShared[key].Add(new Vector2Int(x, z));
                }
            }

        var parent = new Dictionary<MapNode, MapNode>();
        foreach (var n in _nodes) parent[n] = n;
        MapNode Find(MapNode n) { while (parent[n] != n) { parent[n] = parent[parent[n]]; n = parent[n]; } return n; }
        void Union(MapNode a, MapNode b) { parent[Find(a)] = Find(b); }

        var sorted = new List<KeyValuePair<(MapNode, MapNode), List<Vector2Int>>>(pairShared);
        sorted.Sort((x, y) => y.Value.Count.CompareTo(x.Value.Count));

        var guaranteed = new HashSet<(MapNode, MapNode)>();
        foreach (var kvp in sorted)
        {
            var (a, b) = kvp.Key;
            if (Find(a) != Find(b))
            {
                Union(a, b); AddEdge(a, b);
                guaranteed.Add(kvp.Key);
            }
        }

        foreach (var kvp in sorted)
        {
            if (guaranteed.Contains(kvp.Key)) continue;
            if (Random.value < extraDoorChance)
                AddEdge(kvp.Key.Item1, kvp.Key.Item2);
        }

        _sharedCells = pairShared;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 4 — Punch doors
    // ═════════════════════════════════════════════════════════════════════════

    void PunchDoors()
    {
        foreach (var e in _edges)
        {
            var key = e.A.GetHashCode() < e.B.GetHashCode() ? (e.A, e.B) : (e.B, e.A);
            if (_sharedCells.TryGetValue(key, out var cells))
                PunchDoor(cells);
        }
    }

    void PunchDoor(List<Vector2Int> boundary)
    {
        if (boundary.Count == 0) return;

        var valid = new List<Vector2Int>();
        foreach (var c in boundary)
        {
            var owner = _roomMap[c.x, c.y];
            if (owner == null) continue;
            if (owner.Preset != null && owner.Preset.IsVoid(c.x - owner.MinX, c.y - owner.MinZ)) continue;
            valid.Add(c);
        }
        if (valid.Count < doorWidth) return;

        valid.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        int mid = valid.Count / 2;
        int start = Mathf.Max(0, mid - doorWidth / 2);
        int end = Mathf.Min(valid.Count, start + doorWidth);

        for (int i = start; i < end; i++)
            _isDoor[valid[i].x, valid[i].y] = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 5 — Geometry
    // ═════════════════════════════════════════════════════════════════════════

    void SpawnGeometry()
    {
        var floorParent = new GameObject("Floors").transform;
        var wallParent = new GameObject("Walls").transform;

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                if (_matrix[x, z] != Cell.Room) continue;

                SpawnFloorQuad(floorParent, x, z);

                var owner = _roomMap[x, z];
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                    bool nonRoom = !oob && _matrix[nx, nz] != Cell.Room;
                    bool diff = !oob && !nonRoom && _roomMap[nx, nz] != owner;
                    bool door = (_isDoor[x, z] || (!oob && !nonRoom && _isDoor[nx, nz])) && diff;

                    if ((oob || nonRoom || diff) && !door)
                        SpawnWallQuad(wallParent, x, z, d);
                }
            }
    }

    void SpawnFloorQuad(Transform parent, int x, int z)
    {
        var go = new GameObject($"F_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);
        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[] {
            new(-0.5f,0f,0.5f), new(0.5f,0f,0.5f),
            new(0.5f,0f,-0.5f), new(-0.5f,0f,-0.5f),
        });
        poly.extrude = floorThickness; poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh(); pb.Refresh();
        if (floorMat != null) pb.GetComponent<Renderer>().material = floorMat;
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing)
    {
        Vector3 center = new(x + 0.5f, 0f, z + 0.5f);
        Vector3 faceOffset = new(facing.x * 0.5f, wallHeight / 2f, facing.y * 0.5f);
        Quaternion rot = Quaternion.LookRotation(new(-facing.x, 0, -facing.y));

        var go = new GameObject($"W_{x}_{z}_{facing.x}_{facing.y}");
        go.transform.SetParent(parent);
        go.transform.position = center + faceOffset;
        go.transform.rotation = rot;
        go.layer = LayerMask.NameToLayer("Wall");

        float hh = wallHeight / 2f;
        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[] {
            new(-0.5f,-hh,0f), new(0.5f,-hh,0f),
            new(0.5f,hh,0f),   new(-0.5f,hh,0f),
        });
        poly.extrude = wallThickness; poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh(); pb.Refresh();
        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        if (wallMat != null) pb.GetComponent<Renderer>().material = wallMat;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    bool RectEmpty(int ox, int oz, int sx, int sz)
    {
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                if (x >= matrixSize || z >= matrixSize) return false;
                if (_matrix[x, z] != Cell.Empty) return false;
            }
        return true;
    }

    bool RowEmpty(int ox, int z, int w)
    {
        for (int x = ox; x < ox + w; x++)
            if (x >= matrixSize || _matrix[x, z] != Cell.Empty) return false;
        return true;
    }

    Vector2Int? FindEmptyCell()
    {
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_matrix[x, z] == Cell.Empty)
                    return new Vector2Int(x, z);
        return null;
    }

    void AddEdge(MapNode a, MapNode b)
    {
        if (a == b) return;
        foreach (var e in a.Edges)
            if (e.A == b || e.B == b) return;
        var edge = new MapEdge { A = a, B = b };
        _edges.Add(edge);
        a.Edges.Add(edge);
        b.Edges.Add(edge);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    List<RoomNode> ToLegacyRoomNodes()
    {
        var map = new Dictionary<MapNode, RoomNode>();
        foreach (var n in _nodes)
        {
            var legacy = new RoomNode
            {
                Type = n.Type,
                MatrixOrigin = new(n.MinX, n.MinZ),
                MatrixCenter = new(n.CenterX, n.CenterZ),
                Size = new(n.Width, n.Depth),
                WorldPosition = n.WorldCenter,
                ChosenPrefab = n.ChosenPrefab,
                RoomObject = n.RoomObject,
            };
            map[n] = legacy; n.LegacyNode = legacy;
        }
        foreach (var edge in _edges)
        {
            if (!map.TryGetValue(edge.A, out var la) || !map.TryGetValue(edge.B, out var lb)) continue;
            if (!la.Neighbors.Contains(lb)) la.Neighbors.Add(lb);
            if (!lb.Neighbors.Contains(la)) lb.Neighbors.Add(la);
        }
        return new List<RoomNode>(map.Values);
    }

    // ── Gizmos ────────────────────────────────────────────────────────────────

    void OnDrawGizmos()
    {
        if (_roomMap == null) return;
        foreach (var node in _nodes)
        {
            Gizmos.color = node.Type switch
            {
                RoomType.Spawn => Color.green,
                RoomType.Battle => Color.red,
                RoomType.Boss => Color.magenta,
                RoomType.Shop => Color.yellow,
                RoomType.Merge => Color.cyan,
                RoomType.Heal => new Color(0.4f, 1f, 0.4f),
                RoomType.RareLoot => new Color(1f, 0.5f, 0f),
                RoomType.None => new Color(0.55f, 0.55f, 0.55f, 0.5f),
                _ => Color.grey
            };
            DrawRoomBorder(node);
        }
        if (_isDoor == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_isDoor[x, z])
                    Gizmos.DrawCube(new Vector3(x + 0.5f, 1f, z + 0.5f), Vector3.one * 0.5f);
    }

    void DrawRoomBorder(MapNode node)
    {
        for (int x = node.MinX; x <= node.MaxX; x++)
            for (int z = node.MinZ; z <= node.MaxZ; z++)
            {
                if (_roomMap == null || _roomMap[x, z] != node) continue;
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    bool border = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize
                                  || _roomMap[nx, nz] != node;
                    if (!border) continue;
                    Vector3 e = new(x + 0.5f + d.x * 0.5f, 1f, z + 0.5f + d.y * 0.5f);
                    Vector3 size = d.x != 0 ? new(0.05f, 0.15f, 1f) : new(1f, 0.15f, 0.05f);
                    Gizmos.DrawCube(e, size);
                }
            }
    }
}