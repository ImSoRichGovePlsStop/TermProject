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

    [Header("Room Sizes")]
    public int minRoomSize = 6;
    public int maxRoomSize = 24;

    [Header("Doors")]
    public int doorWidth = 3;
    [Tooltip("Extra doors added beyond the spanning tree per adjacent room pair.")]
    [Range(0f, 1f)]
    public float extraDoorChance = 0.35f;

    [Header("Room Presets")]
    [Tooltip("Optional. Preset rooms are placed first; remaining space is filled with random rectangles.")]
    public BSPRoomPreset[] roomPresets;

    [Header("Room Type Assignment")]
    [Range(0f, 1f)] public float emptyRoomChance = 0.15f;
    [Range(0f, 1f)] public float battleRoomFraction = 0.45f;
    [Range(0f, 1f)] public float healRoomFraction = 0.12f;
    [Range(0f, 1f)] public float shopRoomFraction = 0.12f;
    [Range(0f, 1f)] public float rareLootFraction = 0.08f;

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

    static readonly Vector2Int[] Dirs = {
        new(1,0), new(-1,0), new(0,1), new(0,-1)
    };

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
        BuildConnectivity();
        PunchDoors();
        AssignRoomTypes();
        SpawnGeometry();

        FindFirstObjectByType<MinimapManager>()
            ?.BuildMinimapFromMatrix(_matrix, matrixSize, ToLegacyRoomNodes(), _edges);

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        OnMapReady?.Invoke(_nodes);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 1 — Fill the entire matrix with rooms
    //
    //  Seed with preset rooms first. Then grow from a frontier:
    //  pick a random unclaimed cell, find its largest possible rectangle,
    //  stamp it. Repeat until every cell is claimed.
    // ═════════════════════════════════════════════════════════════════════════

    void FillMatrix()
    {
        // Place preset rooms first
        if (roomPresets != null)
        {
            var pool = new List<BSPRoomPreset>(roomPresets);
            Shuffle(pool);
            foreach (var preset in pool)
                TryPlacePreset(preset);
        }

        // Fill remaining empty cells with random rectangles
        for (int pass = 0; pass < matrixSize * matrixSize; pass++)
        {
            // Find any unclaimed cell
            Vector2Int? empty = FindEmptyCell();
            if (!empty.HasValue) break;

            int ox = empty.Value.x, oz = empty.Value.y;

            // Grow the largest rectangle starting from this cell that stays empty
            int bestW = 1, bestH = 1;
            int maxW = Mathf.Min(maxRoomSize, matrixSize - ox);

            for (int w = 1; w <= maxW; w++)
            {
                if (ox + w > matrixSize) break;
                if (_matrix[ox + w - 1, oz] != Cell.Empty) break;

                int maxH = Mathf.Min(maxRoomSize, matrixSize - oz);
                for (int h = 1; h <= maxH; h++)
                {
                    if (oz + h > matrixSize) break;
                    if (!RowEmpty(ox, oz + h - 1, w)) break;
                    bestW = w; bestH = h;
                }
            }

            // Skip if there isn't enough space to meet the minimum size
            if (bestW < minRoomSize || bestH < minRoomSize)
            {
                // Can't fit a valid room here — stamp a 1-cell filler so the loop advances
                // (marks it claimed so we don't loop forever, but it stays non-Room)
                _matrix[ox, oz] = Cell.Occupied;
                continue;
            }

            int sw = Mathf.Min(bestW, Random.Range(minRoomSize, maxRoomSize + 1));
            int sh = Mathf.Min(bestH, Random.Range(minRoomSize, maxRoomSize + 1));

            StampRoom(ox, oz, sw, sh, preset: null);
        }
    }

    bool RowEmpty(int ox, int z, int w)
    {
        for (int x = ox; x < ox + w; x++)
            if (x >= matrixSize || _matrix[x, z] != Cell.Empty) return false;
        return true;
    }

    Vector2Int? FindEmptyCell()
    {
        // Scan top-left to find first empty cell
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_matrix[x, z] == Cell.Empty)
                    return new Vector2Int(x, z);
        return null;
    }

    void TryPlacePreset(BSPRoomPreset preset)
    {
        for (int attempt = 0; attempt < 60; attempt++)
        {
            int ox = Random.Range(0, matrixSize - preset.sizeX);
            int oz = Random.Range(0, matrixSize - preset.sizeZ);
            if (!RectEmpty(ox, oz, preset.sizeX, preset.sizeZ)) continue;
            StampRoom(ox, oz, preset.sizeX, preset.sizeZ, preset);
            return;
        }
    }

    void StampRoom(int ox, int oz, int sx, int sz, BSPRoomPreset preset)
    {
        var node = new MapNode
        {
            Type = RoomType.Battle,
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

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 2 — Build adjacency graph and spanning tree
    // ═════════════════════════════════════════════════════════════════════════

    void BuildConnectivity()
    {
        // Find all adjacent pairs
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

        // MST (Kruskal by shared-edge length descending — more shared = closer)
        var parent = new Dictionary<MapNode, MapNode>();
        foreach (var n in _nodes) parent[n] = n;
        MapNode Find(MapNode n) { while (parent[n] != n) { parent[n] = parent[parent[n]]; n = parent[n]; } return n; }
        void Union(MapNode a, MapNode b) { parent[Find(a)] = Find(b); }

        // Sort: most shared cells first (adjacent rooms get connected preferentially)
        var sorted = new List<KeyValuePair<(MapNode, MapNode), List<Vector2Int>>>(pairShared);
        sorted.Sort((x, y) => y.Value.Count.CompareTo(x.Value.Count));

        var guaranteed = new HashSet<(MapNode, MapNode)>();
        foreach (var kvp in sorted)
        {
            var (a, b) = kvp.Key;
            if (Find(a) != Find(b))
            {
                Union(a, b);
                AddEdge(a, b);
                guaranteed.Add(kvp.Key);
            }
        }

        // Extra doors for the rest of adjacent pairs
        foreach (var kvp in sorted)
        {
            if (guaranteed.Contains(kvp.Key)) continue;
            if (Random.value < extraDoorChance)
                AddEdge(kvp.Key.Item1, kvp.Key.Item2);
        }

        // Store shared cells for door punching
        _sharedCells = pairShared;
        _connectedPairs = new HashSet<(MapNode, MapNode)>();
        foreach (var e in _edges)
        {
            var key = e.A.GetHashCode() < e.B.GetHashCode() ? (e.A, e.B) : (e.B, e.A);
            _connectedPairs.Add(key);
        }
    }

    Dictionary<(MapNode, MapNode), List<Vector2Int>> _sharedCells;
    HashSet<(MapNode, MapNode)> _connectedPairs;

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 3 — Punch doors on all connected pairs
    // ═════════════════════════════════════════════════════════════════════════

    void PunchDoors()
    {
        foreach (var e in _edges)
        {
            var key = e.A.GetHashCode() < e.B.GetHashCode() ? (e.A, e.B) : (e.B, e.A);
            if (!_sharedCells.TryGetValue(key, out var cells)) continue;
            PunchDoor(cells);
        }
    }

    void PunchDoor(List<Vector2Int> boundary)
    {
        if (boundary.Count == 0) return;

        // Filter out void cells — preset void areas must never get doors
        var valid = new List<Vector2Int>();
        foreach (var c in boundary)
        {
            var owner = _roomMap[c.x, c.y];
            if (owner == null) continue;
            // If the owner has a preset, check the cell isn't in a void position
            if (owner.Preset != null && owner.Preset.IsVoid(c.x - owner.MinX, c.y - owner.MinZ)) continue;
            valid.Add(c);
        }
        if (valid.Count < doorWidth) return;

        valid.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        int mid = valid.Count / 2;
        int start = Mathf.Max(0, mid - doorWidth / 2);
        int end = Mathf.Min(valid.Count, start + doorWidth);

        // Only mark the boundary cell itself (A-side).
        // The wall skipping logic handles both sides via the _isDoor check.
        for (int i = start; i < end; i++)
            _isDoor[valid[i].x, valid[i].y] = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 4 — Assign room types
    // ═════════════════════════════════════════════════════════════════════════

    void AssignRoomTypes()
    {
        if (_nodes.Count == 0) return;

        MapNode spawnNode = _nodes[0], bossNode = _nodes[0];
        foreach (var n in _nodes)
        {
            if (n.CenterX + n.CenterZ < spawnNode.CenterX + spawnNode.CenterZ) spawnNode = n;
            if (n.CenterX + n.CenterZ > bossNode.CenterX + bossNode.CenterZ) bossNode = n;
        }
        spawnNode.Type = RoomType.Spawn;
        bossNode.Type = RoomType.Boss;

        // Prefer preset rooms for typed roles
        var rest = new List<MapNode>();
        foreach (var n in _nodes)
            if (n != spawnNode && n != bossNode)
                rest.Add(n);
        Shuffle(rest);

        int total = rest.Count;
        int emptyCnt = Mathf.RoundToInt(total * emptyRoomChance);
        int healCnt = Mathf.RoundToInt(total * healRoomFraction);
        int shopCnt = Mathf.RoundToInt(total * shopRoomFraction);
        int rareCnt = Mathf.RoundToInt(total * rareLootFraction);
        int mergeCnt = Mathf.Max(0, total - emptyCnt - healCnt - shopCnt - rareCnt
                       - Mathf.RoundToInt(total * battleRoomFraction));

        int idx = 0;
        for (int i = 0; i < emptyCnt && idx < total; i++, idx++) rest[idx].Type = RoomType.None;
        for (int i = 0; i < healCnt && idx < total; i++, idx++) rest[idx].Type = RoomType.Heal;
        for (int i = 0; i < shopCnt && idx < total; i++, idx++) rest[idx].Type = RoomType.Shop;
        for (int i = 0; i < rareCnt && idx < total; i++, idx++) rest[idx].Type = RoomType.RareLoot;
        for (int i = 0; i < mergeCnt && idx < total; i++, idx++) rest[idx].Type = RoomType.Merge;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 5 — Spawn geometry
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

    float NodeDist(MapNode a, MapNode b)
    {
        float dx = a.CenterX - b.CenterX, dz = a.CenterZ - b.CenterZ;
        return Mathf.Sqrt(dx * dx + dz * dz);
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