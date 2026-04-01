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


public class RoomPort
{
    public RoomNode Owner;
    public Vector2Int Direction;    
    public Vector2Int MatrixCell;  
    public bool IsUsed;
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
    public RoomPort[] Ports;       
}

public class MapGenerator : MonoBehaviour
{
    [Header("Room Prefabs")]
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
   
    public GameObject[] normalEnemyPrefabs;
    public GameObject[] bossPrefabs;
    public GameObject lootPrefab;

    [Header("Materials")]
    public Material corridorMat;
    public Material wallMat;
    public Material boundaryMaterial;

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
    [Tooltip("How many cells the corridor extends straight out from the room wall before bending")]
    public int exitStubLength = 2;

    [Header("Walls")]
    public float wallHeight = 2f;
    public float wallThickness = 0.1f;
    public float floorThickness = 0.01f;

    [Header("Trigger")]
    public float triggerHeight = 3f;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    private byte[,] _matrix;
    private List<RoomNode> _rooms = new();
    private RoomNode _spawnRoom;
    private List<(RoomPort, RoomPort)> _corridorPairs = new();


    void Start() => GenerateMap();

    void GenerateMap()
    {
        _matrix = new byte[matrixSize, matrixSize];
        _rooms.Clear();
        _corridorPairs.Clear();

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
                ChosenPrefab = prefab
            };

            node.Ports = BuildPorts(node);
            _rooms.Add(node);
            return node;
        }

        Debug.LogWarning($"[MapGen] Could not place {type} after {maxPlacementAttempts} attempts.");
        return null;
    }


    RoomPort[] BuildPorts(RoomNode node)
    {
        var cardinals = new Vector2Int[]
        {
            new Vector2Int( 1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int( 0, 1),
            new Vector2Int( 0,-1),
        };

        var ports = new RoomPort[4];
        for (int i = 0; i < 4; i++)
        {
            var dir = cardinals[i];
       
            Vector2Int edgeCell = node.MatrixCenter + new Vector2Int(
                dir.x * (node.Size.x - 1) / 2,
                dir.y * (node.Size.y - 1) / 2);

            ports[i] = new RoomPort
            {
                Owner = node,
                Direction = dir,
                MatrixCell = edgeCell,
                IsUsed = false
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

    RoomPort ClaimPort(RoomNode node, Vector2Int targetCenter, bool spawnGuard = false, RoomType targetType = RoomType.None)
    {

        if (node.Type == RoomType.Spawn && targetType != RoomType.Battle)
            return null;

        if (spawnGuard && node.Type == RoomType.Spawn)
        {
            foreach (var p in node.Ports)
                if (p.IsUsed) return null;
        }

        Vector2Int toTarget = targetCenter - node.MatrixCenter;

        var sorted = new List<RoomPort>(node.Ports);
        sorted.Sort((a, b) =>
        {
            float dotA = Vector2.Dot(a.Direction, (Vector2)toTarget);
            float dotB = Vector2.Dot(b.Direction, (Vector2)toTarget);
            return dotB.CompareTo(dotA);
        });

        foreach (var port in sorted)
            if (!port.IsUsed) { port.IsUsed = true; return port; }

        return null;
    }

    

    bool AddConnection(RoomNode a, RoomNode b, bool spawnGuard = false)
    {
        
        if (a.Neighbors.Contains(b)) return false;

        var portA = ClaimPort(a, b.MatrixCenter, spawnGuard, b.Type);
        var portB = ClaimPort(b, a.MatrixCenter, targetType: a.Type);

        if (portA == null || portB == null)
        {
            
            if (portA != null) portA.IsUsed = false;
            if (portB != null) portB.IsUsed = false;
            Debug.LogWarning($"[MapGen] Could not connect {a.Type} → {b.Type}: no free ports.");
            return false;
        }

        _corridorPairs.Add((portA, portB));
        if (!a.Neighbors.Contains(b)) a.Neighbors.Add(b);
        if (!b.Neighbors.Contains(a)) b.Neighbors.Add(a);
        return true;
    }



    List<RoomNode> BuildMainPath(RoomNode start, int count)
    {
        var path = new List<RoomNode>();
        var current = start;

        for (int i = 0; i < count; i++)
        {
            int minOffset = i == 0 ? 8 : 15;
            int maxOffset = i == 0 ? 12 : 25;

            var hint = Clamped(current.MatrixCenter + RandomCardinalOffset(minOffset, maxOffset));
            var next = PlaceRoom(RoomType.Battle, hint.x, hint.y);
            if (next == null) break;

            bool ok = AddConnection(current, next, spawnGuard: current.Type == RoomType.Spawn);
            if (!ok) { _rooms.Remove(next); continue; }

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
        int battleCount = mainPath.Count;

        for (int i = 0; i < mainPath.Count - 1; i++)
        {
            if (Random.value > branchChance) continue;

            bool canPlaceBattle = battleCount < maxBattleRooms;
            RoomType type;

            if (canPlaceBattle && Random.value >= 0.25f)
                type = RoomType.Battle;
            else
            {
                type = PickUnusedEventType(eventWeights, usedEventTypes);
                if (type == RoomType.None) type = RoomType.Battle;
            }

            var anchor = FindClosestRoomWithFreePort(mainPath[i]);
            var hint = Clamped(anchor.MatrixCenter + RandomCardinalOffset(10, 20));
            var branch = PlaceRoom(type, hint.x, hint.y);
            if (branch == null) continue;

            if (!AddConnection(anchor, branch)) { _rooms.Remove(branch); continue; }

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
                    var anchor2 = FindClosestRoomWithFreePort(branch);
                    var hint2 = Clamped(anchor2.MatrixCenter + RandomCardinalOffset(10, 20));
                    var branch2 = PlaceRoom(type2, hint2.x, hint2.y);
                    if (branch2 != null)
                    {
                        if (!AddConnection(anchor2, branch2)) { _rooms.Remove(branch2); }
                        else
                        {
                            usedEventTypes.Add(type2);
                            RunManager.Instance?.RegisterEventRoomPlaced(type2);
                        }
                    }
                }
            }
        }
    }


    RoomNode FindClosestRoomWithFreePort(RoomNode from)
    {
        RoomNode best = from;
        float bestDist = float.MaxValue;

        foreach (var room in _rooms)
        {
            if (room == from) continue;
          
            if (room.Type == RoomType.Spawn) continue;
            bool hasFreePort = false;
            foreach (var port in room.Ports)
                if (!port.IsUsed) { hasFreePort = true; break; }
            if (!hasFreePort) continue;

            float dist = Vector2Int.Distance(from.MatrixCenter, room.MatrixCenter);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = room;
            }
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

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var (t, w) in available)
        {
            cumulative += w;
            if (roll <= cumulative) return t;
        }
        return available[available.Count - 1].t;
    }


    void CarveAllCorridors()
    {
        foreach (var (pA, pB) in _corridorPairs)
            CarvePortToPort(pA, pB);
    }

    void CarvePortToPort(RoomPort portA, RoomPort portB)
    {
        
        Vector2Int startCell = portA.MatrixCell + portA.Direction;
        Vector2Int endCell = portB.MatrixCell + portB.Direction;

     
        Vector2Int stubEndA = startCell;
        for (int i = 0; i < exitStubLength; i++)
        {
            Vector2Int c = startCell + portA.Direction * i;
            SafeSetCorridor(c);
            stubEndA = c;
        }

       
        Vector2Int stubEndB = endCell;
        for (int i = 0; i < exitStubLength; i++)
        {
            Vector2Int c = endCell + portB.Direction * i;
            SafeSetCorridor(c);
            stubEndB = c;
        }

        CarveL(stubEndA, stubEndB, portA.Direction, portB.Direction);
    }


    void CarveL(Vector2Int from, Vector2Int to, Vector2Int dirA, Vector2Int dirB)
    {
        if (from == to) return;

    
        Vector2Int? clearH = FindClearCorner(from, to, horizontal: true);
        Vector2Int? clearV = FindClearCorner(from, to, horizontal: false);

        Vector2Int corner;
        if (clearH.HasValue && clearV.HasValue)
        {
            
            int distH = ManhattanDist(from, clearH.Value) + ManhattanDist(clearH.Value, to);
            int distV = ManhattanDist(from, clearV.Value) + ManhattanDist(clearV.Value, to);
            corner = distH <= distV ? clearH.Value : clearV.Value;
        }
        else if (clearH.HasValue)
            corner = clearH.Value;
        else if (clearV.HasValue)
            corner = clearV.Value;
        else
        {
            
            Vector2Int cornerH = new Vector2Int(to.x, from.y);
            Vector2Int cornerV = new Vector2Int(from.x, to.y);
            int costH = SegmentRoomCount(from, cornerH) + SegmentRoomCount(cornerH, to);
            int costV = SegmentRoomCount(from, cornerV) + SegmentRoomCount(cornerV, to);
            corner = costH <= costV ? cornerH : cornerV;
            Debug.LogWarning($"[MapGen] CarveL: no clear path from {from} to {to}, using least-bad corner.");
        }

        CarveWidthSegment(from, corner);
        CarveWidthSegment(corner, to);
    }


    Vector2Int? FindClearCorner(Vector2Int from, Vector2Int to, bool horizontal)
    {
        Vector2Int natural = horizontal
            ? new Vector2Int(to.x, from.y)
            : new Vector2Int(from.x, to.y);

        
        int maxSlide = matrixSize;

        for (int slide = 0; slide <= maxSlide; slide++)
        {
            
            for (int sign = -1; sign <= 1; sign += 2)
            {
                if (slide == 0 && sign == 1) { sign = 0; continue; } 
                if (slide == 0 && sign == 0) sign = 1;            

                Vector2Int corner = horizontal
                    ? new Vector2Int(natural.x + slide * sign, natural.y)
                    : new Vector2Int(natural.x, natural.y + slide * sign);

                if (corner.x < 1 || corner.y < 1 || corner.x >= matrixSize - 1 || corner.y >= matrixSize - 1)
                    continue;

                if (SegmentRoomCount(from, corner) == 0 && SegmentRoomCount(corner, to) == 0)
                    return corner;
            }
        }
        return null;
    }


    int SegmentRoomCount(Vector2Int from, Vector2Int to)
    {
        int count = 0;
        foreach (var cell in AxisAlignedCells(from, to))
        {
            if (cell.x < 0 || cell.y < 0 || cell.x >= matrixSize || cell.y >= matrixSize) continue;
            if (_matrix[cell.x, cell.y] == Cell.Room) count++;
        }
        return count;
    }

    int ManhattanDist(Vector2Int a, Vector2Int b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);


    void CarveWidthSegment(Vector2Int from, Vector2Int to)
    {
        if (from == to) return;

        bool horizontal = from.y == to.y;
        int half = corridorWidth / 2;

        foreach (var center in AxisAlignedCells(from, to))
        {
            for (int w = -half; w < corridorWidth - half; w++)
            {
                int cx = horizontal ? center.x : center.x + w;
                int cz = horizontal ? center.y + w : center.y;
                if (cx < 0 || cz < 0 || cx >= matrixSize || cz >= matrixSize) continue;
                if (_matrix[cx, cz] == Cell.Empty)
                    _matrix[cx, cz] = Cell.Corridor;
            }
        }
    }

 
    IEnumerable<Vector2Int> AxisAlignedCells(Vector2Int from, Vector2Int to)
    {
        int x0 = Mathf.Min(from.x, to.x), x1 = Mathf.Max(from.x, to.x);
        int z0 = Mathf.Min(from.y, to.y), z1 = Mathf.Max(from.y, to.y);
        for (int x = x0; x <= x1; x++)
            for (int z = z0; z <= z1; z++)
                yield return new Vector2Int(x, z);
    }

    void SafeSetCorridor(Vector2Int c)
    {
        if (c.x < 0 || c.y < 0 || c.x >= matrixSize || c.y >= matrixSize) return;
        if (_matrix[c.x, c.y] == Cell.Empty)
            _matrix[c.x, c.y] = Cell.Corridor;
    }



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
        pb.ToMesh();
        pb.Refresh();

        var mc = go.AddComponent<MeshCollider>();
        mc.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        if (wallMat != null)
            pb.GetComponent<Renderer>().material = wallMat;
    }



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

    GameObject[] PickFloorWeightedEnemyPrefabs()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
            return new GameObject[0];

        int floor = Mathf.Clamp(RunManager.Instance?.CurrentFloor ?? 1, 1, 4);
        int availableCount = floor switch
        {
            1 => 2,
            2 => 2,
            3 => Mathf.Min(3, normalEnemyPrefabs.Length),
            _ => Mathf.Min(4, normalEnemyPrefabs.Length)
        };

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

      
            if (node.Ports == null) continue;
            Gizmos.color = Color.cyan;
            foreach (var port in node.Ports)
            {
                Vector3 portWorld = new Vector3(port.MatrixCell.x + 0.5f, 1f, port.MatrixCell.y + 0.5f);
                Vector3 dir3 = new Vector3(port.Direction.x, 0, port.Direction.y);
                Gizmos.DrawRay(portWorld, dir3 * 2f);
            }
        }
    }
}