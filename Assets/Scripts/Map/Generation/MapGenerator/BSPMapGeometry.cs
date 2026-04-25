/*
 * BSPMapGeometry — Office-style dungeon generator
 *
 * Generation pipeline:
 *   1. FillMatrix        — place all typed rooms (corners first, then battle, event, fill);
 *                          FillRemaining uses FindLargestRectangle on all remaining empty cells:
 *                          large enough rect → Unmarked room, too small/narrow → Cell.Occupied seal
 *   2. MergeSmallRooms   — merge Unmarked rooms below mergeAreaThreshold into neighbours
 *   3. BuildConnectivity — scan adjacency, build MST + all-pairs edges, store shared cells
 *   4. PunchDoors        — for each edge punch a door opening; Spawn/Boss restricted
 *   5. SpawnGeometry     — floor quads, wall quads (2-layer), void blockers, sealed cubes
 *
 * Room types:
 *   Marked   = Spawn | Boss | Battle | event types
 *   Event    = Heal | Shop | RareLoot | Merge  (max 1 each per floor, weighted random)
 *   Unmarked = filler space with no content
 *
 * Corner assignment:
 *   Spawn → random corner, Boss → different random corner,
 *   remaining 2 corners → 1 guaranteed Battle + 1 Battle-or-Event (cornerEventChance),
 *   Boss gets an adjacent guard room (BattleRoom) that is the ONLY door to boss.
 */

using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
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
    [Tooltip("Minimum battle rooms guaranteed (uses smaller sizes to fit if needed).")]
    public int minBattleCount = 4;
    [Tooltip("Minimum distinct event room types guaranteed on each floor.")]
    public int minEventCount = 2;

    [Header("Corner Rooms")]
    [Tooltip("Chance the 2nd free corner (not Spawn/Boss) becomes an event room instead of a battle room.")]
    [Range(0f, 1f)] public float cornerEventChance = 0.5f;
    [Range(1f, 10f)] public float cornerPityBoost = 2f;
    [Range(0f, 1f)] public float cornerBattleFallbackChance = 0.1f;

    [Header("Event Room Weights")]
    public float weightHeal = 1f;
    public float weightShop = 1f;
    public float weightRareLoot = 1f;
    public float weightMerge = 1f;
    public float weightFountain = 1f;

    [Header("Floor-Repeat Penalty")]
    [Range(0f, 1f)] public float repeatPenalty = 0.4f;

    [Header("Preset Usage Probability")]
    [Range(0f, 1f)] public float presetChanceBattle = 0.7f;
    [Range(0f, 1f)] public float presetChanceHeal = 1f;
    [Range(0f, 1f)] public float presetChanceShop = 1f;
    [Range(0f, 1f)] public float presetChanceRareLoot = 1f;
    [Range(0f, 1f)] public float presetChanceMerge = 1f;
    [Range(0f, 1f)] public float presetChanceFountain = 1f;
    [Range(0f, 1f)] public float presetChanceSpawn = 1f;
    [Range(0f, 1f)] public float presetChanceBoss = 1f;

    [Header("Room Presets")]
    public BSPRoomPreset[] roomPresets;

    [Header("Doors")]
    public int doorWidth = 3;

    [Header("Walls")]
    [Tooltip("1×1 wall prefab. If assigned, used instead of ProBuilder quads. " +
             "Pivot should be at the base-center of the wall face.")]
    public GameObject wallPrefab;
    [Tooltip("Decorative pillar placed at every corner where a horizontal and vertical wall intersect. " +
             "Pivot should be at the base-center of the pillar.")]
    public GameObject pillarPrefab;
    [Tooltip("Torch prefab placed on eligible inner wall faces. Leave null to skip.")]
    public GameObject torchPrefab;
    [Tooltip("Chance (0–1) per eligible wall face to spawn a torch.")]
    [Range(0f, 1f)]
    public float torchChance = 0.04f;
    [Tooltip("Height offset from the floor for torch placement.")]
    public float torchHeightOffset = 1.2f;
    public float wallHeight = 2f;
    public float wallThickness = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material floorMat;
    public Material wallMat;
    public Material sealedMat;

    [System.Serializable]
    public struct SegmentMaterials
    {
        [Tooltip("Floor material for this segment. Leave null to use the default floorMat.")]
        public Material floorMat;
        [Tooltip("Wall material for this segment. Leave null to use the default wallMat.")]
        public Material wallMat;
    }

    [Tooltip("Per-segment material overrides. Element 0 = segment 1, element 1 = segment 2, etc. " +
             "Segments beyond the array length fall back to the default materials above.")]
    public SegmentMaterials[] segmentMaterials;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    public IReadOnlyList<MapNode> Nodes => _nodes;
    public IReadOnlyList<MapEdge> Edges => _edges;
    public byte[,] Matrix => _matrix;
    public int MatrixSize => matrixSize;
    public MapNode[,] RoomMapPublic => _roomMap;
    public bool[,] IsDoor => _isDoor;

    public event Action<IReadOnlyList<MapNode>> OnMapReady;

    byte[,] _matrix;
    MapNode[,] _roomMap;
    MapNode[,] _voidOwnerMap;
    bool[,] _isDoor;

    List<MapNode> _nodes = new();
    List<MapEdge> _edges = new();

    Dictionary<(MapNode, MapNode), List<Vector2Int>> _sharedCells;

    MapNode _bossGuardNode;   // battle room directly adjacent to boss, sole door to boss


    // Debug: rectangles produced by FillRemaining — stamped rooms vs sealed gaps
    readonly List<RectResult> _fillStamped = new();
    readonly List<RectResult> _fillSealed  = new();

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
        _fillStamped.Clear();
        _fillSealed.Clear();

        FillMatrix();
        MergeSmallEmptyRooms();
        BuildConnectivity();
        PunchDoors();
        SpawnGeometry();

        FindFirstObjectByType<MinimapManager>()
            ?.BuildMinimapFromMatrix(_matrix, matrixSize, _roomMap, ToLegacyRoomNodes(), _edges);

        if (navMeshSurface != null)
        {
            // Use physics colliders so wall MeshColliders block the bake without needing render meshes
            navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            // Ensure the Wall layer is included so NavMeshModifier markers on wall objects are respected
            int wallLayer = LayerMask.NameToLayer("Wall");
            if (wallLayer >= 0)
                navMeshSurface.layerMask |= 1 << wallLayer;
            navMeshSurface.BuildNavMesh();
        }

        OnMapReady?.Invoke(_nodes);
    }



    void FillMatrix()
    {
        int spawnCorner = Random.Range(0, 4);
        int bossCorner  = spawnCorner ^ 1;  // always the diagonally opposite corner

        var rm = RunManager.Instance;
        int effectiveMaxBattle = maxBattleCount + (rm?.EffectiveExtraBattleRoomMin ?? 0);
        int effectiveMaxEvent  = maxEventCount  + (rm?.EffectiveExtraEventRoomMin  ?? 0);

        PlaceTypedRoom(RoomType.Spawn, minBattleRoomSize, maxBattleRoomSize, presetChanceSpawn, spawnCorner);
        PlaceTypedRoom(RoomType.Boss,  minBattleRoomSize, maxBattleRoomSize, presetChanceBoss,  bossCorner);

        var freeCorners = new List<int>();
        for (int ci = 0; ci < 4; ci++)
            if (ci != spawnCorner && ci != bossCorner) freeCorners.Add(ci);
        Shuffle(freeCorners);

        int curFloor = rm?.CurrentFloor ?? 1;
        float c0EventBias = (curFloor % 2 == 1) ? 0.35f : -0.35f;
        float c0EventChance = Mathf.Clamp01(cornerEventChance + c0EventBias);

        if (Random.value < c0EventChance)
        {
            float shopBonus = (curFloor % 2 == 1) ? 2f : 1f;
            var c0Type = PickWeightedCornerEvent(weightHeal, weightShop * shopBonus, weightRareLoot, weightMerge, weightFountain);
            PlaceCornerEventOfType(c0Type, freeCorners[0]);
        }
        else
            PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, freeCorners[0]);

        if (freeCorners.Count > 1)
        {
            float wH = weightHeal     * (rm != null && rm.WasMissingLastFloor(RoomType.Heal)     ? cornerPityBoost : 1f);
            float wS = weightShop     * (rm != null && rm.WasMissingLastFloor(RoomType.Shop)     ? cornerPityBoost : 1f);
            float wR = weightRareLoot * (rm != null && rm.WasMissingLastFloor(RoomType.RareLoot) ? cornerPityBoost : 1f);
            float wM = weightMerge    * (rm != null && rm.WasMissingLastFloor(RoomType.Merge)    ? cornerPityBoost : 1f);
            float wF = weightFountain * (rm != null && rm.WasMissingLastFloor(RoomType.Fountain) ? cornerPityBoost : 1f);
            if (_nodes.Exists(n => n.Type == RoomType.Heal))     wH = 0f;
            if (_nodes.Exists(n => n.Type == RoomType.Shop))     wS = 0f;
            if (_nodes.Exists(n => n.Type == RoomType.RareLoot)) wR = 0f;
            if (_nodes.Exists(n => n.Type == RoomType.Merge))    wM = 0f;
            if (_nodes.Exists(n => n.Type == RoomType.Fountain)) wF = 0f;
            float c1Total = wH + wS + wR + wM + wF;
            bool anyEventExists = _nodes.Exists(n =>
                n.Type == RoomType.Heal || n.Type == RoomType.Shop || n.Type == RoomType.RareLoot ||
                n.Type == RoomType.Merge || n.Type == RoomType.Fountain);
            if (c1Total <= 0f || (anyEventExists && Random.value < cornerBattleFallbackChance))
                PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, freeCorners[1]);
            else
                PlaceCornerEventOfType(PickWeightedCornerEvent(wH, wS, wR, wM, wF), freeCorners[1]);
        }

        PlaceAdjacentBattle();
        PlaceBossGuardRoom();     // battle room touching boss — becomes the only door to boss

        for (int i = 0; i < effectiveMaxBattle - 1; i++)
            PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, -1);

        float wHeal = weightHeal * (rm != null && rm.WasMissingLastFloor(RoomType.Heal) ? 1f : repeatPenalty);
        float wShop = weightShop * (rm != null && rm.WasMissingLastFloor(RoomType.Shop) ? 1f : repeatPenalty);
        float wRare = weightRareLoot * (rm != null && rm.WasMissingLastFloor(RoomType.RareLoot) ? 1f : repeatPenalty);
        float wMrge = weightMerge * (rm != null && rm.WasMissingLastFloor(RoomType.Merge) ? 1f : repeatPenalty);
        float wFoun = weightFountain * (rm != null && rm.WasMissingLastFloor(RoomType.Fountain) ? 1f : repeatPenalty);

        var placed = new Dictionary<RoomType, int>
            { { RoomType.Heal, 0 }, { RoomType.Shop, 0 }, { RoomType.RareLoot, 0 }, { RoomType.Merge, 0 }, { RoomType.Fountain, 0 } };

        foreach (var n in _nodes)
            if (placed.ContainsKey(n.Type)) placed[n.Type]++;

        int preExisting = 0;
        foreach (var kvp in placed) preExisting += kvp.Value;

        int evtPlaced  = preExisting;
        int evtAttempt = 0;
        int evtMaxAttempts = (effectiveMaxEvent - preExisting) * 4;
        while (evtPlaced < effectiveMaxEvent && evtAttempt++ < evtMaxAttempts)
        {
            float h = placed[RoomType.Heal]     > 0 ? 0f : wHeal;
            float s = placed[RoomType.Shop]     > 0 ? 0f : wShop;
            float r = placed[RoomType.RareLoot] > 0 ? 0f : wRare;
            float m = placed[RoomType.Merge]    > 0 ? 0f : wMrge;
            float f = placed[RoomType.Fountain] > 0 ? 0f : wFoun;
            float evtTotal = h + s + r + m + f;
            if (evtTotal <= 0f) break;  // all eligible types already placed

            float evtRoll = Random.Range(0f, evtTotal);
            RoomType chosen;
            if      ((evtRoll -= h) < 0f) chosen = RoomType.Heal;
            else if ((evtRoll -= s) < 0f) chosen = RoomType.Shop;
            else if ((evtRoll -= r) < 0f) chosen = RoomType.RareLoot;
            else if ((evtRoll -= m) < 0f) chosen = RoomType.Merge;
            else                          chosen = RoomType.Fountain;

            float pChance = chosen switch
            {
                RoomType.Heal     => presetChanceHeal,
                RoomType.Shop     => presetChanceShop,
                RoomType.RareLoot => presetChanceRareLoot,
                RoomType.Fountain => presetChanceFountain,
                _                 => presetChanceMerge,
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
            if (didPlace) { placed[chosen]++; evtPlaced++; rm?.RegisterEventRoomPlaced(chosen); }
        }

        int effectiveMinBattle = minBattleCount + (RunManager.Instance?.EffectiveExtraBattleRoomMin ?? 0);
        int bc = 0;
        foreach (var n in _nodes) if (n.Type == RoomType.Battle) bc++;
        for (int i = bc; i < effectiveMinBattle; i++)
        {
            if (!TryPlaceRandomRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, -1))
                TryPlaceRandomRoom(RoomType.Battle, minBattleRoomSize, minBattleRoomSize + 4, -1);
        }

        int effectiveMinEvent = minEventCount + (RunManager.Instance?.EffectiveExtraEventRoomMin ?? 0);
        int ec = 0;
        foreach (var kvp in placed) ec += kvp.Value;
        if (ec < effectiveMinEvent)
        {
            int gAttempt = 0, gMaxAttempts = effectiveMinEvent * 6;
            while (ec < effectiveMinEvent && gAttempt++ < gMaxAttempts)
            {
                float gh = placed[RoomType.Heal]     > 0 ? 0f : wHeal;
                float gs = placed[RoomType.Shop]     > 0 ? 0f : wShop;
                float gr = placed[RoomType.RareLoot] > 0 ? 0f : wRare;
                float gm = placed[RoomType.Merge]    > 0 ? 0f : wMrge;
                float gf = placed[RoomType.Fountain] > 0 ? 0f : wFoun;
                float gTotal = gh + gs + gr + gm + gf;
                if (gTotal <= 0f) break;

                float gRoll = Random.Range(0f, gTotal);
                RoomType et;
                if      ((gRoll -= gh) < 0f) et = RoomType.Heal;
                else if ((gRoll -= gs) < 0f) et = RoomType.Shop;
                else if ((gRoll -= gr) < 0f) et = RoomType.RareLoot;
                else if ((gRoll -= gm) < 0f) et = RoomType.Merge;
                else                         et = RoomType.Fountain;

                if (TryPlaceRandomRoom(et, minEventRoomSize, maxEventRoomSize, -1))
                { placed[et]++; ec++; rm?.RegisterEventRoomPlaced(et); }
            }
        }

        FillWithSmallTypedRooms(placed, effectiveMaxEvent);

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
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int sx = Random.Range(minBattleRoomSize, maxBattleRoomSize + 1);
                int sz = Random.Range(minBattleRoomSize, maxBattleRoomSize + 1);
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

    void PlaceBossGuardRoom()
    {
        _bossGuardNode = null;
        MapNode boss = null;
        foreach (var n in _nodes) if (n.Type == RoomType.Boss) { boss = n; break; }
        if (boss == null) return;

        var faces = new List<(int dx, int dz)> { (1, 0), (-1, 0), (0, 1), (0, -1) };
        Shuffle(faces);

        foreach (var (dx, dz) in faces)
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int sx = Random.Range(minBattleRoomSize, maxBattleRoomSize + 1);
                int sz = Random.Range(minBattleRoomSize, maxBattleRoomSize + 1);
                int ox, oz;
                if (dx == 1)       { ox = boss.MaxX + 1; oz = boss.MinZ + (boss.Depth - sz) / 2; }
                else if (dx == -1) { ox = boss.MinX - sx; oz = boss.MinZ + (boss.Depth - sz) / 2; }
                else if (dz == 1)  { oz = boss.MaxZ + 1; ox = boss.MinX + (boss.Width - sx) / 2; }
                else               { oz = boss.MinZ - sz; ox = boss.MinX + (boss.Width - sx) / 2; }

                ox = Mathf.Clamp(ox, 0, matrixSize - sx);
                oz = Mathf.Clamp(oz, 0, matrixSize - sz);
                if (!RectEmpty(ox, oz, sx, sz)) continue;
                StampRoom(ox, oz, sx, sz, null, RoomType.Battle);
                _bossGuardNode = _nodes[_nodes.Count - 1];
                return;
            }
    }

    void PlaceCornerEventOfType(RoomType type, int cornerIdx)
    {
        float pChance = type switch
        {
            RoomType.Heal     => presetChanceHeal,
            RoomType.Shop     => presetChanceShop,
            RoomType.RareLoot => presetChanceRareLoot,
            RoomType.Fountain => presetChanceFountain,
            _                 => presetChanceMerge,
        };

        bool didPlace = false;
        if (Random.value < pChance && roomPresets != null)
        {
            var compat = new List<BSPRoomPreset>();
            foreach (var p in roomPresets) if (p != null && p.AllowsType(type)) compat.Add(p);
            Shuffle(compat);
            foreach (var preset in compat)
                if (TryPlacePresetRoom(type, preset, cornerIdx)) { didPlace = true; break; }
        }
        if (!didPlace) didPlace = TryPlaceRandomRoom(type, minEventRoomSize, maxEventRoomSize, cornerIdx);
        if (!didPlace) PlaceTypedRoom(RoomType.Battle, minBattleRoomSize, maxBattleRoomSize, presetChanceBattle, cornerIdx);
        else RunManager.Instance?.RegisterEventRoomPlaced(type);
    }

    void FillWithSmallTypedRooms(Dictionary<RoomType, int> alreadyPlaced, int eventCap)
    {
        int smallBattle = Mathf.Max(minBattleRoomSize, (minBattleRoomSize + maxBattleRoomSize) / 2);
        int smallEvent  = Mathf.Max(minEventRoomSize,  (minEventRoomSize  + maxEventRoomSize)  / 2);

        int bc = 0;
        foreach (var n in _nodes) if (n.Type == RoomType.Battle) bc++;
        for (int i = bc; i < maxBattleCount; i++)
            if (!TryPlaceRandomRoom(RoomType.Battle, minBattleRoomSize, smallBattle, -1)) break;

        var rm = RunManager.Instance;
        var eventTypes = new List<RoomType>
            { RoomType.Heal, RoomType.Shop, RoomType.RareLoot, RoomType.Merge, RoomType.Fountain };
        Shuffle(eventTypes);
        foreach (var et in eventTypes)
        {
            int currentTotal = 0;
            foreach (var kvp in alreadyPlaced) currentTotal += kvp.Value;
            if (currentTotal >= eventCap) break;

            if (alreadyPlaced.TryGetValue(et, out int cnt) && cnt > 0) continue;
            if (TryPlaceRandomRoom(et, minEventRoomSize, smallEvent, -1))
            { alreadyPlaced[et] = 1; rm?.RegisterEventRoomPlaced(et); }
        }
    }

    void PlaceTypedRoom(RoomType type, int minSz, int maxSz, float presetChance, int cornerIdx)
    {

        if (type == RoomType.Boss)
        {
            int floor      = RunManager.Instance?.CurrentFloor ?? 1;
            var pool       = EnemyPoolManager.Instance;
            int fps        = pool != null ? pool.floorsPerSegment : 3;
            int floorInSeg = ((floor - 1) % fps) + 1;
            bool isTrueBoss = fps == 1 || floorInSeg == fps;

            if (isTrueBoss)
            {
                var config = pool?.GetBossConfig(floor);
                if (config?.roomPreset != null)
                    if (TryPlacePresetRoom(type, config.roomPreset, cornerIdx)) return;
            }
        }

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
        while (true)
        {
            // Collect every remaining empty cell
            var empty = new HashSet<Vector2Int>();
            for (int x = 0; x < matrixSize; x++)
                for (int z = 0; z < matrixSize; z++)
                    if (_matrix[x, z] == Cell.Empty)
                        empty.Add(new Vector2Int(x, z));

            if (empty.Count == 0) break;

            var rect = FindLargestRectangle(empty);
            if (rect.width <= 0 || rect.height <= 0) break;   // safety — should never happen

            // Too narrow or too small → seal every cell in the rectangle
            if (rect.width < sealMinWidth || rect.height < sealMinWidth || rect.width * rect.height < sealMinArea)
            {
                for (int x = rect.x; x < rect.x + rect.width; x++)
                    for (int z = rect.z; z < rect.z + rect.height; z++)
                        _matrix[x, z] = Cell.Occupied;
                _fillSealed.Add(rect);
            }
            else
            {
                // Pick a random size within the found rectangle (capped at inspector maxima)
                int sw = Mathf.Min(rect.width,  Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1));
                int sh = Mathf.Min(rect.height, Random.Range(minEmptyRoomSize, maxEmptyRoomSize + 1));
                _fillStamped.Add(new RectResult { x = rect.x, z = rect.z, width = sw, height = sh });
                StampRoom(rect.x, rect.z, sw, sh, null, RoomType.Unmarked);
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
                    _matrix[x, z] = Cell.Occupied;  
                    _roomMap[x, z] = node;          
                }
                else
                {
                    _matrix[x, z] = Cell.Room;
                    _roomMap[x, z] = isVoid ? null : node;
                    if (isVoid) _voidOwnerMap[x, z] = node;
                }
            }
    }

    struct RectResult { public int x, z, width, height; }

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

   

    void PunchDoors()
    {
        foreach (var kvp in _sharedCells)
        {
            var (a, b) = kvp.Key;

            // Spawn room: only doors to Battle or Boss
            bool involvesSpawn = a.Type == RoomType.Spawn || b.Type == RoomType.Spawn;
            if (involvesSpawn)
            {
                var other = a.Type == RoomType.Spawn ? b : a;
                if (other.Type != RoomType.Battle && other.Type != RoomType.Boss) continue;
            }

            // Boss room: only door allowed is to the guard room
            bool involvesBoss = a.Type == RoomType.Boss || b.Type == RoomType.Boss;
            if (involvesBoss && _bossGuardNode != null)
            {
                var other = a.Type == RoomType.Boss ? b : a;
                if (other != _bossGuardNode) continue;
            }

            PunchDoor(kvp.Value);
        }
        EnsureCornerRoomHasDoor(RoomType.Spawn);
        EnsureBossGuardDoor();
    }

    void EnsureBossGuardDoor()
    {
        if (_bossGuardNode == null) return;
        MapNode boss = null;
        foreach (var n in _nodes) if (n.Type == RoomType.Boss) { boss = n; break; }
        if (boss == null) return;

        for (int x = boss.MinX; x <= boss.MaxX; x++)
            for (int z = boss.MinZ; z <= boss.MaxZ; z++)
                if (_isDoor[x, z]) return;

        AddEdge(boss, _bossGuardNode);
        var key = boss.GetHashCode() < _bossGuardNode.GetHashCode()
            ? (boss, _bossGuardNode) : (_bossGuardNode, boss);
        if (_sharedCells.TryGetValue(key, out var cells)) PunchDoor(cells);
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

    // Returns the floor/wall materials for the current segment, falling back to defaults.
    (Material floor, Material wall) ActiveSegmentMaterials()
    {
        int floor      = RunManager.Instance?.CurrentFloor ?? 1;
        int fps        = EnemyPoolManager.Instance?.floorsPerSegment ?? 3;
        int segIdx     = (floor - 1) / fps;  // 0-based

        if (segmentMaterials != null && segIdx < segmentMaterials.Length)
        {
            var sm = segmentMaterials[segIdx];
            return (sm.floorMat != null ? sm.floorMat : floorMat,
                    sm.wallMat  != null ? sm.wallMat  : wallMat);
        }
        return (floorMat, wallMat);
    }

    void SpawnGeometry()
    {
        var (activeFlorMat, activeWallMat) = ActiveSegmentMaterials();
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
                        if (oob) { SpawnWallQuad(wallP, x, z, d, activeWallMat); continue; }
                        if (_roomMap[nx, nz] != vo && _voidOwnerMap[nx, nz] != vo)
                            SpawnWallQuad(wallP, x, z, d, activeWallMat);
                    }
                    continue;
                }

                SpawnFloorQuad(floorP, x, z, owner, activeFlorMat);
                foreach (var d in Dirs)
                {
                    int nx = x + d.x, nz = z + d.y;
                    bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;
                    bool nonRoom = !oob && _matrix[nx, nz] != Cell.Room;
                    bool voidNb = !oob && !nonRoom && _roomMap[nx, nz] == null;
                    bool voidSameOwner = voidNb && _voidOwnerMap[nx, nz] == owner;
                    bool diff = !oob && !nonRoom && !voidNb && _roomMap[nx, nz] != owner;
                    bool door = (_isDoor[x, z] || (!oob && !nonRoom && _isDoor[nx, nz])) && diff;

                    // Only spawn the shared wall from the positive-direction side to avoid duplicates.
                    // The neighbour cell will handle it from its own (negative) direction pass.
                    bool skipDiff = diff && !door && (d.x < 0 || (d.x == 0 && d.y < 0));

                    if (!skipDiff && !door && (oob || nonRoom || diff || (voidNb && !voidSameOwner)))
                        SpawnWallQuad(wallP, x, z, d, activeWallMat);
                }
            }

        if (pillarPrefab != null)
        {
            var pillarP = new GameObject("Pillars").transform;
            SpawnPillars(pillarP, activeWallMat);
        }

        if (torchPrefab != null)
        {
            var torchP = new GameObject("Torches").transform;
            SpawnTorches(torchP);
        }

        // ── Static batching — collapses same-material renderers to 1 draw call each ──
        StaticBatchingUtility.Combine(floorP.gameObject);
        StaticBatchingUtility.Combine(wallP.gameObject);
        StaticBatchingUtility.Combine(sealedP.gameObject);
    }

    // Places a decorative pillar at every corner where both a horizontal wall (±Z face)
    // and a vertical wall (±X face) meet.  Corner (cx,cz) sits at world (cx, _, cz).
    void SpawnPillars(Transform parent, Material mat)
    {
        for (int cx = 0; cx <= matrixSize; cx++)
        for (int cz = 0; cz <= matrixSize; cz++)
        {
            // Wall running perpendicular to X at world X=cx:
            //   boundary between col (cx-1) and col (cx), either in the row below or above.
            bool hasXWall = WallBoundaryExists(cx - 1, cz - 1, cx, cz - 1)
                         || WallBoundaryExists(cx - 1, cz,     cx, cz);

            // Wall running perpendicular to Z at world Z=cz:
            //   boundary between row (cz-1) and row (cz), either in the col to the left or right.
            bool hasZWall = WallBoundaryExists(cx - 1, cz - 1, cx - 1, cz)
                         || WallBoundaryExists(cx,     cz - 1, cx,     cz);

            if (hasXWall && hasZWall)
            {
                var inst = Object.Instantiate(pillarPrefab,
                    new Vector3(cx, 0f, cz),
                    Quaternion.identity,
                    parent);

                if (mat != null)
                    foreach (var r in inst.GetComponentsInChildren<Renderer>())
                        r.sharedMaterial = mat;
            }
        }
    }

    // Spawns torches on the inner face of walls bordering room cells.
    // Eligible face: room cell neighbour is non-room (wall/OOB/Occupied), not a door, random chance passes.
    void SpawnTorches(Transform parent)
    {
        var dirs = new[] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

        for (int x = 0; x < matrixSize; x++)
        for (int z = 0; z < matrixSize; z++)
        {
            if (_matrix[x, z] != Cell.Room) continue;
            if (_isDoor[x, z]) continue;                          // skip door cells

            foreach (var d in dirs)
            {
                int nx = x + d.x, nz = z + d.y;
                bool oob = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize;

                // Must border a solid wall (OOB, Occupied, or non-Room)
                if (!oob && _matrix[nx, nz] == Cell.Room) continue;
                if (!oob && _isDoor[nx, nz]) continue;            // skip door-adjacent faces

                if (Random.value > torchChance) continue;

                // World position: on the wall face, slightly inset toward the room
                float wx = x + 0.5f + d.x * 0.48f;
                float wz = z + 0.5f + d.y * 0.48f;
                var pos = new Vector3(wx, torchHeightOffset, wz);

                // Face torch INTO the room (opposite to wall normal)
                var rot = Quaternion.LookRotation(new Vector3(-d.x, 0f, -d.y));

                Object.Instantiate(torchPrefab, pos, rot, parent);
            }
        }
    }

    MapNode EffectiveRoom(int x, int z)
        => _roomMap[x, z] ?? _voidOwnerMap[x, z];

    // Returns true if a wall face exists between two adjacent cells —
    // i.e. they belong to different spaces (one void/empty, or different rooms).
    bool WallBoundaryExists(int ax, int az, int bx, int bz)
    {
        bool aOccupied = ax >= 0 && az >= 0 && ax < matrixSize && az < matrixSize
                      && (_matrix[ax, az] == Cell.Room || _matrix[ax, az] == Cell.Occupied);
        bool bOccupied = bx >= 0 && bz >= 0 && bx < matrixSize && bz < matrixSize
                      && (_matrix[bx, bz] == Cell.Room || _matrix[bx, bz] == Cell.Occupied);

        if (!aOccupied && !bOccupied) return false;   // both empty — no wall
        if (aOccupied != bOccupied)   return true;    // solid meets empty — wall

        return EffectiveRoom(ax, az) != EffectiveRoom(bx, bz);
    }

    void SpawnVoidBlocker(Transform parent, int x, int z)
    {
        var go = new GameObject($"Void_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x + 0.5f, 0f, z + 0.5f);
        go.layer = LayerMask.NameToLayer("Barrier");
        var col = go.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, wallHeight / 2f, 0f);
        col.size = new Vector3(1f, wallHeight * 2f, 1f);
    }

    void SpawnSealedCube(Transform parent, int x, int z)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Sealed_{x}_{z}";
        go.transform.SetParent(parent);
        go.transform.position  = new Vector3(x + 0.5f, wallHeight * 0.5f, z + 0.5f);
        go.transform.localScale = new Vector3(1f, wallHeight, 1f);
        go.layer   = LayerMask.NameToLayer("Wall");
        go.isStatic = true;
        go.GetComponent<MeshRenderer>().sharedMaterial = sealedMat;
    }

    void SpawnFloorQuad(Transform parent, int x, int z, MapNode owner, Material mat)
    {
        float depth = Mathf.Abs(floorThickness);

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"F_{x}_{z}";
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(x + 0.5f, floorThickness * 0.5f, z + 0.5f);
        go.transform.localScale = new Vector3(1f, depth, 1f);
        go.layer    = LayerMask.NameToLayer("Ground");
        go.isStatic = true;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        bool needsCollider = owner != null &&
            (owner.Type == RoomType.Battle   ||
             owner.Type == RoomType.Boss     ||
             owner.Type == RoomType.RareLoot);
        if (!needsCollider)
            Object.Destroy(go.GetComponent<BoxCollider>());
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing, Material mat)
    {
        if (wallPrefab != null)
        {
            SpawnWallPrefabTile(parent, x, z, facing, mat);
            return;
        }

        float totalH  = wallHeight + Mathf.Abs(floorThickness);
        float centerY = floorThickness + totalH * 0.5f;
        bool  faceX   = facing.x != 0;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"W_{x}_{z}_{facing.x}_{facing.y}";
        go.transform.SetParent(parent);
        go.transform.position   = new Vector3(x + 0.5f + facing.x * 0.5f, centerY, z + 0.5f + facing.y * 0.5f);
        go.transform.localScale = faceX
            ? new Vector3(Mathf.Max(0.05f, wallThickness), totalH, 1f)
            : new Vector3(1f, totalH, Mathf.Max(0.05f, wallThickness));
        go.layer    = LayerMask.NameToLayer("Wall");
        go.isStatic = true;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        var mod = go.AddComponent<NavMeshModifier>();
        mod.overrideArea = true;
        mod.area = NavMesh.GetAreaFromName("Not Walkable");
    }


    void SpawnWallPrefabTile(Transform parent, int x, int z, Vector2Int facing, Material mat)
    {
        float totalH = wallHeight + Mathf.Abs(floorThickness);
        int tiles = Mathf.CeilToInt(totalH);
        float startY = floorThickness;

        float faceX = x + 0.5f + facing.x * 0.5f;
        float faceZ = z + 0.5f + facing.y * 0.5f;

        Quaternion rot = Quaternion.LookRotation(new Vector3(facing.x, 0f, facing.y));

        for (int i = 0; i < tiles; i++)
        {
            float tileY = startY + i;
            var inst = Object.Instantiate(
                wallPrefab,
                new Vector3(faceX, tileY, faceZ),
                rot,
                parent);
            inst.name = $"W_{x}_{z}_{facing.x}_{facing.y}_{i}";
            inst.layer = LayerMask.NameToLayer("Wall");

            if (mat != null)
                foreach (var r in inst.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = mat;

            var mod = inst.AddComponent<NavMeshModifier>();
            mod.overrideArea = true;
            mod.area = NavMesh.GetAreaFromName("Not Walkable");
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



    RoomType PickWeightedCornerEvent(float wH, float wS, float wR, float wM, float wF)
    {
        float total = wH + wS + wR + wM + wF;
        if (total <= 0f) return RoomType.Heal;
        float roll = Random.Range(0f, total);
        if ((roll -= wH) < 0f) return RoomType.Heal;
        if ((roll -= wS) < 0f) return RoomType.Shop;
        if ((roll -= wR) < 0f) return RoomType.RareLoot;
        if ((roll -= wM) < 0f) return RoomType.Merge;
        return RoomType.Fountain;
    }

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
                RoomType.Fountain => new Color(0.3f, 0.6f, 1f, 1f),
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

        const float gizY = 2f;
        Gizmos.color = new Color(0f, 1f, 1f, 0.55f);
        foreach (var r in _fillStamped)
            Gizmos.DrawWireCube(
                new Vector3(r.x + r.width  * 0.5f, gizY, r.z + r.height * 0.5f),
                new Vector3(r.width, 0.15f, r.height));

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.7f);
        foreach (var r in _fillSealed)
            Gizmos.DrawWireCube(
                new Vector3(r.x + r.width  * 0.5f, gizY, r.z + r.height * 0.5f),
                new Vector3(r.width, 0.15f, r.height));
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
                            RoomType.Fountain => 'f',
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
