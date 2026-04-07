using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using Random = UnityEngine.Random;

public enum RoomType { None, Spawn, Battle, Boss, Shop, Merge, Heal, RareLoot }

public static class Cell
{
    public const byte Empty = 0;
    public const byte Corridor = 1;
    public const byte Room = 2;
    public const byte Occupied = 3;
}

[Serializable]
public class MapNode
{
    public RoomType Type;
    public int MinX, MinZ, MaxX, MaxZ;
    public Vector3 WorldCenter;
    public GameObject ChosenPrefab;
    public GameObject RoomObject;

    [NonSerialized] public List<MapEdge> Edges = new();
    [NonSerialized] public RoomNode LegacyNode;

    public int CenterX => (MinX + MaxX) / 2;
    public int CenterZ => (MinZ + MaxZ) / 2;
    public int Width => MaxX - MinX + 1;
    public int Depth => MaxZ - MinZ + 1;
}

public class MapEdge
{
    public MapNode A;
    public MapNode B;

    public List<List<Vector2Int>> Segments = new();
}

[Serializable]
public class RoomNode
{
    public RoomType Type;
    public Vector2Int MatrixOrigin;
    public Vector2Int MatrixCenter;
    public Vector2Int Size;
    public Vector3 WorldPosition;
    public GameObject ChosenPrefab;
    public GameObject RoomObject;
    [NonSerialized] public List<RoomNode> Neighbors = new();
}

public class MapGeometry : MonoBehaviour
{

    [Header("Room Prefabs")]
    public GameObject[] spawnRoomPrefabs;
    public GameObject[] battleRoomPrefabs;
    public GameObject[] bossRoomPrefabs;
    public GameObject[] shopRoomPrefabs;
    public GameObject[] healRoomPrefabs;
    public GameObject[] mergeRoomPrefabs;
    public GameObject[] rareLootRoomPrefabs;

    [Header("Matrix")]
    public int matrixSize = 150;
    public int minRoomSize = 7;
    public int maxRoomSize = 15;

    [Header("Boss Room")]
    public int bossRoomSize = 13;
    public int cornerMargin = 4;

    [Header("Main Path")]
    [Range(2, 8)] public int mainPathBattleRooms = 4;

    [Header("Fill Placement")]
    public int maxPlacementAttempts = 80;
    public int roomPadding = 3;
    [Range(0.1f, 0.6f)]
    public float fillRoomBudget = 0.35f;
    [Tooltip("Max extra Battle rooms placed outside the main path. -1 = unlimited.")]
    public int maxExtraBattleRooms = 6;
    [Tooltip("Max extra Event rooms (Heal/Shop/RareLoot/Merge) placed outside the main path. -1 = unlimited.")]
    public int maxExtraEventRooms = 4;

    [Header("Event Room Weights")]
    public float weightBattle = 4f;
    public float weightHeal = 1f;
    public float weightShop = 1f;
    public float weightRareLoot = 1f;
    public float weightMerge = 1f;

    [Header("Corridors")]
    public int corridorWidth = 3;
    [Range(0f, 1f)]
    public float straightLineThreshold = 0.30f;
    [Range(0f, 1f)]
    public float coverageThreshold = 0.70f;

    [Header("Walls")]
    public float wallHeight = 2f;
    public float wallThickness = 0.01f;
    public float floorThickness = -50f;

    [Header("Materials")]
    public Material corridorMat;
    public Material wallMat;

    public IReadOnlyList<MapNode> Nodes => _nodes;
    public IReadOnlyList<MapEdge> Edges => _edges;
    public byte[,] Matrix => _matrix;
    public int MatrixSize => matrixSize;

    public event Action<IReadOnlyList<MapNode>> OnMapReady;

    byte[,] _matrix;
    List<MapNode> _nodes = new();
    List<MapEdge> _edges = new();
    MapNode _spawnNode;
    MapNode _bossNode;

    void Start() => GenerateMap();

    public void GenerateMap()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _nodes.Clear();
        _edges.Clear();

        int spawnCorner = PickRandomCorner();
        _spawnNode = PlaceSpawnInCorner(spawnCorner);

        int bossCorner = OppositeCorner(spawnCorner);
        _bossNode = PlaceBossInCorner(bossCorner);

        BuildMainPath();

        FillRemainingRooms();

        BuildEdges();

        StampAllCorridors();

        SpawnCorridorGeometry();
        SpawnAllWalls();

        FindFirstObjectByType<MinimapManager>()
            ?.BuildMinimapFromMatrix(_matrix, matrixSize, ToLegacyRoomNodes(), _edges);

        OnMapReady?.Invoke(_nodes);
    }

    int PickRandomCorner() => Random.Range(0, 4);

    int OppositeCorner(int c)
    {

        return c ^ 3;
    }

    MapNode PlaceBossInCorner(int corner)
    {
        bool right = (corner & 1) != 0;
        bool top = (corner & 2) != 0;

        int ox = right ? matrixSize - cornerMargin - bossRoomSize : cornerMargin;
        int oz = top ? matrixSize - cornerMargin - bossRoomSize : cornerMargin;

        if (!FitsInMatrix(ox, oz, bossRoomSize, bossRoomSize))
        {
            return null;
        }

        return StampAndCreateNode(RoomType.Boss, RandomFrom(bossRoomPrefabs), ox, oz, bossRoomSize, bossRoomSize);
    }

    MapNode PlaceSpawnInCorner(int corner)
    {
        int zoneW = matrixSize / 5;

        bool right = (corner & 1) != 0;
        bool top = (corner & 2) != 0;

        int regionMinX = right ? matrixSize - cornerMargin - zoneW : cornerMargin;
        int regionMinZ = top ? matrixSize - cornerMargin - zoneW : cornerMargin;
        int regionMaxX = regionMinX + zoneW;
        int regionMaxZ = regionMinZ + zoneW;

        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            var prefab = RandomFrom(spawnRoomPrefabs);
            var preset = prefab.GetComponent<RoomPreset>();
            int sx = OddClamp(preset != null ? Mathf.RoundToInt(preset.roomSize.x) : RandomSize());
            int sz = OddClamp(preset != null ? Mathf.RoundToInt(preset.roomSize.y) : RandomSize());

            int ox = Random.Range(regionMinX, Mathf.Max(regionMinX + 1, regionMaxX - sx));
            int oz = Random.Range(regionMinZ, Mathf.Max(regionMinZ + 1, regionMaxZ - sz));

            if (!FitsInMatrix(ox, oz, sx, sz)) continue;
            if (!AreaFree(ox, oz, sx, sz)) continue;

            return StampAndCreateNode(RoomType.Spawn, prefab, ox, oz, sx, sz);
        }

        return null;
    }

    void BuildMainPath()
    {
        var spawnC = new Vector2Int(_spawnNode.CenterX, _spawnNode.CenterZ);
        var bossC = new Vector2Int(_bossNode.CenterX, _bossNode.CenterZ);
        int n = mainPathBattleRooms;
        MapNode prev = _spawnNode;

        for (int i = 1; i <= n; i++)
        {
            float t = (float)i / (n + 1);
            var hint = ClampToMatrix(Vector2Int.RoundToInt(Vector2.Lerp(spawnC, bossC, t)), maxRoomSize + roomPadding);

            var node = TryPlaceNear(RoomType.Battle, battleRoomPrefabs, hint, scatter: 10);
            if (node == null) { Debug.LogWarning($"[MapGeometry] Main-path battle node {i}/{n} failed."); continue; }

            AddEdge(prev, node);
            prev = node;
        }

        AddEdge(prev, _bossNode);
    }

    void FillRemainingRooms()
    {
        int budgetCells = Mathf.RoundToInt(matrixSize * matrixSize * fillRoomBudget);
        int consecutiveFails = 0;
        int maxFails = 40;
        int extraBattle = 0;
        int extraEvent = 0;

        while (CountUsedCells() < budgetCells && consecutiveFails < maxFails)
        {
            bool battleFull = maxExtraBattleRooms >= 0 && extraBattle >= maxExtraBattleRooms;
            bool eventFull = maxExtraEventRooms >= 0 && extraEvent >= maxExtraEventRooms;
            if (battleFull && eventFull) break;

            RoomType type = PickWeightedRoomType(battleFull, eventFull);

            int hintX = Random.Range(maxRoomSize + roomPadding, matrixSize - maxRoomSize - roomPadding);
            int hintZ = Random.Range(maxRoomSize + roomPadding, matrixSize - maxRoomSize - roomPadding);

            var node = TryPlaceNear(type, PrefabsFor(type), new Vector2Int(hintX, hintZ), scatter: 15);
            if (node != null)
            {
                if (type == RoomType.Battle) extraBattle++;
                else if (IsEventRoom(type)) extraEvent++;
            }
            consecutiveFails = node == null ? consecutiveFails + 1 : 0;
        }
    }

    RoomType PickWeightedRoomType(bool battleFull, bool eventFull)
    {
        float bw = battleFull ? 0f : weightBattle;
        float hw = eventFull ? 0f : weightHeal;
        float sw = eventFull ? 0f : weightShop;
        float rw = eventFull ? 0f : weightRareLoot;
        float mw = eventFull ? 0f : weightMerge;

        float total = bw + hw + sw + rw + mw;
        if (total <= 0f) return RoomType.Battle;

        float roll = Random.Range(0f, total);
        if ((roll -= bw) < 0f) return RoomType.Battle;
        if ((roll -= hw) < 0f) return RoomType.Heal;
        if ((roll -= sw) < 0f) return RoomType.Shop;
        if ((roll -= rw) < 0f) return RoomType.RareLoot;
        return RoomType.Merge;
    }

    int CountUsedCells()
    {
        int c = 0;
        for (int x = 0; x < matrixSize; x++)
            for (int z = 0; z < matrixSize; z++)
                if (_matrix[x, z] != Cell.Empty) c++;
        return c;
    }

    void BuildEdges()
    {
        var parent = new Dictionary<MapNode, MapNode>();
        MapNode Find(MapNode n) { return parent[n] == n ? n : (parent[n] = Find(parent[n])); }
        void Union(MapNode a, MapNode b) { parent[Find(a)] = Find(b); }

        foreach (var n in _nodes) parent[n] = n;
        foreach (var e in _edges) Union(e.A, e.B);

        foreach (var node in _nodes)
        {
            if (!IsEventRoom(node)) continue;

            MapNode nearestBattle = null;
            float nearestDist = float.MaxValue;
            foreach (var other in _nodes)
            {
                if (other.Type != RoomType.Battle) continue;
                if (AlreadyConnected(node, other)) continue;
                float d = NodeDist(node, other);
                if (d < nearestDist) { nearestDist = d; nearestBattle = other; }
            }

            if (nearestBattle != null && !CenterLineCrosses(node, nearestBattle))
            {
                AddEdge(node, nearestBattle);
                Union(node, nearestBattle);
            }
        }

        var candidates = new List<(float dist, MapNode a, MapNode b)>();
        for (int i = 0; i < _nodes.Count; i++)
            for (int j = i + 1; j < _nodes.Count; j++)
            {
                if (AlreadyConnected(_nodes[i], _nodes[j])) continue;
                if (EdgeViolatesRestriction(_nodes[i], _nodes[j])) continue;
                candidates.Add((NodeDist(_nodes[i], _nodes[j]), _nodes[i], _nodes[j]));
            }
        candidates.Sort((x, y) => x.dist.CompareTo(y.dist));

        foreach (var (_, a, b) in candidates)
        {
            if (Find(a) == Find(b)) continue;
            if (CenterLineCrosses(a, b)) continue;
            AddEdge(a, b);
            Union(a, b);
        }



        var treeParent = BfsParent(_spawnNode);

        var onCycle = new HashSet<MapNode>();

        bool addedAny = true;
        while (addedAny)
        {
            addedAny = false;

            List<MapNode> bestCycleNodes = null;
            MapNode bestLeaf = null, bestPartner = null;

            foreach (var leaf in _nodes)
            {
                if (leaf.Edges.Count != 1) continue;
                if (onCycle.Contains(leaf)) continue;

                foreach (var partner in _nodes)
                {
                    if (partner == leaf) continue;
                    if (AlreadyConnected(leaf, partner)) continue;
                    if (EdgeViolatesRestriction(leaf, partner)) continue;
                    if (CenterLineCrosses(leaf, partner)) continue;

                    var cyclePath = GetTreePath(leaf, partner, treeParent);
                    if (cyclePath == null) continue;

                    if (bestCycleNodes != null && cyclePath.Count <= bestCycleNodes.Count) continue;

                    if (!CycleIsEmpty(cyclePath)) continue;

                    bestCycleNodes = cyclePath;
                    bestLeaf = leaf;
                    bestPartner = partner;
                }
            }

            if (bestLeaf != null)
            {
                AddEdge(bestLeaf, bestPartner);
                foreach (var n in bestCycleNodes) onCycle.Add(n);
                addedAny = true;
            }
        }
    }

    Dictionary<MapNode, MapNode> BfsParent(MapNode start)
    {
        var parent = new Dictionary<MapNode, MapNode> { [start] = null };
        var queue = new Queue<MapNode>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var edge in cur.Edges)
            {
                var nb = edge.A == cur ? edge.B : edge.A;
                if (!parent.ContainsKey(nb))
                {
                    parent[nb] = cur;
                    queue.Enqueue(nb);
                }
            }
        }
        return parent;
    }


    List<MapNode> GetTreePath(MapNode a, MapNode b, Dictionary<MapNode, MapNode> parent)
    {
        if (!parent.ContainsKey(a) || !parent.ContainsKey(b)) return null;

        var ancestorsA = new Dictionary<MapNode, int>();
        var cur = a; int depth = 0;
        while (cur != null) { ancestorsA[cur] = depth++; cur = parent[cur]; }

        var pathB = new List<MapNode>();
        cur = b;
        while (cur != null && !ancestorsA.ContainsKey(cur))
        {
            pathB.Add(cur);
            cur = parent[cur];
        }
        if (cur == null) return null;
        var lca = cur;

        var pathA = new List<MapNode>();
        cur = a;
        while (cur != lca) { pathA.Add(cur); cur = parent[cur]; }
        pathA.Add(lca);

        pathB.Reverse();
        pathA.AddRange(pathB);
        return pathA;
    }


    bool CycleIsEmpty(List<MapNode> cyclePath)
    {
        var polygon = new List<Vector2>(cyclePath.Count);
        foreach (var n in cyclePath)
            polygon.Add(new Vector2(n.WorldCenter.x, n.WorldCenter.z));

        var cycleSet = new HashSet<MapNode>(cyclePath);
        foreach (var n in _nodes)
        {
            if (cycleSet.Contains(n)) continue;
            if (PointInPolygon(new Vector2(n.WorldCenter.x, n.WorldCenter.z), polygon))
                return false;
        }
        return true;
    }

    static bool PointInPolygon(Vector2 point, List<Vector2> polygon)
    {
        int n = polygon.Count;
        bool inside = false;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            float xi = polygon[i].x, yi = polygon[i].y;
            float xj = polygon[j].x, yj = polygon[j].y;
            if (((yi > point.y) != (yj > point.y)) &&
                point.x < (xj - xi) * (point.y - yi) / (yj - yi) + xi)
                inside = !inside;
        }
        return inside;
    }



    bool CenterLineCrosses(MapNode a, MapNode b)
    {
        var p1 = new Vector2(a.CenterX, a.CenterZ);
        var p2 = new Vector2(b.CenterX, b.CenterZ);
        foreach (var e in _edges)
        {
            if (e.A == a || e.A == b || e.B == a || e.B == b) continue;
            if (SegmentsIntersect(p1, p2,
                    new Vector2(e.A.CenterX, e.A.CenterZ),
                    new Vector2(e.B.CenterX, e.B.CenterZ))) return true;
        }
        return false;
    }

    static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
    {
        float d1x = p2.x - p1.x, d1y = p2.y - p1.y;
        float d2x = q2.x - q1.x, d2y = q2.y - q1.y;
        float cross = d1x * d2y - d1y * d2x;
        if (Mathf.Abs(cross) < 1e-6f) return false;
        float t = ((q1.x - p1.x) * d2y - (q1.y - p1.y) * d2x) / cross;
        float u = ((q1.x - p1.x) * d1y - (q1.y - p1.y) * d1x) / cross;
        return t > 0.01f && t < 0.99f && u > 0.01f && u < 0.99f;
    }

    void StampAllCorridors()
    {
        foreach (var edge in _edges)
        {
            edge.Segments = RouteEdge(edge.A, edge.B);
            foreach (var seg in edge.Segments)
                StampSegment(seg);
        }
    }

    List<List<Vector2Int>> RouteEdge(MapNode a, MapNode b)
    {

        float overlapX = AxisOverlapRatio(a.MinX, a.MaxX, b.MinX, b.MaxX);
        float overlapZ = AxisOverlapRatio(a.MinZ, a.MaxZ, b.MinZ, b.MaxZ);

        bool coverX = AxisCoverageRatio(a.MinX, a.MaxX, b.MinX, b.MaxX) >= coverageThreshold;
        bool coverZ = AxisCoverageRatio(a.MinZ, a.MaxZ, b.MinZ, b.MaxZ) >= coverageThreshold;

        bool tryX = overlapZ >= straightLineThreshold || coverZ;
        bool tryZ = overlapX >= straightLineThreshold || coverX;

        if (tryX || tryZ)
        {
            bool preferTravelX = coverZ || overlapZ >= overlapX;

            var straight = TryStraight(a, b, preferTravelX);
            if (straight != null)
            {
                return straight;
            }

            straight = TryStraight(a, b, !preferTravelX);
            if (straight != null)
            {
                return straight;
            }
        }



        var szShape = TrySZShape(a, b);
        if (szShape != null)
        {

            return szShape;
        }

        var lShape = TryLShape(a, b);
        if (lShape != null)
        {

            return lShape;
        }

        return WaypointFallback(a, b);
    }

    static float AxisCoverageRatio(int a0, int a1, int b0, int b1)
    {
        int intersection = Mathf.Max(0, Mathf.Min(a1, b1) - Mathf.Max(a0, b0) + 1);
        if (intersection == 0) return 0f;
        int spanA = a1 - a0 + 1;
        int spanB = b1 - b0 + 1;
        return Mathf.Max((float)intersection / spanA, (float)intersection / spanB);
    }

    List<List<Vector2Int>> TryStraight(MapNode a, MapNode b, bool travelX)
    {
        List<Vector2Int> seg;

        if (travelX)
        {

            int zMin = Mathf.Max(a.MinZ, b.MinZ);
            int zMax = Mathf.Min(a.MaxZ, b.MaxZ);
            if (zMin > zMax) return null;
            int z = (zMin + zMax) / 2;

            int xA = a.CenterX < b.CenterX ? a.MaxX + 1 : a.MinX - 1;
            int xB = a.CenterX < b.CenterX ? b.MinX - 1 : b.MaxX + 1;
            seg = Rasterize(xA, z, xB, z);
        }
        else
        {
            int xMin = Mathf.Max(a.MinX, b.MinX);
            int xMax = Mathf.Min(a.MaxX, b.MaxX);
            if (xMin > xMax) return null;
            int x = (xMin + xMax) / 2;

            int zA = a.CenterZ < b.CenterZ ? a.MaxZ + 1 : a.MinZ - 1;
            int zB = a.CenterZ < b.CenterZ ? b.MinZ - 1 : b.MaxZ + 1;
            seg = Rasterize(x, zA, x, zB);
        }

        if (HitsRoom(seg)) return null;
        return new List<List<Vector2Int>> { seg };
    }

    List<List<Vector2Int>> TryLShape(MapNode a, MapNode b)
    {
        var exitsA = AllFaceExits(a, b);
        var exitsB = AllFaceExits(b, a);

        var pairs = new List<(int score, Vector2Int eA, Vector2Int eB)>();
        foreach (var eA in exitsA)
        {
            bool aIsHorizontal = eA.x < a.MinX || eA.x > a.MaxX;
            foreach (var eB in exitsB)
            {
                bool bIsHorizontal = eB.x < b.MinX || eB.x > b.MaxX;

                if (aIsHorizontal == bIsHorizontal) continue;

                int dx = b.CenterX - a.CenterX, dz = b.CenterZ - a.CenterZ;
                int scoreA = (eA.x > a.MaxX ? (dx > 0 ? 0 : 2) :
                              eA.x < a.MinX ? (dx < 0 ? 0 : 2) :
                              eA.y > a.MaxZ ? (dz > 0 ? 0 : 2) : (dz < 0 ? 0 : 2));
                int scoreB = (eB.x > b.MaxX ? (dx < 0 ? 0 : 2) :
                              eB.x < b.MinX ? (dx > 0 ? 0 : 2) :
                              eB.y > b.MaxZ ? (dz < 0 ? 0 : 2) : (dz > 0 ? 0 : 2));
                pairs.Add((scoreA + scoreB, eA, eB));
            }
        }
        pairs.Sort((x, y) => x.score.CompareTo(y.score));

        foreach (var (_, eA, eB) in pairs)
        {
            bool aExitsEastWest = eA.x < a.MinX || eA.x > a.MaxX;

            if (aExitsEastWest)
            {

                var bend = new Vector2Int(eB.x, eA.y);
                var sA = Rasterize(eA.x, eA.y, bend.x, bend.y);
                var sB = Rasterize(bend.x, bend.y, eB.x, eB.y);
                if (!HitsRoom(sA) && !HitsRoom(sB))
                    return new List<List<Vector2Int>> { sA, sB };
            }
            else
            {

                var bend = new Vector2Int(eA.x, eB.y);
                var sA = Rasterize(eA.x, eA.y, bend.x, bend.y);
                var sB = Rasterize(bend.x, bend.y, eB.x, eB.y);
                if (!HitsRoom(sA) && !HitsRoom(sB))
                    return new List<List<Vector2Int>> { sA, sB };
            }
        }

        return null;
    }

    List<Vector2Int> AllFaceExits(MapNode node, MapNode other)
    {
        int midX = Mathf.Clamp((node.CenterX + other.CenterX) / 2, node.MinX, node.MaxX);
        int midZ = Mathf.Clamp((node.CenterZ + other.CenterZ) / 2, node.MinZ, node.MaxZ);
        return new List<Vector2Int>
        {
            new Vector2Int(node.MaxX + 1, midZ),
            new Vector2Int(node.MinX - 1, midZ),
            new Vector2Int(midX, node.MaxZ + 1),
            new Vector2Int(midX, node.MinZ - 1),
        };
    }

    List<List<Vector2Int>> TrySZShape(MapNode a, MapNode b)
    {
        int dxAB = b.CenterX - a.CenterX;
        int dzAB = b.CenterZ - a.CenterZ;

        {
            bool aRight = dxAB >= 0;
            int faceAX = aRight ? a.MaxX : a.MinX;
            int exitAX = aRight ? faceAX + 1 : faceAX - 1;
            int faceBX = aRight ? b.MinX : b.MaxX;
            int exitBX = aRight ? faceBX - 1 : faceBX + 1;

            int gapMin = Mathf.Min(exitAX, exitBX);
            int gapMax = Mathf.Max(exitAX, exitBX);

            if (gapMax - gapMin >= 2)
            {

                int centerMidX = (exitAX + exitBX) / 2;
                int turnX = Mathf.Clamp(centerMidX, gapMin + 1, gapMax - 1);

                var p1 = new Vector2Int(exitAX, a.CenterZ);
                var p2 = new Vector2Int(turnX, a.CenterZ);
                var p3 = new Vector2Int(turnX, b.CenterZ);
                var p4 = new Vector2Int(exitBX, b.CenterZ);

                var seg1 = Rasterize(p1.x, p1.y, p2.x, p2.y);
                var seg2 = Rasterize(p2.x, p2.y, p3.x, p3.y);
                var seg3 = Rasterize(p3.x, p3.y, p4.x, p4.y);
                if (!HitsRoom(seg1) && !HitsRoom(seg2) && !HitsRoom(seg3))
                    return new List<List<Vector2Int>> { seg1, seg2, seg3 };
            }
        }

        {
            bool aUp = dzAB >= 0;
            int faceAZ = aUp ? a.MaxZ : a.MinZ;
            int exitAZ = aUp ? faceAZ + 1 : faceAZ - 1;
            int faceBZ = aUp ? b.MinZ : b.MaxZ;
            int exitBZ = aUp ? faceBZ - 1 : faceBZ + 1;

            int gapMin = Mathf.Min(exitAZ, exitBZ);
            int gapMax = Mathf.Max(exitAZ, exitBZ);

            if (gapMax - gapMin >= 2)
            {

                int centerMidZ = (exitAZ + exitBZ) / 2;
                int turnZ = Mathf.Clamp(centerMidZ, gapMin + 1, gapMax - 1);

                var p1 = new Vector2Int(a.CenterX, exitAZ);
                var p2 = new Vector2Int(a.CenterX, turnZ);
                var p3 = new Vector2Int(b.CenterX, turnZ);
                var p4 = new Vector2Int(b.CenterX, exitBZ);

                var seg1 = Rasterize(p1.x, p1.y, p2.x, p2.y);
                var seg2 = Rasterize(p2.x, p2.y, p3.x, p3.y);
                var seg3 = Rasterize(p3.x, p3.y, p4.x, p4.y);
                if (!HitsRoom(seg1) && !HitsRoom(seg2) && !HitsRoom(seg3))
                    return new List<List<Vector2Int>> { seg1, seg2, seg3 };
            }
        }

        return null;
    }

    List<List<Vector2Int>> WaypointFallback(MapNode a, MapNode b)
    {
        Vector2Int exitA = FaceExitToward(a, b);
        Vector2Int exitB = FaceExitToward(b, a);

        Vector2Int? wp = NearestCorridorCell((exitA + exitB) / 2, searchRadius: 40);

        if (wp.HasValue)
        {
            var toWP = Rasterize(exitA.x, exitA.y, wp.Value.x, wp.Value.y);
            var fromWP = Rasterize(wp.Value.x, wp.Value.y, exitB.x, exitB.y);
            return new List<List<Vector2Int>> { toWP, fromWP };
        }

        Debug.LogWarning($"[MapGeometry] Waypoint fallback forcing straight: {a.Type}↔{b.Type}");
        return new List<List<Vector2Int>> { Rasterize(exitA.x, exitA.y, exitB.x, exitB.y) };
    }

    Vector2Int FaceExitToward(MapNode from, MapNode to)
    {
        int dx = to.CenterX - from.CenterX;
        int dz = to.CenterZ - from.CenterZ;

        if (Mathf.Abs(dx) >= Mathf.Abs(dz))
        {

            int faceX = dx > 0 ? from.MaxX : from.MinX;
            int exitX = dx > 0 ? faceX + 1 : faceX - 1;

            int midZ = Mathf.Clamp((from.CenterZ + to.CenterZ) / 2, from.MinZ, from.MaxZ);
            return new Vector2Int(exitX, midZ);
        }
        else
        {

            int faceZ = dz > 0 ? from.MaxZ : from.MinZ;
            int exitZ = dz > 0 ? faceZ + 1 : faceZ - 1;
            int midX = Mathf.Clamp((from.CenterX + to.CenterX) / 2, from.MinX, from.MaxX);
            return new Vector2Int(midX, exitZ);
        }
    }

    List<Vector2Int> Rasterize(int x0, int z0, int x1, int z1)
    {
        var cells = new List<Vector2Int>();
        if (x0 == x1)
        {
            int zMin = Mathf.Min(z0, z1), zMax = Mathf.Max(z0, z1);
            for (int z = zMin; z <= zMax; z++) cells.Add(new Vector2Int(x0, z));
        }
        else
        {
            int xMin = Mathf.Min(x0, x1), xMax = Mathf.Max(x0, x1);
            for (int x = xMin; x <= xMax; x++) cells.Add(new Vector2Int(x, z0));
        }
        return cells;
    }

    void StampSegment(List<Vector2Int> seg)
    {
        int half = corridorWidth / 2;
        foreach (var cell in seg)
        {
            for (int dx = -half; dx <= half; dx++)
                for (int dz = -half; dz <= half; dz++)
                {
                    int cx = cell.x + dx, cz = cell.y + dz;
                    if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                    if (_matrix[cx, cz] == Cell.Empty || _matrix[cx, cz] == Cell.Occupied)
                        _matrix[cx, cz] = Cell.Corridor;
                }
        }
    }

    bool HitsRoom(List<Vector2Int> seg)
    {
        foreach (var c in seg)
        {
            if (c.x < 0 || c.y < 0 || c.x >= matrixSize || c.y >= matrixSize) continue;
            if (_matrix[c.x, c.y] == Cell.Room) return true;
        }
        return false;
    }

    static float AxisOverlapRatio(int a0, int a1, int b0, int b1)
    {
        int overlap = Mathf.Max(0, Mathf.Min(a1, b1) - Mathf.Max(a0, b0) + 1);
        int union = Mathf.Max(a1, b1) - Mathf.Min(a0, b0) + 1;
        return union == 0 ? 0f : (float)overlap / union;
    }

    Vector2Int? NearestCorridorCell(Vector2Int from, int searchRadius)
    {
        int bestDist = int.MaxValue;
        Vector2Int? best = null;
        for (int x = Mathf.Max(0, from.x - searchRadius); x < Mathf.Min(matrixSize, from.x + searchRadius); x++)
            for (int z = Mathf.Max(0, from.y - searchRadius); z < Mathf.Min(matrixSize, from.y + searchRadius); z++)
            {
                if (_matrix[x, z] != Cell.Corridor) continue;
                int d = Mathf.Abs(x - from.x) + Mathf.Abs(z - from.y);
                if (d < bestDist) { bestDist = d; best = new Vector2Int(x, z); }
            }
        return best;
    }

    MapNode TryPlaceNear(RoomType type, GameObject[] prefabs, Vector2Int hint, int scatter)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            var prefab = RandomFrom(prefabs);
            var preset = prefab.GetComponent<RoomPreset>();
            int sx = OddClamp(preset != null ? Mathf.RoundToInt(preset.roomSize.x) : RandomSize());
            int sz = OddClamp(preset != null ? Mathf.RoundToInt(preset.roomSize.y) : RandomSize());

            int cx = hint.x + Random.Range(-scatter, scatter + 1);
            int cz = hint.y + Random.Range(-scatter, scatter + 1);
            int ox = cx - sx / 2;
            int oz = cz - sz / 2;

            if (!FitsInMatrix(ox, oz, sx, sz)) continue;
            if (!AreaFree(ox, oz, sx, sz)) continue;

            return StampAndCreateNode(type, prefab, ox, oz, sx, sz);
        }
        return null;
    }

    MapNode StampAndCreateNode(RoomType type, GameObject prefab, int ox, int oz, int sx, int sz)
    {
        for (int x = ox; x < ox + sx; x++)
            for (int z = oz; z < oz + sz; z++)
                _matrix[x, z] = Cell.Room;

        for (int x = ox - 1; x <= ox + sx; x++)
            for (int z = oz - 1; z <= oz + sz; z++)
            {
                if (x < 0 || z < 0 || x >= matrixSize || z >= matrixSize) continue;
                if (_matrix[x, z] == Cell.Empty) _matrix[x, z] = Cell.Occupied;
            }

        int cx = ox + sx / 2;
        int cz = oz + sz / 2;

        var node = new MapNode
        {
            Type = type,
            MinX = ox,
            MinZ = oz,
            MaxX = ox + sx - 1,
            MaxZ = oz + sz - 1,
            WorldCenter = new Vector3(cx + 0.5f, 0f, cz + 0.5f),
            ChosenPrefab = prefab,
        };
        _nodes.Add(node);
        return node;
    }

    bool FitsInMatrix(int ox, int oz, int sx, int sz)
    {
        int p = roomPadding;
        return ox >= p && oz >= p && ox + sx < matrixSize - p && oz + sz < matrixSize - p;
    }

    bool AreaFree(int ox, int oz, int sx, int sz)
    {
        int p = roomPadding;
        for (int x = ox - p; x < ox + sx + p; x++)
            for (int z = oz - p; z < oz + sz + p; z++)
            {
                if (x < 0 || z < 0 || x >= matrixSize || z >= matrixSize) return false;
                if (_matrix[x, z] != Cell.Empty) return false;
            }
        return true;
    }

    void AddEdge(MapNode a, MapNode b)
    {
        if (a == b || AlreadyConnected(a, b)) return;
        var edge = new MapEdge { A = a, B = b };
        _edges.Add(edge);
        a.Edges.Add(edge);
        b.Edges.Add(edge);
    }

    bool AlreadyConnected(MapNode a, MapNode b)
    {
        foreach (var e in a.Edges)
            if (e.A == b || e.B == b) return true;
        return false;
    }

    static bool EdgeViolatesRestriction(MapNode a, MapNode b)
    {
        bool aEvent = IsEventRoom(a);
        bool bEvent = IsEventRoom(b);
        bool aGate = a.Type == RoomType.Spawn || a.Type == RoomType.Boss;
        bool bGate = b.Type == RoomType.Spawn || b.Type == RoomType.Boss;
        return (aEvent && bGate) || (bEvent && aGate);
    }

    static bool IsEventRoom(MapNode n) =>
        n.Type == RoomType.Heal || n.Type == RoomType.Shop ||
        n.Type == RoomType.RareLoot || n.Type == RoomType.Merge;

    static bool IsEventRoom(RoomType t) =>
        t == RoomType.Heal || t == RoomType.Shop ||
        t == RoomType.RareLoot || t == RoomType.Merge;

    static RoomType PickEventType(int idx)
    {
        var events = new[] { RoomType.Heal, RoomType.Shop, RoomType.RareLoot, RoomType.Merge };
        return events[idx % events.Length];
    }

    float NodeDist(MapNode a, MapNode b)
    {
        float dx = a.CenterX - b.CenterX, dz = a.CenterZ - b.CenterZ;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

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
        pb.ToMesh(); pb.Refresh();

        if (corridorMat != null)
            pb.GetComponent<Renderer>().material = corridorMat;
    }

    void SpawnAllWalls()
    {
        var parent = new GameObject("Walls").transform;
        var dirs = new[]
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
                    int nx = x + d.x, nz = z + d.y;
                    bool empty = nx < 0 || nz < 0 || nx >= matrixSize || nz >= matrixSize
                                 || _matrix[nx, nz] == Cell.Empty
                                 || _matrix[nx, nz] == Cell.Occupied;
                    if (empty) SpawnWallQuad(parent, x, z, d);
                }
            }
    }

    void SpawnWallQuad(Transform parent, int x, int z, Vector2Int facing)
    {
        Vector3 center = new Vector3(x + 0.5f, 0f, z + 0.5f);
        Vector3 faceOffset = new Vector3(facing.x * 0.5f, wallHeight / 2f, facing.y * 0.5f);
        Quaternion rot = Quaternion.LookRotation(new Vector3(-facing.x, 0, -facing.y));

        var go = new GameObject($"W_{x}_{z}");
        go.transform.SetParent(parent);
        go.transform.position = center + faceOffset;
        go.transform.rotation = rot;
        go.layer = LayerMask.NameToLayer("Wall");

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
        pb.ToMesh(); pb.Refresh();

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        if (wallMat != null)
            pb.GetComponent<Renderer>().material = wallMat;
    }

    GameObject[] PrefabsFor(RoomType t) => t switch
    {
        RoomType.Spawn => spawnRoomPrefabs,
        RoomType.Battle => battleRoomPrefabs,
        RoomType.Boss => bossRoomPrefabs,
        RoomType.Shop => shopRoomPrefabs,
        RoomType.Heal => healRoomPrefabs,
        RoomType.RareLoot => rareLootRoomPrefabs,
        RoomType.Merge => mergeRoomPrefabs,
        _ => battleRoomPrefabs
    };

    GameObject RandomFrom(GameObject[] arr) => arr[Random.Range(0, arr.Length)];

    int OddClamp(int v)
    {
        v = Mathf.Clamp(v, minRoomSize, maxRoomSize);
        return v % 2 == 0 ? v - 1 : v;
    }

    int RandomSize() => Random.Range(minRoomSize, maxRoomSize + 1);

    Vector2Int ClampToMatrix(Vector2Int v, int pad) => new Vector2Int(
        Mathf.Clamp(v.x, pad, matrixSize - pad),
        Mathf.Clamp(v.y, pad, matrixSize - pad));

    List<RoomNode> ToLegacyRoomNodes()
    {
        var nodeMap = new Dictionary<MapNode, RoomNode>();
        foreach (var n in _nodes)
        {
            var legacy = new RoomNode
            {
                Type = n.Type,
                MatrixOrigin = new Vector2Int(n.MinX, n.MinZ),
                MatrixCenter = new Vector2Int(n.CenterX, n.CenterZ),
                Size = new Vector2Int(n.Width, n.Depth),
                WorldPosition = n.WorldCenter,
                ChosenPrefab = n.ChosenPrefab,
                RoomObject = n.RoomObject,
            };
            nodeMap[n] = legacy;
            n.LegacyNode = legacy;
        }
        foreach (var edge in _edges)
        {
            if (!nodeMap.TryGetValue(edge.A, out var la) ||
                !nodeMap.TryGetValue(edge.B, out var lb)) continue;
            if (!la.Neighbors.Contains(lb)) la.Neighbors.Add(lb);
            if (!lb.Neighbors.Contains(la)) lb.Neighbors.Add(la);
        }
        return new List<RoomNode>(nodeMap.Values);
    }

    void OnDrawGizmos()
    {
        if (_nodes == null) return;
        foreach (var node in _nodes)
        {
            Gizmos.color = node.Type switch
            {
                RoomType.Spawn => Color.green,
                RoomType.Battle => Color.red,
                RoomType.Boss => Color.magenta,
                RoomType.Shop => Color.yellow,
                RoomType.Merge => Color.cyan,
                RoomType.Heal => Color.white,
                RoomType.RareLoot => new Color(1f, 0.5f, 0f),
                _ => Color.grey
            };
            Gizmos.DrawWireCube(
                node.WorldCenter + Vector3.up,
                new Vector3(node.Width, 2f, node.Depth));
        }

        if (_edges == null) return;
        Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.7f);
        foreach (var edge in _edges)
            Gizmos.DrawLine(edge.A.WorldCenter + Vector3.up, edge.B.WorldCenter + Vector3.up);
    }
}