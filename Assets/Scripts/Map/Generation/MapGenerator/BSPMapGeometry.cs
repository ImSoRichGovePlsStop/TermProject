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
    public int minEmptyRoomSize  = 4;
    public int maxEmptyRoomSize  = 14;
    [Tooltip("Empty rooms smaller than this area (cells) will be merged with neighbours.")]
    public int mergeAreaThreshold = 20;

    [Header("Room Type Counts (Maximums)")]
    [Tooltip("Exactly 1 spawn is always placed.")]
    public int spawnCount    = 1;
    [Tooltip("Exactly 1 boss is always placed.")]
    public int bossCount     = 1;
    [Tooltip("Max battle rooms.")]
    public int maxBattleCount   = 10;
    [Tooltip("Max total event rooms across all event types.")]
    public int maxEventCount    = 6;
    [Tooltip("Max empty rooms (remaining space beyond this becomes solid wall).")]
    public int maxEmptyCount    = 8;

    [Header("Event Room Type Weights")]
    [Tooltip("Relative weight for each event type. Higher = more likely to be chosen.")]
    public float weightHeal     = 1f;
    public float weightShop     = 1f;
    public float weightRareLoot = 1f;
    public float weightMerge    = 1f;

    [Header("Floor-Repeat Penalty")]
    [Tooltip("Weight multiplier applied to an event type that appeared last floor (0 = never repeat, 1 = no penalty).")]
    [Range(0f, 1f)]
    public float repeatPenalty  = 0.4f;

    [Header("Preset Usage Probability per Type")]
    [Tooltip("0 = always random rect, 1 = always use preset if available.")]
    [Range(0f,1f)] public float presetChanceBattle   = 0.7f;
    [Range(0f,1f)] public float presetChanceHeal      = 1f;
    [Range(0f,1f)] public float presetChanceShop      = 1f;
    [Range(0f,1f)] public float presetChanceRareLoot  = 1f;
    [Range(0f,1f)] public float presetChanceMerge     = 1f;
    [Range(0f,1f)] public float presetChanceSpawn     = 1f;
    [Range(0f,1f)] public float presetChanceBoss      = 1f;

    [Header("Room Presets")]
    public BSPRoomPreset[] roomPresets;

    [Header("Doors")]
    public int   doorWidth       = 3;
    [Range(0f,1f)]
    public float extraDoorChance = 0.35f;

    [Header("Walls")]
    public float wallHeight     = 2f;
    public float wallThickness  = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material floorMat;
    public Material wallMat;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    // ── Public API ────────────────────────────────────────────────────────────

    public IReadOnlyList<MapNode> Nodes         => _nodes;
    public IReadOnlyList<MapEdge> Edges         => _edges;
    public byte[,]                Matrix        => _matrix;
    public int                    MatrixSize    => matrixSize;
    public MapNode[,]             RoomMapPublic => _roomMap;

    public event Action<IReadOnlyList<MapNode>> OnMapReady;

    // ── Internal ──────────────────────────────────────────────────────────────

    byte[,]    _matrix;
    MapNode[,] _roomMap;
    MapNode[,] _voidOwnerMap;  // which room owns each void cell
    bool[,]    _isDoor;

    List<MapNode> _nodes = new();
    List<MapEdge> _edges = new();

    static readonly Vector2Int[] Dirs = { new(1,0), new(-1,0), new(0,1), new(0,-1) };

    // ── Entry ─────────────────────────────────────────────────────────────────

    void Start() => Generate();

    public void Generate()
    {
        _matrix       = new byte[matrixSize, matrixSize];
        _roomMap       = new MapNode[matrixSize, matrixSize];
        _voidOwnerMap  = new MapNode[matrixSize, matrixSize];
        _isDoor        = new bool[matrixSize, matrixSize];
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

        if (navMeshSurface != null)
            navMeshSurface.BuildNavMesh();

        OnMapReady?.Invoke(_nodes);
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 1 — Fill matrix: place typed rooms in order, then fill remainder
    // ═════════════════════════════════════════════════════════════════════════

    void FillMatrix()
    {
        int spawnCorner = Random.Range(0, 4);
        int bossCorner  = (spawnCorner + 1 + Random.Range(0, 3)) % 4;  // any other corner

        // ── Place all four corner rooms first so random placement can't steal them
        var usedCorners = new HashSet<int> { spawnCorner, bossCorner };
        PlaceTypedRoom(RoomType.Spawn,  minBattleRoomSize, maxBattleRoomSize, presetChanceSpawn,   spawnCorner);
        PlaceTypedRoom(RoomType.Boss,   minBattleRoomSize, maxBattleRoomSize, presetChanceBoss,    bossCorner);
        for (int ci = 0; ci < 4; ci++)
        {
            if (usedCorners.Contains(ci)) continue;
            PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, ci);
        }

        // ── Place at least one battle room directly adjacent to spawn ────────
        PlaceAdjacentBattle();

        // ── Place remaining battle rooms anywhere ─────────────────────────────
        for (int i = 0; i < maxBattleCount - 1; i++)
            PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, -1);

        // ── Place event rooms via weighted random ────────────────────────────
        // Build per-type weights, penalising types seen last floor
        var rm = RunManager.Instance;
        float wHeal     = weightHeal     * (rm != null && rm.WasMissingLastFloor(RoomType.Heal)     ? 1f : repeatPenalty);
        float wShop     = weightShop     * (rm != null && rm.WasMissingLastFloor(RoomType.Shop)     ? 1f : repeatPenalty);
        float wRareLoot = weightRareLoot * (rm != null && rm.WasMissingLastFloor(RoomType.RareLoot) ? 1f : repeatPenalty);
        float wMerge    = weightMerge    * (rm != null && rm.WasMissingLastFloor(RoomType.Merge)    ? 1f : repeatPenalty);

        // Track how many of each type we place (each type max 1 per floor)
        var eventPlaced = new Dictionary<RoomType, int>
        {
            { RoomType.Heal, 0 }, { RoomType.Shop, 0 },
            { RoomType.RareLoot, 0 }, { RoomType.Merge, 0 }
        };

        for (int i = 0; i < maxEventCount; i++)
        {
            // Rebuild weights zeroing out any type already placed
            float h  = eventPlaced[RoomType.Heal]     > 0 ? 0f : wHeal;
            float s  = eventPlaced[RoomType.Shop]     > 0 ? 0f : wShop;
            float r  = eventPlaced[RoomType.RareLoot] > 0 ? 0f : wRareLoot;
            float m  = eventPlaced[RoomType.Merge]    > 0 ? 0f : wMerge;
            float total = h + s + r + m;
            if (total <= 0f) break;  // all types placed

            float roll = Random.Range(0f, total);
            RoomType chosen;
            if ((roll -= h) < 0f)      chosen = RoomType.Heal;
            else if ((roll -= s) < 0f) chosen = RoomType.Shop;
            else if ((roll -= r) < 0f) chosen = RoomType.RareLoot;
            else                       chosen = RoomType.Merge;

            float pChance = chosen switch
            {
                RoomType.Heal     => presetChanceHeal,
                RoomType.Shop     => presetChanceShop,
                RoomType.RareLoot => presetChanceRareLoot,
                _                 => presetChanceMerge,
            };

            bool placed = false;
            // Try preset first
            if (Random.value < pChance && roomPresets != null)
            {
                var compat = new List<BSPRoomPreset>();
                foreach (var p in roomPresets)
                    if (p != null && p.AllowsType(chosen)) compat.Add(p);
                Shuffle(compat);
                foreach (var preset in compat)
                    if (TryPlacePresetRoom(chosen, preset, -1)) { placed = true; break; }
            }
            if (!placed)
                placed = TryPlaceRandomRoom(chosen, minEventRoomSize, maxEventRoomSize, -1);

            if (placed)
            {
                eventPlaced[chosen]++;
                rm?.RegisterEventRoomPlaced(chosen);
            }
        }

        // ── Fill remaining space ─────────────────────────────────────────────
        FillRemaining();
    }

    // Places a battle room flush against one of the spawn room's four faces.
    // Tries all four faces with multiple size attempts on each.
    // Respects minBattleRoomSize/maxBattleRoomSize — no relabeling of existing rooms.
    void PlaceAdjacentBattle()
    {
        MapNode spawnNode = null;
        foreach (var n in _nodes)
            if (n.Type == RoomType.Spawn) { spawnNode = n; break; }
        if (spawnNode == null) return;

        // Build all four faces and shuffle for variety
        var faces = new List<(int faceX, int faceZ, int dirX, int dirZ)>
        {
            (spawnNode.MaxX + 1, spawnNode.MinZ,  1,  0),  // East
            (spawnNode.MinX - 1, spawnNode.MinZ, -1,  0),  // West
            (spawnNode.MinX,     spawnNode.MaxZ + 1, 0,  1),  // North
            (spawnNode.MinX,     spawnNode.MinZ - 1, 0, -1),  // South
        };
        Shuffle(faces);

        // Try every face, every size from max down to min.
        // The matrix is nearly empty at this point so one of these must work.
        foreach (var (faceX, faceZ, dirX, dirZ) in faces)
        {
            for (int sx = maxBattleRoomSize; sx >= minBattleRoomSize; sx--)
            for (int sz = maxBattleRoomSize; sz >= minBattleRoomSize; sz--)
            {
                // Anchor room flush against the spawn face, centered along it
                int ox, oz;
                if (dirX == 1)       { ox = spawnNode.MaxX + 1; oz = spawnNode.MinZ + (spawnNode.Depth - sz) / 2; }
                else if (dirX == -1) { ox = spawnNode.MinX - sx; oz = spawnNode.MinZ + (spawnNode.Depth - sz) / 2; }
                else if (dirZ == 1)  { oz = spawnNode.MaxZ + 1; ox = spawnNode.MinX + (spawnNode.Width - sx) / 2; }
                else                 { oz = spawnNode.MinZ - sz; ox = spawnNode.MinX + (spawnNode.Width - sx) / 2; }

                ox = Mathf.Clamp(ox, 0, matrixSize - sx);
                oz = Mathf.Clamp(oz, 0, matrixSize - sz);

                if (!RectEmpty(ox, oz, sx, sz)) continue;

                StampRoom(ox, oz, sx, sz, null, RoomType.Battle);
                Debug.Log($"[BSPMap] Placed adjacent Battle at ({ox},{oz}) {sx}x{sz}");
                return;
            }
        }

        // Absolute last resort: place minimum size flush to any valid face
        Debug.LogWarning("[BSPMap] Standard adjacent battle failed — using minimum fallback.");
        foreach (var (faceX, faceZ, dirX, dirZ) in faces)
        {
            int sx = minBattleRoomSize, sz = minBattleRoomSize;
            int ox, oz;
            if (dirX == 1)       { ox = spawnNode.MaxX + 1; oz = Mathf.Clamp(spawnNode.CenterZ - sz/2, 0, matrixSize - sz); }
            else if (dirX == -1) { ox = Mathf.Clamp(spawnNode.MinX - sx, 0, matrixSize - sx); oz = Mathf.Clamp(spawnNode.CenterZ - sz/2, 0, matrixSize - sz); }
            else if (dirZ == 1)  { oz = spawnNode.MaxZ + 1; ox = Mathf.Clamp(spawnNode.CenterX - sx/2, 0, matrixSize - sx); }
            else                 { oz = Mathf.Clamp(spawnNode.MinZ - sz, 0, matrixSize - sz); ox = Mathf.Clamp(spawnNode.CenterX - sx/2, 0, matrixSize - sx); }

            if (!RectEmpty(ox, oz, sx, sz)) continue;
            StampRoom(ox, oz, sx, sz, null, RoomType.Battle);
            Debug.Log($"[BSPMap] Fallback adjacent Battle at ({ox},{oz})");
            return;
        }

        Debug.LogError("[BSPMap] Could not place adjacent battle room at all.");
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
            if (!RectEmptyNoGapCheck(ox, oz, preset.sizeX, preset.sizeZ)) return false;
            StampRoom(ox, oz, preset.sizeX, preset.sizeZ, preset, type);
            return true;
        }

        for (int attempt = 0; attempt < 80; attempt++)
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
            // Corner rooms — try a few sizes, all anchored to the exact corner
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int sx = Random.Range(minSz, maxSz + 1);
                int sz = Random.Range(minSz, maxSz + 1);
                GetCornerOrigin(cornerIdx, sx, sz, out int ox, out int oz);
                if (!RectEmptyNoGapCheck(ox, oz, sx, sz)) continue;
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
            SnapToMinPadding(ref ox, ref oz, sx, sz);
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
            case 0:  ox = 0;              oz = 0;              break;  // top-left
            case 1:  ox = matrixSize-sx;  oz = matrixSize-sz;  break;  // bottom-right
            case 2:  ox = matrixSize-sx;  oz = 0;              break;  // top-right
            default: ox = 0;              oz = matrixSize-sz;  break;  // bottom-left
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
                    // Can't fit minimum here — fill the detected rect as solid wall
                    // so it doesn't leave thin strips
                    if (minSz == 1)
                        for (int fx = ox; fx < ox + bestW; fx++)
                            for (int fz = oz; fz < oz + bestH; fz++)
                                if (_matrix[fx, fz] == Cell.Empty)
                                    _matrix[fx, fz] = Cell.Occupied;
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
            Type        = type,
            MinX = ox,           MinZ = oz,
            MaxX = ox + sx - 1,  MaxZ = oz + sz - 1,
            WorldCenter = new Vector3(ox + sx * 0.5f, 0f, oz + sz * 0.5f),
            Preset      = preset,
        };
        _nodes.Add(node);

        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                bool isVoid = preset != null && preset.IsVoid(x - ox, z - oz);
                _matrix[x, z] = Cell.Room;
                _roomMap[x, z] = isVoid ? null : node;
                if (isVoid) _voidOwnerMap[x, z] = node;
            }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 2 — Merge small empty rooms into adjacent neighbours
    // ═════════════════════════════════════════════════════════════════════════

    // Detects any contiguous empty region whose bounding box is 1 cell wide
    // on either axis (a thin strip that the player couldn't meaningfully navigate).
    // Seals those cells as Cell.Occupied so they become solid wall.
    void SealNarrowGaps()
    {
        bool[,] visited = new bool[matrixSize, matrixSize];

        for (int sx = 0; sx < matrixSize; sx++)
            for (int sz = 0; sz < matrixSize; sz++)
            {
                if (visited[sx, sz]) continue;
                byte v = _matrix[sx, sz];
                if (v != Cell.Empty && v != Cell.Occupied) continue;

                // Flood-fill the contiguous empty region
                var region = new List<Vector2Int>();
                var queue  = new Queue<Vector2Int>();
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

                // Seal any empty region whose bounding box is narrower than
                // minEmptyRoomSize on either axis — these are unnavigable thin strips
                int regionW = maxX - minX + 1;
                int regionH = maxZ - minZ + 1;
                bool tooThin = regionW < minEmptyRoomSize || regionH < minEmptyRoomSize;

                if (tooThin)
                {
                    foreach (var cell in region)
                        _matrix[cell.x, cell.y] = Cell.Occupied;
                }
            }
    }

    void MergeSmallEmptyRooms()
    {
        bool anyMerged = true;
        while (anyMerged)
        {
            anyMerged = false;
            foreach (var node in new List<MapNode>(_nodes))
            {
                if (node.Type != RoomType.Unmarked) continue;

                int area = CountCells(node);
                if (area >= mergeAreaThreshold) continue;

                // Find the largest adjacent EMPTY room to merge into
                MapNode best = null;
                int bestShared = 0;

                foreach (var other in _nodes)
                {
                    if (other == node) continue;
                    if (other.Type != RoomType.Unmarked) continue;  // only merge with empty rooms
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
                foreach (var d in new[] { new Vector2Int(1,0), new Vector2Int(0,1) })
                {
                    int nx = x + d.x, nz = z + d.y;
                    if (nx >= matrixSize || nz >= matrixSize) continue;
                    var b = _roomMap[nx, nz];
                    if (b == null || b == a) continue;
                    var key = a.GetHashCode() < b.GetHashCode() ? (a,b) : (b,a);
                    if (!pairShared.ContainsKey(key)) pairShared[key] = new();
                    pairShared[key].Add(new Vector2Int(x, z));
                }
            }

        var parent = new Dictionary<MapNode, MapNode>();
        foreach (var n in _nodes) parent[n] = n;
        MapNode Find(MapNode n) { while (parent[n] != n) { parent[n] = parent[parent[n]]; n = parent[n]; } return n; }
        void Union(MapNode a, MapNode b) { parent[Find(a)] = Find(b); }

        var sorted = new List<KeyValuePair<(MapNode,MapNode), List<Vector2Int>>>(pairShared);
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
            // Spawn room only opens doors toward Battle or Boss rooms
            bool spawnEdge = e.A.Type == RoomType.Spawn || e.B.Type == RoomType.Spawn;
            if (spawnEdge)
            {
                var other = e.A.Type == RoomType.Spawn ? e.B : e.A;
                if (other.Type != RoomType.Battle && other.Type != RoomType.Boss) continue;
            }

            var key = e.A.GetHashCode() < e.B.GetHashCode() ? (e.A, e.B) : (e.B, e.A);
            if (_sharedCells.TryGetValue(key, out var cells))
                PunchDoor(cells);
        }

        EnsureSpawnHasDoor();
    }

    // Guarantees spawn always has at least one door.
    // If no battle/boss neighbour door was punched, relax the rule and
    // force a door to whichever adjacent room shares the most boundary cells.
    void EnsureSpawnHasDoor()
    {
        MapNode spawnNode = null;
        foreach (var n in _nodes)
            if (n.Type == RoomType.Spawn) { spawnNode = n; break; }
        if (spawnNode == null) return;

        // Check if spawn already has a door
        for (int x = spawnNode.MinX; x <= spawnNode.MaxX; x++)
            for (int z = spawnNode.MinZ; z <= spawnNode.MaxZ; z++)
                if (_isDoor[x, z]) return;  // already has a door — nothing to do

        Debug.LogWarning("[BSPMap] Spawn has no door — forcing door to best neighbour.");

        // Find the adjacent room with the most shared boundary cells
        MapNode best = null;
        int bestCount = 0;
        foreach (var kvp in _sharedCells)
        {
            MapNode a = kvp.Key.Item1, b = kvp.Key.Item2;
            MapNode other = null;
            if (a == spawnNode) other = b;
            else if (b == spawnNode) other = a;
            if (other == null) continue;
            if (kvp.Value.Count > bestCount) { bestCount = kvp.Value.Count; best = other; }
        }

        if (best == null) return;

        // Force the edge to exist if it doesn't yet
        AddEdge(spawnNode, best);

        var key = spawnNode.GetHashCode() < best.GetHashCode()
            ? (spawnNode, best) : (best, spawnNode);
        if (_sharedCells.TryGetValue(key, out var cells))
            PunchDoor(cells);
    }

    void PunchDoor(List<Vector2Int> boundary)
    {
        if (boundary.Count == 0) return;

        // Group boundary cells by the exact direction they face room B.
        // A cell can only be in one group — if it faces B in two directions
        // it's a corner touch and is excluded entirely.
        // Key = facing direction (one of the 4 Dirs), Value = cells facing that way.
        var groups = new Dictionary<Vector2Int, List<Vector2Int>>();

        foreach (var c in boundary)
        {
            var owner = _roomMap[c.x, c.y];
            if (owner == null) continue;
            if (owner.Preset != null && owner.Preset.IsVoid(c.x - owner.MinX, c.y - owner.MinZ)) continue;

            var facingDirs = new List<Vector2Int>();
            foreach (var d in Dirs)
            {
                int nx = c.x + d.x, nz = c.y + d.y;
                if (nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize) continue;
                var nb = _roomMap[nx, nz];
                if (nb != null && nb != owner) facingDirs.Add(d);
            }

            // Only include cells that face exactly one direction toward another room.
            // Two directions = corner cell = excluded.
            if (facingDirs.Count != 1) continue;

            var dir = facingDirs[0];
            if (!groups.ContainsKey(dir)) groups[dir] = new List<Vector2Int>();
            groups[dir].Add(c);
        }

        if (groups.Count == 0) return;

        // Find the longest contiguous run across all direction groups.
        // For each group, sort along the perpendicular axis and find the longest run.
        List<Vector2Int> bestRun = null;

        foreach (var kvp in groups)
        {
            var dir   = kvp.Key;
            var cells = kvp.Value;

            // Sort along the axis perpendicular to the facing direction
            // facing X → cells line up along Z axis
            // facing Z → cells line up along X axis
            bool faceX = dir.x != 0;
            cells.Sort((a, b) => faceX ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            // Find longest contiguous run in this group
            var curRun = new List<Vector2Int> { cells[0] };
            var groupBest = new List<Vector2Int> { cells[0] };

            for (int i = 1; i < cells.Count; i++)
            {
                var prev = cells[i - 1];
                var cur  = cells[i];
                // Contiguous = same position on facing axis, adjacent on run axis
                bool sameAxis   = faceX ? cur.x == prev.x : cur.y == prev.y;
                bool adjacent   = faceX ? cur.y == prev.y + 1 : cur.x == prev.x + 1;

                if (sameAxis && adjacent)
                    curRun.Add(cur);
                else
                {
                    if (curRun.Count > groupBest.Count) groupBest = new List<Vector2Int>(curRun);
                    curRun = new List<Vector2Int> { cur };
                }
            }
            if (curRun.Count > groupBest.Count) groupBest = curRun;

            if (bestRun == null || groupBest.Count > bestRun.Count)
                bestRun = groupBest;
        }

        if (bestRun == null || bestRun.Count < doorWidth) return;

        int mid   = bestRun.Count / 2;
        int start = Mathf.Max(0, mid - doorWidth / 2);
        int end   = Mathf.Min(bestRun.Count, start + doorWidth);

        for (int i = start; i < end; i++)
            _isDoor[bestRun[i].x, bestRun[i].y] = true;
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  Step 5 — Geometry
    // ═════════════════════════════════════════════════════════════════════════

    void SpawnGeometry()
    {
        var floorParent = new GameObject("Floors").transform;
        var wallParent  = new GameObject("Walls").transform;
        var voidParent  = new GameObject("VoidBlockers").transform;

        var seenVoid = new HashSet<Vector2Int>();

        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
            {
                if (_matrix[x, z] != Cell.Room) continue;

                var owner = _roomMap[x, z];

                if (owner == null)
                {
                    // Void cell — blocker so player can't enter
                    var key = new Vector2Int(x, z);
                    if (!seenVoid.Contains(key))
                    {
                        seenVoid.Add(key);
                        SpawnVoidBlocker(voidParent, x, z);
                    }

                    // Only wall on sides that face outside this void's owning room
                    var voidOwner = _voidOwnerMap[x, z];
                    foreach (var d in Dirs)
                    {
                        int  nx  = x + d.x, nz = z + d.y;
                        bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                        if (oob) { SpawnWallQuad(wallParent, x, z, d); continue; }

                        // Neighbour belongs to the same owning room (real cell or another void) — no wall
                        bool sameRoom = _roomMap[nx, nz] == voidOwner
                                     || _voidOwnerMap[nx, nz] == voidOwner;
                        if (!sameRoom)
                            SpawnWallQuad(wallParent, x, z, d);
                    }
                    continue;
                }

                SpawnFloorQuad(floorParent, x, z);

                foreach (var d in Dirs)
                {
                    int  nx      = x + d.x, nz = z + d.y;
                    bool oob     = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                    bool nonRoom = !oob && _matrix[nx, nz] != Cell.Room;
                    // Void neighbour (Cell.Room but _roomMap==null): no wall
                    bool voidNb  = !oob && !nonRoom && _roomMap[nx, nz] == null;
                    bool diff    = !oob && !nonRoom && !voidNb && _roomMap[nx, nz] != owner;
                    bool door    = (_isDoor[x, z] || (!oob && !nonRoom && _isDoor[nx, nz])) && diff;

                    if ((oob || nonRoom || diff) && !door)
                        SpawnWallQuad(wallParent, x, z, d);
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
        col.size   = new Vector3(1f, wallHeight * 2f, 1f);
    }

    void SpawnFloorQuad(Transform parent, int x, int z)
    {
        var go = new GameObject($"F_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);
        var pb   = go.AddComponent<ProBuilderMesh>();
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
        Vector3    center     = new(x + 0.5f, 0f, z + 0.5f);
        Vector3    faceOffset = new(facing.x * 0.5f, wallHeight / 2f, facing.y * 0.5f);
        Quaternion rot        = Quaternion.LookRotation(new(-facing.x, 0, -facing.y));

        var go = new GameObject($"W_{x}_{z}_{facing.x}_{facing.y}");
        go.transform.SetParent(parent);
        go.transform.position = center + faceOffset;
        go.transform.rotation = rot;
        go.layer = LayerMask.NameToLayer("Wall");

        float hh = wallHeight / 2f;
        var pb   = go.AddComponent<ProBuilderMesh>();
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
        // The rect itself must be fully empty
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
            {
                if (x >= matrixSize || z >= matrixSize) return false;
                if (_matrix[x, z] != Cell.Empty) return false;
            }

        // No existing room cell may be within 1..(minEmptyRoomSize-1) cells of
        // any edge — gap must be either 0 (flush) or >= minEmptyRoomSize.
        int minGap = minEmptyRoomSize;
        for (int gap = 1; gap < minGap; gap++)
        {
            // West strip
            int wx = ox - gap;
            if (wx >= 0)
                for (int z = oz; z < oz + sz; z++)
                    if (wx < matrixSize && _matrix[wx, z] == Cell.Room) return false;
            // East strip
            int ex = ox + sx + gap - 1;
            if (ex < matrixSize)
                for (int z = oz; z < oz + sz; z++)
                    if (_matrix[ex, z] == Cell.Room) return false;
            // South strip
            int sz2 = oz - gap;
            if (sz2 >= 0)
                for (int x = ox; x < ox + sx; x++)
                    if (sz2 < matrixSize && _matrix[x, sz2] == Cell.Room) return false;
            // North strip
            int nz = oz + sz + gap - 1;
            if (nz < matrixSize)
                for (int x = ox; x < ox + sx; x++)
                    if (_matrix[x, nz] == Cell.Room) return false;
        }
        return true;
    }

    // Same as RectEmpty but skips the gap check — used for corner rooms that
    // must be flush with the matrix edge regardless of nearby rooms.
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

    // If this room would sit 1 or 2 cells away from an existing room on any side,
    // snap it flush (gap = 0) on that side to avoid thin walkable strips.
    void SnapToMinPadding(ref int ox, ref int oz, int sx, int sz, int minGap = 3)
    {
        // Check West side (ox-1 .. ox-minGap)
        for (int gap = 1; gap < minGap; gap++)
        {
            int checkX = ox - gap;
            if (checkX < 0) break;
            bool hasRoom = false;
            for (int z = oz; z < oz + sz && !hasRoom; z++)
                if (checkX < matrixSize && _matrix[checkX, z] == Cell.Room) hasRoom = true;
            if (hasRoom) { ox -= gap; break; }
        }

        // Check East side (ox+sx .. ox+sx+minGap-1)
        for (int gap = 1; gap < minGap; gap++)
        {
            int checkX = ox + sx + gap - 1;
            if (checkX >= matrixSize) break;
            bool hasRoom = false;
            for (int z = oz; z < oz + sz && !hasRoom; z++)
                if (_matrix[checkX, z] == Cell.Room) hasRoom = true;
            if (hasRoom) { ox += gap; break; }
        }

        // Check South side (oz-1 .. oz-minGap)
        for (int gap = 1; gap < minGap; gap++)
        {
            int checkZ = oz - gap;
            if (checkZ < 0) break;
            bool hasRoom = false;
            for (int x = ox; x < ox + sx && !hasRoom; x++)
                if (checkZ < matrixSize && _matrix[x, checkZ] == Cell.Room) hasRoom = true;
            if (hasRoom) { oz -= gap; break; }
        }

        // Check North side (oz+sz .. oz+sz+minGap-1)
        for (int gap = 1; gap < minGap; gap++)
        {
            int checkZ = oz + sz + gap - 1;
            if (checkZ >= matrixSize) break;
            bool hasRoom = false;
            for (int x = ox; x < ox + sx && !hasRoom; x++)
                if (_matrix[x, checkZ] == Cell.Room) hasRoom = true;
            if (hasRoom) { oz += gap; break; }
        }

        // Snap to matrix walls: if within minGap of any edge, push flush
        if (ox > 0 && ox < minGap)                         ox = 0;
        if (oz > 0 && oz < minGap)                         oz = 0;
        if (ox + sx < matrixSize && ox + sx > matrixSize - minGap) ox = matrixSize - sx;
        if (oz + sz < matrixSize && oz + sz > matrixSize - minGap) oz = matrixSize - sz;

        // Final clamp into matrix bounds
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
                Type          = n.Type,
                MatrixOrigin  = new(n.MinX, n.MinZ),
                MatrixCenter  = new(n.CenterX, n.CenterZ),
                Size          = new(n.Width, n.Depth),
                WorldPosition = n.WorldCenter,
                ChosenPrefab  = n.ChosenPrefab,
                RoomObject    = n.RoomObject,
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
                RoomType.Spawn    => Color.green,
                RoomType.Battle   => Color.red,
                RoomType.Boss     => Color.magenta,
                RoomType.Shop     => Color.yellow,
                RoomType.Merge    => Color.cyan,
                RoomType.Heal     => new Color(0.4f, 1f, 0.4f),
                RoomType.RareLoot => new Color(1f, 0.5f, 0f),
                RoomType.Unmarked     => new Color(0.55f, 0.55f, 0.55f, 0.5f),
                _                 => Color.grey
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
                    Vector3 e    = new(x + 0.5f + d.x * 0.5f, 1f, z + 0.5f + d.y * 0.5f);
                    Vector3 size = d.x != 0 ? new(0.05f, 0.15f, 1f) : new(1f, 0.15f, 0.05f);
                    Gizmos.DrawCube(e, size);
                }
            }
    }

    // ── ASCII Matrix Debug ─────────────────────────────────────────────────
    // Right-click the component in the Inspector → "Print Matrix Debug"
    // Each cell prints one character. Room types use letters, doors = +, walls = #

    [ContextMenu("Print Matrix Debug")]
    void PrintMatrixDebug()
    {
        if (_matrix == null || _roomMap == null)
        {
            Debug.LogWarning("[BSPMap] Matrix not generated yet.");
            return;
        }

        // Downsample for readability — print one char per N cells
        int step = Mathf.Max(1, matrixSize / 60);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[BSPMap] Matrix {matrixSize}x{matrixSize} (1 char = {step} cells)");
        sb.AppendLine("Legend: S=Spawn B=Battle X=Boss s=Shop h=Heal r=RareLoot m=Merge .=Unmarked _=empty/wall +=door v=void");
        sb.AppendLine();

        for (int z = 0; z < matrixSize; z += step)
        {
            for (int x = 0; x < matrixSize; x += step)
            {
                char ch;
                if (_matrix[x, z] != Cell.Room)
                {
                    ch = '_';
                }
                else if (_isDoor[x, z])
                {
                    ch = '+';
                }
                else if (_roomMap[x, z] == null)
                {
                    ch = 'v';
                }
                else
                {
                    ch = _roomMap[x, z].Type switch
                    {
                        RoomType.Spawn    => 'S',
                        RoomType.Boss     => 'X',
                        RoomType.Battle   => 'B',
                        RoomType.Shop     => 's',
                        RoomType.Heal     => 'h',
                        RoomType.RareLoot => 'r',
                        RoomType.Merge    => 'm',
                        RoomType.Unmarked => '.',
                        _                 => '?',
                    };
                }
                sb.Append(ch);
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());
    }
}