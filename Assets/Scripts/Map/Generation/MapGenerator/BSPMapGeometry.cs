/*
 * BSPMapGeometry — Office-style dungeon generator
 *
 * Generation pipeline:
 *   1. FillMatrix        — place all typed rooms (corners first, then battle, event, fill)
 *   2. SealNarrowGaps    — flood-fill empty regions and Unmarked room nodes; seal thin ones
 *   3. MergeSmallRooms   — merge Unmarked rooms below mergeAreaThreshold into neighbours
 *   4. BuildConnectivity — scan adjacency, build MST + all-pairs edges, store shared cells
 *   5. PunchDoors        — for each edge punch a door opening; Spawn restricted to Battle/Boss
 *   6. SpawnGeometry     — floor quads, wall quads (2-layer), void blockers, sealed cubes
 *
 * Room types:
 *   Marked   = Spawn | Boss | Battle | event types
 *   Event    = Heal | Shop | RareLoot | Merge  (max 1 each per floor, weighted random)
 *   Unmarked = filler space with no content
 *
 * Corner assignment:
 *   Spawn → random corner, Boss → different random corner,
 *   remaining 2 corners → Battle rooms, all placed before any random placement
 */

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
    public int mergeAreaThreshold = 20;

    [Header("Gap Sealing")]
    public int sealMinWidth = 4;
    public int sealMinArea = 10;

    [Header("Room Type Counts")]
    public int maxBattleCount = 10;
    public int maxEventCount = 6;

    [Header("Event Room Weights")]
    public float weightHeal = 1f;
    public float weightShop = 1f;
    public float weightRareLoot = 1f;
    public float weightMerge = 1f;

    [Header("Floor-Repeat Penalty")]
    [Range(0f, 1f)] public float repeatPenalty = 0.4f;

    [Header("Preset Usage Probability")]
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

    [Header("Walls")]
    public float wallHeight = 2f;
    public float wallThickness = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material floorMat;
    public Material wallMat;
    public Material sealedMat;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    public IReadOnlyList<MapNode> Nodes => _nodes;
    public IReadOnlyList<MapEdge> Edges => _edges;
    public byte[,] Matrix => _matrix;
    public int MatrixSize => matrixSize;
    public MapNode[,] RoomMapPublic => _roomMap;

    public event Action<IReadOnlyList<MapNode>> OnMapReady;

    byte[,] _matrix;
    MapNode[,] _roomMap;
    MapNode[,] _voidOwnerMap;
    bool[,] _isDoor;

    List<MapNode> _nodes = new();
    List<MapEdge> _edges = new();

    Dictionary<(MapNode, MapNode), List<Vector2Int>> _sharedCells;

    struct RectResult { public int x, z, width, height; }

    static readonly Vector2Int[] Dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

    void Start() => Generate();

    public void Generate()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _roomMap = new MapNode[matrixSize, matrixSize];
        _voidOwnerMap = new MapNode[matrixSize, matrixSize];
        _isDoor = new bool[matrixSize, matrixSize];
        _nodes.Clear();
        _edges.Clear();

        FillMatrix();
        SealNarrowGaps();
        MergeSmallEmptyRooms();
        BuildConnectivity();
        PunchDoors();
        SpawnGeometry();

        FindFirstObjectByType<MinimapManager>()
            ?.BuildMinimapFromMatrix(_matrix, matrixSize, ToLegacyRoomNodes(), _edges);

        if (navMeshSurface != null) navMeshSurface.BuildNavMesh();

        OnMapReady?.Invoke(_nodes);
    }

    // ── Step 1: Fill Matrix ───────────────────────────────────────────────────

    void FillMatrix()
    {
        int spawnCorner = Random.Range(0, 4);
        int bossCorner = (spawnCorner + 1 + Random.Range(0, 3)) % 4;

        PlaceTypedRoom(RoomType.Spawn, minBattleRoomSize, maxBattleRoomSize, presetChanceSpawn, spawnCorner);
        PlaceTypedRoom(RoomType.Boss, minBattleRoomSize, maxBattleRoomSize, presetChanceBoss, bossCorner);
        var usedCorners = new HashSet<int> { spawnCorner, bossCorner };
        for (int ci = 0; ci < 4; ci++)
            if (!usedCorners.Contains(ci))
                PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, ci);

        PlaceAdjacentBattle();

        for (int i = 0; i < maxBattleCount - 1; i++)
            PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, -1);

        var rm = RunManager.Instance;
        float wHeal = weightHeal * (rm != null && rm.WasMissingLastFloor(RoomType.Heal) ? 1f : repeatPenalty);
        float wShop = weightShop * (rm != null && rm.WasMissingLastFloor(RoomType.Shop) ? 1f : repeatPenalty);
        float wRare = weightRareLoot * (rm != null && rm.WasMissingLastFloor(RoomType.RareLoot) ? 1f : repeatPenalty);
        float wMrge = weightMerge * (rm != null && rm.WasMissingLastFloor(RoomType.Merge) ? 1f : repeatPenalty);

        var placed = new Dictionary<RoomType, int>
            { { RoomType.Heal, 0 }, { RoomType.Shop, 0 }, { RoomType.RareLoot, 0 }, { RoomType.Merge, 0 } };

        for (int i = 0; i < maxEventCount; i++)
        {
            float h = placed[RoomType.Heal] > 0 ? 0f : wHeal;
            float s = placed[RoomType.Shop] > 0 ? 0f : wShop;
            float r = placed[RoomType.RareLoot] > 0 ? 0f : wRare;
            float m = placed[RoomType.Merge] > 0 ? 0f : wMrge;
            float total = h + s + r + m;
            if (total <= 0f) break;

            float roll = Random.Range(0f, total);
            RoomType chosen;
            if ((roll -= h) < 0f) chosen = RoomType.Heal;
            else if ((roll -= s) < 0f) chosen = RoomType.Shop;
            else if ((roll -= r) < 0f) chosen = RoomType.RareLoot;
            else chosen = RoomType.Merge;

            float pChance = chosen switch
            {
                RoomType.Heal => presetChanceHeal,
                RoomType.Shop => presetChanceShop,
                RoomType.RareLoot => presetChanceRareLoot,
                _ => presetChanceMerge,
            };

            bool didPlace = false;
            if (Random.value < pChance && roomPresets != null)
            {
                var compat = new List<BSPRoomPreset>();
                foreach (var p in roomPresets) if (p != null && p.AllowsType(chosen)) compat.Add(p);
                Shuffle(compat);
                foreach (var preset in compat)
                    if (TryPlacePresetRoom(chosen, preset, -1)) { didPlace = true; break; }
            }
            if (!didPlace) didPlace = TryPlaceRandomRoom(chosen, minEventRoomSize, maxEventRoomSize, -1);
            if (didPlace) { placed[chosen]++; rm?.RegisterEventRoomPlaced(chosen); }
        }

        FillRemaining();
    }

    void PlaceAdjacentBattle()
    {
        MapNode spawn = null;
        foreach (var n in _nodes) if (n.Type == RoomType.Spawn) { spawn = n; break; }
        if (spawn == null) return;

        var faces = new List<(int fx, int fz, int dx, int dz)>
        {
            (spawn.MaxX + 1, spawn.MinZ,  1,  0),
            (spawn.MinX - 1, spawn.MinZ, -1,  0),
            (spawn.MinX, spawn.MaxZ + 1,  0,  1),
            (spawn.MinX, spawn.MinZ - 1,  0, -1),
        };
        Shuffle(faces);

        foreach (var (fx, fz, dx, dz) in faces)
            for (int sx = maxBattleRoomSize; sx >= minBattleRoomSize; sx--)
                for (int sz = maxBattleRoomSize; sz >= minBattleRoomSize; sz--)
                {
                    int ox, oz;
                    if (dx == 1) { ox = spawn.MaxX + 1; oz = spawn.MinZ + (spawn.Depth - sz) / 2; }
                    else if (dx == -1) { ox = spawn.MinX - sx; oz = spawn.MinZ + (spawn.Depth - sz) / 2; }
                    else if (dz == 1) { oz = spawn.MaxZ + 1; ox = spawn.MinX + (spawn.Width - sx) / 2; }
                    else { oz = spawn.MinZ - sz; ox = spawn.MinX + (spawn.Width - sx) / 2; }

                    ox = Mathf.Clamp(ox, 0, matrixSize - sx);
                    oz = Mathf.Clamp(oz, 0, matrixSize - sz);
                    if (!RectEmpty(ox, oz, sx, sz)) continue;
                    StampRoom(ox, oz, sx, sz, null, RoomType.Battle);
                    return;
                }
    }

    void PlaceTypedRoom(RoomType type, int minSz, int maxSz, float presetChance, int cornerIdx)
    {
        if (Random.value < presetChance && roomPresets != null && roomPresets.Length > 0)
        {
            var compat = new List<BSPRoomPreset>();
            foreach (var p in roomPresets) if (p != null && p.AllowsType(type)) compat.Add(p);
            Shuffle(compat);
            foreach (var preset in compat)
                if (TryPlacePresetRoom(type, preset, cornerIdx)) return;
        }
        TryPlaceRandomRoom(type, minSz, maxSz, cornerIdx);
    }

    bool TryPlacePresetRoom(RoomType type, BSPRoomPreset preset, int cornerIdx)
    {
        if (cornerIdx >= 0)
        {
            GetCornerOrigin(cornerIdx, preset.sizeX, preset.sizeZ, out int ox, out int oz);
            if (!RectEmptyNoGapCheck(ox, oz, preset.sizeX, preset.sizeZ)) return false;
            StampRoom(ox, oz, preset.sizeX, preset.sizeZ, preset, type);
            return true;
        }
        for (int i = 0; i < 80; i++)
        {
            int ox = Random.Range(0, matrixSize - preset.sizeX);
            int oz = Random.Range(0, matrixSize - preset.sizeZ);
            SnapToMinPadding(ref ox, ref oz, preset.sizeX, preset.sizeZ);
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
            for (int i = 0; i < 20; i++)
            {
                int sx = Random.Range(minSz, maxSz + 1), sz = Random.Range(minSz, maxSz + 1);
                GetCornerOrigin(cornerIdx, sx, sz, out int ox, out int oz);
                if (!RectEmptyNoGapCheck(ox, oz, sx, sz)) continue;
                StampRoom(ox, oz, sx, sz, null, type);
                return true;
            }
            return false;
        }
        for (int i = 0; i < 80; i++)
        {
            int sx = Random.Range(minSz, maxSz + 1), sz = Random.Range(minSz, maxSz + 1);
            int ox = Random.Range(0, Mathf.Max(1, matrixSize - sx));
            int oz = Random.Range(0, Mathf.Max(1, matrixSize - sz));
            SnapToMinPadding(ref ox, ref oz, sx, sz);
            if (!RectEmpty(ox, oz, sx, sz)) continue;
            StampRoom(ox, oz, sx, sz, null, type);
            return true;
        }
        return false;
    }

    void GetCornerOrigin(int cornerIdx, int sx, int sz, out int ox, out int oz)
    {
        switch (cornerIdx % 4)
        {
            case 0: ox = 0; oz = 0; break;
            case 1: ox = matrixSize - sx; oz = matrixSize - sz; break;
            case 2: ox = matrixSize - sx; oz = 0; break;
            default: ox = 0; oz = matrixSize - sz; break;
        }
    }

    void FillRemaining()
    {
        for (int minSz = minEmptyRoomSize; minSz >= 1; minSz--)
        {
            for (int pass = 0; pass < matrixSize * matrixSize; pass++)
            {
                var cell = FindEmptyCell();
                if (!cell.HasValue) break;

                int ox = cell.Value.x, oz = cell.Value.y;
                int bestW = 1, bestH = 1;

                for (int w = 1; w <= Mathf.Min(maxEmptyRoomSize, matrixSize - ox); w++)
                {
                    if (_matrix[ox + w - 1, oz] != Cell.Empty) break;
                    for (int h = 1; h <= Mathf.Min(maxEmptyRoomSize, matrixSize - oz); h++)
                    {
                        if (!RowEmpty(ox, oz + h - 1, w)) break;
                        bestW = w; bestH = h;
                    }
                }

                if (bestW < minSz || bestH < minSz)
                {
                    if (minSz == 1)
                        for (int fx = ox; fx < ox + bestW; fx++)
                            for (int fz = oz; fz < oz + bestH; fz++)
                                if (_matrix[fx, fz] == Cell.Empty) _matrix[fx, fz] = Cell.Occupied;
                    continue;
                }

                int sw = Mathf.Min(bestW, Mathf.Max(minSz, Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1)));
                int sh = Mathf.Min(bestH, Mathf.Max(minSz, Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1)));
                StampRoom(ox, oz, sw, sh, null, RoomType.Unmarked);
            }
        }
    }

    void StampRoom(int ox, int oz, int sx, int sz, BSPRoomPreset preset, RoomType type)
    {
        var node = new MapNode
        {
            Type = type,
            Preset = preset,
            MinX = ox,
            MinZ = oz,
            MaxX = ox + sx - 1,
            MaxZ = oz + sz - 1,
            WorldCenter = new Vector3(ox + sx * 0.5f, 0f, oz + sz * 0.5f),
        };
        _nodes.Add(node);
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                int presetZ = (sz - 1) - (z - oz);
                bool isVoid = preset != null && preset.IsVoid(x - ox, presetZ);
                bool isPillar = preset != null && preset.IsPillar(x - ox, presetZ);

                if (isPillar)
                {
                    _matrix[x, z] = Cell.Occupied;  // sealed cube via SpawnGeometry
                    _roomMap[x, z] = node;            // still belongs to this room
                }
                else
                {
                    _matrix[x, z] = Cell.Room;
                    _roomMap[x, z] = isVoid ? null : node;
                    if (isVoid) _voidOwnerMap[x, z] = node;
                }
            }
    }

    // ── Step 2: Seal Narrow Gaps ──────────────────────────────────────────────

    void SealNarrowGaps()
    {
        var visited = new bool[matrixSize, matrixSize];
        for (int sx = 0; sx < matrixSize; sx++)
            for (int sz = 0; sz < matrixSize; sz++)
            {
                if (visited[sx, sz]) continue;
                byte v = _matrix[sx, sz];
                if (v != Cell.Empty && v != Cell.Occupied) continue;

                var region = new List<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(sx, sz));
                visited[sx, sz] = true;
                int minX = sx, maxX = sx, minZ = sz, maxZ = sz;

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    region.Add(cur);
                    if (cur.x < minX) minX = cur.x; if (cur.x > maxX) maxX = cur.x;
                    if (cur.y < minZ) minZ = cur.y; if (cur.y > maxZ) maxZ = cur.y;
                    foreach (var d in Dirs)
                    {
                        int nx = cur.x + d.x, nz = cur.y + d.y;
                        if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                        if (visited[nx, nz]) continue;
                        byte nv = _matrix[nx, nz];
                        if (nv != Cell.Empty && nv != Cell.Occupied) continue;
                        visited[nx, nz] = true;
                        queue.Enqueue(new Vector2Int(nx, nz));
                    }
                }

                if (maxX - minX + 1 < minEmptyRoomSize || maxZ - minZ + 1 < minEmptyRoomSize)
                    foreach (var c in region) _matrix[c.x, c.y] = Cell.Occupied;
            }

        var vis2 = new bool[matrixSize, matrixSize];
        for (int sx = 0; sx < matrixSize; sx++)
            for (int sz = 0; sz < matrixSize; sz++)
            {
                if (vis2[sx, sz]) continue;
                var owner = _roomMap[sx, sz];
                if (owner == null || owner.Type != RoomType.Unmarked) continue;

                var region = new HashSet<Vector2Int>();
                var regionNodes = new HashSet<MapNode>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(new Vector2Int(sx, sz));
                vis2[sx, sz] = true;

                while (queue.Count > 0)
                {
                    var cur = queue.Dequeue();
                    region.Add(cur);
                    var co = _roomMap[cur.x, cur.y];
                    if (co != null) regionNodes.Add(co);
                    foreach (var d in Dirs)
                    {
                        int nx = cur.x + d.x, nz = cur.y + d.y;
                        if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                        if (vis2[nx, nz]) continue;
                        var nb = _roomMap[nx, nz];
                        if (nb == null || nb.Type != RoomType.Unmarked) continue;
                        vis2[nx, nz] = true;
                        queue.Enqueue(new Vector2Int(nx, nz));
                    }
                }

                var remaining = new HashSet<Vector2Int>(region);
                var toSeal = new List<Vector2Int>();

                while (remaining.Count > 0)
                {
                    var best = FindLargestRectangle(remaining);
                    for (int x = best.x; x < best.x + best.width; x++)
                        for (int z = best.z; z < best.z + best.height; z++)
                            remaining.Remove(new Vector2Int(x, z));

                    if (best.width < sealMinWidth || best.height < sealMinWidth || best.width * best.height < sealMinArea)
                        for (int x = best.x; x < best.x + best.width; x++)
                            for (int z = best.z; z < best.z + best.height; z++)
                                toSeal.Add(new Vector2Int(x, z));
                }
                foreach (var c in remaining) toSeal.Add(c);

                foreach (var c in toSeal) { _matrix[c.x, c.y] = Cell.Occupied; _roomMap[c.x, c.y] = null; }

                foreach (var node in regionNodes)
                {
                    bool anyLeft = false;
                    for (int x = node.MinX; x <= node.MaxX && !anyLeft; x++)
                        for (int z = node.MinZ; z <= node.MaxZ && !anyLeft; z++)
                            if (_roomMap[x, z] == node) anyLeft = true;
                    if (!anyLeft) _nodes.Remove(node);
                }
            }
    }

    RectResult FindLargestRectangle(HashSet<Vector2Int> cells)
    {
        if (cells.Count == 0) return default;
        int minX = int.MaxValue, maxX = int.MinValue, minZ = int.MaxValue, maxZ = int.MinValue;
        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minZ) minZ = c.y; if (c.y > maxZ) maxZ = c.y;
        }
        int W = maxX - minX + 1, H = maxZ - minZ + 1;
        var heights = new int[W];
        var best = new RectResult();
        int bestArea = 0;

        for (int row = 0; row < H; row++)
        {
            int z = minZ + row;
            for (int col = 0; col < W; col++)
                heights[col] = cells.Contains(new Vector2Int(minX + col, z)) ? heights[col] + 1 : 0;

            var stack = new Stack<int>();
            for (int col = 0; col <= W; col++)
            {
                int curH = col < W ? heights[col] : 0;
                while (stack.Count > 0 && heights[stack.Peek()] >= curH)
                {
                    int popCol = stack.Pop(), popH = heights[popCol];
                    int left = stack.Count > 0 ? stack.Peek() + 1 : 0;
                    int w = col - left, area = w * popH;
                    if (area > bestArea)
                    {
                        bestArea = area;
                        best = new RectResult { x = minX + left, z = z - popH + 1, width = w, height = popH };
                    }
                }
                if (col < W) stack.Push(col);
            }
        }
        return best;
    }

    // ── Step 3: Merge Small Empty Rooms ──────────────────────────────────────

    void MergeSmallEmptyRooms()
    {
        bool anyMerged = true;
        while (anyMerged)
        {
            anyMerged = false;
            foreach (var node in new List<MapNode>(_nodes))
            {
                if (node.Type != RoomType.Unmarked) continue;

                int area = 0;
                for (int x = node.MinX; x <= node.MaxX; x++)
                    for (int z = node.MinZ; z <= node.MaxZ; z++)
                        if (_roomMap[x, z] == node) area++;
                if (area >= mergeAreaThreshold) continue;

                MapNode best = null; int bestShared = 0;
                foreach (var other in _nodes)
                {
                    if (other == node || other.Type != RoomType.Unmarked) continue;
                    int shared = 0;
                    for (int x = node.MinX; x <= node.MaxX; x++)
                        for (int z = node.MinZ; z <= node.MaxZ; z++)
                        {
                            if (_roomMap[x, z] != node) continue;
                            foreach (var d in Dirs)
                            {
                                int nx = x + d.x, nz = z + d.y;
                                if (nx >= 0 && nz >= 0 && nx < matrixSize && nz < matrixSize && _roomMap[nx, nz] == other) shared++;
                            }
                        }
                    if (shared > bestShared) { bestShared = shared; best = other; }
                }
                if (best == null) continue;

                for (int x = node.MinX; x <= node.MaxX; x++)
                    for (int z = node.MinZ; z <= node.MaxZ; z++)
                        if (_roomMap[x, z] == node)
                        {
                            _roomMap[x, z] = best;
                            best.MinX = Mathf.Min(best.MinX, x); best.MinZ = Mathf.Min(best.MinZ, z);
                            best.MaxX = Mathf.Max(best.MaxX, x); best.MaxZ = Mathf.Max(best.MaxZ, z);
                        }
                _nodes.Remove(node);
                anyMerged = true;
                break;
            }
        }
    }

    // ── Step 4: Build Connectivity ────────────────────────────────────────────

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
            if (Find(a) != Find(b)) { Union(a, b); AddEdge(a, b); guaranteed.Add(kvp.Key); }
        }
        foreach (var kvp in sorted)
            if (!guaranteed.Contains(kvp.Key)) AddEdge(kvp.Key.Item1, kvp.Key.Item2);

        _sharedCells = pairShared;
    }

    // ── Step 5: Punch Doors ───────────────────────────────────────────────────

    void PunchDoors()
    {
        foreach (var kvp in _sharedCells)
        {
            var (a, b) = kvp.Key;
            bool involvesSpawn = a.Type == RoomType.Spawn || b.Type == RoomType.Spawn;
            if (involvesSpawn)
            {
                var other = a.Type == RoomType.Spawn ? b : a;
                if (other.Type != RoomType.Battle && other.Type != RoomType.Boss) continue;
            }
            PunchDoor(kvp.Value);
        }
        EnsureCornerRoomHasDoor(RoomType.Spawn);
    }

    void EnsureCornerRoomHasDoor(RoomType targetType)
    {
        MapNode target = null;
        foreach (var n in _nodes) if (n.Type == targetType) { target = n; break; }
        if (target == null) return;

        for (int x = target.MinX; x <= target.MaxX; x++)
            for (int z = target.MinZ; z <= target.MaxZ; z++)
                if (_isDoor[x, z]) return;

        MapNode best = null; int bestCount = 0;
        foreach (var pt in new[] { RoomType.Battle, RoomType.Boss })
        {
            foreach (var kvp in _sharedCells)
            {
                var other = kvp.Key.Item1 == target ? kvp.Key.Item2
                          : kvp.Key.Item2 == target ? kvp.Key.Item1 : null;
                if (other == null || other.Type != pt) continue;
                if (kvp.Value.Count > bestCount) { bestCount = kvp.Value.Count; best = other; }
            }
            if (best != null) break;
        }
        if (best == null)
            foreach (var kvp in _sharedCells)
            {
                var other = kvp.Key.Item1 == target ? kvp.Key.Item2
                          : kvp.Key.Item2 == target ? kvp.Key.Item1 : null;
                if (other != null && kvp.Value.Count > bestCount) { bestCount = kvp.Value.Count; best = other; }
            }

        if (best == null) return;
        AddEdge(target, best);
        var k = target.GetHashCode() < best.GetHashCode() ? (target, best) : (best, target);
        if (_sharedCells.TryGetValue(k, out var cells)) PunchDoor(cells);
    }

    void PunchDoor(List<Vector2Int> boundary)
    {
        if (boundary.Count == 0) return;
        var groups = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (var c in boundary)
        {
            var owner = _roomMap[c.x, c.y];
            if (owner == null) continue;
            if (owner.Preset != null && owner.Preset.IsVoid(c.x - owner.MinX, c.y - owner.MinZ)) continue;

            var fd = new List<Vector2Int>();
            foreach (var d in Dirs)
            {
                int nx = c.x + d.x, nz = c.y + d.y;
                if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                var nb = _roomMap[nx, nz];
                if (nb != null && nb != owner) fd.Add(d);
            }
            if (fd.Count != 1) continue;
            if (!groups.ContainsKey(fd[0])) groups[fd[0]] = new();
            groups[fd[0]].Add(c);
        }
        if (groups.Count == 0) return;

        List<Vector2Int> bestRun = null;
        foreach (var kvp in groups)
        {
            bool faceX = kvp.Key.x != 0;
            var cls = kvp.Value;
            cls.Sort((a, b) => faceX ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            var cur = new List<Vector2Int> { cls[0] };
            var gb = new List<Vector2Int> { cls[0] };
            for (int i = 1; i < cls.Count; i++)
            {
                bool same = faceX ? cls[i].x == cls[i - 1].x : cls[i].y == cls[i - 1].y;
                bool adj = faceX ? cls[i].y == cls[i - 1].y + 1 : cls[i].x == cls[i - 1].x + 1;
                if (same && adj) cur.Add(cls[i]);
                else { if (cur.Count > gb.Count) gb = new(cur); cur = new() { cls[i] }; }
            }
            if (cur.Count > gb.Count) gb = cur;
            if (bestRun == null || gb.Count > bestRun.Count) bestRun = gb;
        }

        if (bestRun == null || bestRun.Count < doorWidth) return;
        int mid = bestRun.Count / 2;
        int s = Mathf.Max(0, mid - doorWidth / 2);
        int e = Mathf.Min(bestRun.Count, s + doorWidth);
        for (int i = s; i < e; i++) _isDoor[bestRun[i].x, bestRun[i].y] = true;
    }

    // ── Step 6: Spawn Geometry ────────────────────────────────────────────────

    void SpawnGeometry()
    {
        var floorP = new GameObject("Floors").transform;
        var wallP = new GameObject("Walls").transform;
        var voidP = new GameObject("VoidBlockers").transform;
        var sealedP = new GameObject("Sealed").transform;
        var seenVoid = new HashSet<Vector2Int>();

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                if (_matrix[x, z] == Cell.Occupied) { SpawnSealedCube(sealedP, x, z); continue; }
                if (_matrix[x, z] != Cell.Room) continue;

                var owner = _roomMap[x, z];
                if (owner == null)
                {
                    if (seenVoid.Add(new Vector2Int(x, z))) SpawnVoidBlocker(voidP, x, z);
                    var vo = _voidOwnerMap[x, z];
                    foreach (var d in Dirs)
                    {
                        int nx = x + d.x, nz = z + d.y;
                        bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                        if (oob) { SpawnWallQuad(wallP, x, z, d); continue; }
                        if (_roomMap[nx, nz] != vo && _voidOwnerMap[nx, nz] != vo)
                            SpawnWallQuad(wallP, x, z, d);
                    }
                    continue;
                }

                SpawnFloorQuad(floorP, x, z, owner);
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                    bool nonRoom = !oob && _matrix[nx, nz] != Cell.Room;
                    bool voidNb = !oob && !nonRoom && _roomMap[nx, nz] == null;
                    bool voidSameOwner = voidNb && _voidOwnerMap[nx, nz] == owner;
                    bool diff = !oob && !nonRoom && !voidNb && _roomMap[nx, nz] != owner;
                    bool door = (_isDoor[x, z] || (!oob && !nonRoom && _isDoor[nx, nz])) && diff;

                    if (!door && (oob || nonRoom || diff || (voidNb && !voidSameOwner)))
                        SpawnWallQuad(wallP, x, z, d);
                }
            }
    }

    void SpawnVoidBlocker(Transform parent, int x, int z)
    {
        var go = new GameObject($"Void_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);
        go.layer = LayerMask.NameToLayer("Wall");
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, wallHeight / 2f, 0f);
        col.size = new Vector3(1f, wallHeight * 2f, 1f);
    }

    void SpawnSealedCube(Transform parent, int x, int z)
    {
        var go = new GameObject($"Sealed_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, wallHeight * 0.5f, z + 0.5f);
        go.transform.localScale = new Vector3(1f, wallHeight, 1f);
        go.layer = LayerMask.NameToLayer("Wall");
        go.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        go.AddComponent<MeshRenderer>().material = sealedMat;
        go.AddComponent<BoxCollider>();
    }

    void SpawnFloorQuad(Transform parent, int x, int z, MapNode owner)
    {
        var go = new GameObject($"F_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);
        go.layer = LayerMask.NameToLayer("Ground");

        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[] {
            new(-0.5f,0f,0.5f), new(0.5f,0f,0.5f), new(0.5f,0f,-0.5f), new(-0.5f,0f,-0.5f)
        });
        poly.extrude = floorThickness; poly.flipNormals = false;
        pb.CreateShapeFromPolygon(poly.controlPoints, poly.extrude, poly.flipNormals);
        pb.ToMesh(); pb.Refresh();
        if (floorMat != null) pb.GetComponent<Renderer>().material = floorMat;

        // Only Battle and Boss room floors get a MeshCollider — NavMesh bakes from these
        if (owner != null && (owner.Type == RoomType.Battle || owner.Type == RoomType.Boss))
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        }
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing)
    {
        float totalH = wallHeight + Mathf.Abs(floorThickness);
        float centerY = floorThickness + totalH / 2f;
        float hh = totalH / 2f;

        var go = new GameObject($"W_{x}_{z}_{facing.x}_{facing.y}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f + facing.x * 0.5f, centerY, z + 0.5f + facing.y * 0.5f);
        go.transform.rotation = Quaternion.LookRotation(new Vector3(-facing.x, 0, -facing.y));
        go.layer = LayerMask.NameToLayer("Wall");

        var pb = go.AddComponent<ProBuilderMesh>();
        var poly = go.AddComponent<PolyShape>();
        poly.SetControlPoints(new Vector3[] {
            new(-0.5f,-hh,0f), new(0.5f,-hh,0f), new(0.5f,hh,0f), new(-0.5f,hh,0f)
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
        for (int gap = 1; gap < minEmptyRoomSize; gap++)
        {
            int wx = ox - gap;
            if (wx >= 0)
            {
                bool empty = true;
                for (int z = oz; z < oz + sz && empty; z++) if (_matrix[wx, z] != Cell.Empty) empty = false;
                if (empty && wx - 1 >= 0)
                    for (int z = oz; z < oz + sz; z++) if (_matrix[wx - 1, z] == Cell.Room) return false;
            }
            int ex = ox + sx - 1 + gap;
            if (ex < matrixSize)
            {
                bool empty = true;
                for (int z = oz; z < oz + sz && empty; z++) if (_matrix[ex, z] != Cell.Empty) empty = false;
                if (empty && ex + 1 < matrixSize)
                    for (int z = oz; z < oz + sz; z++) if (_matrix[ex + 1, z] == Cell.Room) return false;
            }
            int sz2 = oz - gap;
            if (sz2 >= 0)
            {
                bool empty = true;
                for (int x = ox; x < ox + sx && empty; x++) if (_matrix[x, sz2] != Cell.Empty) empty = false;
                if (empty && sz2 - 1 >= 0)
                    for (int x = ox; x < ox + sx; x++) if (_matrix[x, sz2 - 1] == Cell.Room) return false;
            }
            int nz = oz + sz - 1 + gap;
            if (nz < matrixSize)
            {
                bool empty = true;
                for (int x = ox; x < ox + sx && empty; x++) if (_matrix[x, nz] != Cell.Empty) empty = false;
                if (empty && nz + 1 < matrixSize)
                    for (int x = ox; x < ox + sx; x++) if (_matrix[x, nz + 1] == Cell.Room) return false;
            }
        }
        return true;
    }

    bool RectEmptyNoGapCheck(int ox, int oz, int sx, int sz)
    {
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                if (x >= matrixSize || z >= matrixSize) return false;
                if (_matrix[x, z] != Cell.Empty) return false;
            }
        return true;
    }

    void SnapToMinPadding(ref int ox, ref int oz, int sx, int sz, int minGap = 3)
    {
        for (int gap = 1; gap < minGap; gap++)
        {
            int cx = ox - gap; if (cx < 0) break;
            bool hit = false;
            for (int z = oz; z < oz + sz && !hit; z++) if (cx < matrixSize && _matrix[cx, z] == Cell.Room) hit = true;
            if (hit) { ox -= gap; break; }
        }
        for (int gap = 1; gap < minGap; gap++)
        {
            int cx = ox + sx + gap - 1; if (cx >= matrixSize) break;
            bool hit = false;
            for (int z = oz; z < oz + sz && !hit; z++) if (_matrix[cx, z] == Cell.Room) hit = true;
            if (hit) { ox += gap; break; }
        }
        for (int gap = 1; gap < minGap; gap++)
        {
            int cz = oz - gap; if (cz < 0) break;
            bool hit = false;
            for (int x = ox; x < ox + sx && !hit; x++) if (cz < matrixSize && _matrix[x, cz] == Cell.Room) hit = true;
            if (hit) { oz -= gap; break; }
        }
        for (int gap = 1; gap < minGap; gap++)
        {
            int cz = oz + sz + gap - 1; if (cz >= matrixSize) break;
            bool hit = false;
            for (int x = ox; x < ox + sx && !hit; x++) if (_matrix[x, cz] == Cell.Room) hit = true;
            if (hit) { oz += gap; break; }
        }
        if (ox > 0 && ox < minGap) ox = 0;
        if (oz > 0 && oz < minGap) oz = 0;
        if (ox + sx < matrixSize && ox + sx > matrixSize - minGap) ox = matrixSize - sx;
        if (oz + sz < matrixSize && oz + sz > matrixSize - minGap) oz = matrixSize - sz;
        ox = Mathf.Clamp(ox, 0, matrixSize - sx);
        oz = Mathf.Clamp(oz, 0, matrixSize - sz);
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
                if (_matrix[x, z] == Cell.Empty) return new Vector2Int(x, z);
        return null;
    }

    void AddEdge(MapNode a, MapNode b)
    {
        if (a == b) return;
        foreach (var e in a.Edges) if (e.A == b || e.B == b) return;
        var edge = new MapEdge { A = a, B = b };
        _edges.Add(edge); a.Edges.Add(edge); b.Edges.Add(edge);
    }

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    List<RoomNode> ToLegacyRoomNodes()
    {
        var map = new Dictionary<MapNode, RoomNode>();
        foreach (var n in _nodes)
        {
            var leg = new RoomNode
            {
                Type = n.Type,
                MatrixOrigin = new(n.MinX, n.MinZ),
                MatrixCenter = new(n.CenterX, n.CenterZ),
                Size = new(n.Width, n.Depth),
                WorldPosition = n.WorldCenter,
                ChosenPrefab = n.ChosenPrefab,
                RoomObject = n.RoomObject,
            };
            map[n] = leg; n.LegacyNode = leg;
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
                RoomType.Unmarked => new Color(0.55f, 0.55f, 0.55f, 0.5f),
                _ => Color.grey
            };
            for (int x = node.MinX; x <= node.MaxX; x++)
                for (int z = node.MinZ; z <= node.MaxZ; z++)
                {
                    if (_roomMap[x, z] != node) continue;
                    foreach (var d in Dirs)
                    {
                        int nx = x + d.x, nz = z + d.y;
                        if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize || _roomMap[nx, nz] != node)
                        {
                            Gizmos.DrawCube(
                                new Vector3(x + 0.5f + d.x * 0.5f, 1f, z + 0.5f + d.y * 0.5f),
                                d.x != 0 ? new Vector3(0.05f, 0.15f, 1f) : new Vector3(1f, 0.15f, 0.05f));
                        }
                    }
                }
        }
        if (_isDoor == null) return;
        Gizmos.color = new Color(1f, 0.85f, 0f, 0.9f);
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_isDoor[x, z])
                    Gizmos.DrawCube(new Vector3(x + 0.5f, 1f, z + 0.5f), Vector3.one * 0.5f);
    }

    [ContextMenu("Print Matrix Debug")]
    void PrintMatrixDebug()
    {
        if (_matrix == null || _roomMap == null) return;
        int step = Mathf.Max(1, matrixSize / 60);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[BSPMap] Matrix {matrixSize}x{matrixSize} (1 char = {step} cells)");
        sb.AppendLine("Legend: S=Spawn B=Battle X=Boss s=Shop h=Heal r=RareLoot m=Merge .=Unmarked _=wall +=door v=void");
        sb.AppendLine();
        for (int z = 0; z < matrixSize; z += step)
        {
            for (int x = 0; x < matrixSize; x += step)
            {
                char ch = _matrix[x, z] != Cell.Room ? '_'
                        : _isDoor[x, z] ? '+'
                        : _roomMap[x, z] == null ? 'v'
                        : _roomMap[x, z].Type switch
                        {
                            RoomType.Spawn => 'S',
                            RoomType.Boss => 'X',
                            RoomType.Battle => 'B',
                            RoomType.Shop => 's',
                            RoomType.Heal => 'h',
                            RoomType.RareLoot => 'r',
                            RoomType.Merge => 'm',
                            RoomType.Unmarked => '.',
                            _ => '?',
                        };
                sb.Append(ch);
            }
            sb.AppendLine();
        }
        Debug.Log(sb.ToString());
    }
}