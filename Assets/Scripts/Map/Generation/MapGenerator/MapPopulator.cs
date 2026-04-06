using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;


[RequireComponent(typeof(MapGeometry))]
public class MapPopulator : MonoBehaviour
{
    [Header("Interactable Prefabs")]
    public GameObject healStationPrefab;
    public GameObject shopStationPrefab;
    public GameObject upgradeStationPrefab;
    public GameObject mergeStationPrefab;

    [Header("Portals")]
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    [Header("Enemy / Loot")]
    [Tooltip("Normal enemies — floor 1 uses first 2, floor 4+ uses all")]
    public GameObject[] normalEnemyPrefabs;
    [Tooltip("One boss prefab per floor, index 0 = floor 1")]
    public GameObject[] bossPrefabs;
    public GameObject lootPrefab;

    [Header("Visual")]
    public Material boundaryMaterial;

    [Header("Navigation")]
    public NavMeshSurface navMeshSurface;

    [Header("Trigger")]
    public float triggerHeight = 3f;

   

    void Awake()
    {
        GetComponent<MapGeometry>().OnMapReady += PopulateRooms;
    }

    void OnDestroy()
    {
        var geo = GetComponent<MapGeometry>();
        if (geo != null) geo.OnMapReady -= PopulateRooms;
    }



    void PopulateRooms(System.Collections.Generic.IReadOnlyList<MapNode> nodes)
    {
        foreach (var node in nodes)
            node.RoomObject = SpawnRoom(node);

        StartCoroutine(BakeNavMeshNextFrame());
    }

    System.Collections.IEnumerator BakeNavMeshNextFrame()
    {

        yield return null;

        if (navMeshSurface != null)
        {
            navMeshSurface.BuildNavMesh();
            Debug.Log("[MapPopulator] NavMesh baked.");
        }
        else
        {
            Debug.LogWarning("[MapPopulator] NavMeshSurface not assigned — navmesh skipped.");
        }
    }

    GameObject SpawnRoom(MapNode node) => node.Type switch
    {
        RoomType.Spawn => SpawnSpawnRoom(node),
        RoomType.Battle => SpawnBattleRoom(node),
        RoomType.Boss => SpawnBossRoom(node),
        RoomType.Heal => SpawnEventRoom(node, healStationPrefab, SetupHeal),
        RoomType.Shop => SpawnEventRoom(node, shopStationPrefab, SetupShop),
        RoomType.Upgrade => SpawnEventRoom(node, upgradeStationPrefab, SetupUpgrade),
        RoomType.Merge => SpawnEventRoom(node, mergeStationPrefab, SetupMerge),
        _ => null
    };


    GameObject SpawnSpawnRoom(MapNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldCenter, Quaternion.identity);
        obj.name = "SpawnRoom";
        var sr = obj.AddComponent<SpawnRoom>();
        sr.node = ToLegacy(node);
        return obj;
    }

    GameObject SpawnBattleRoom(MapNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldCenter, Quaternion.identity);
        obj.name = "BattleRoom";
        Vector3 vol = Volume(node);

        var room = obj.AddComponent<BattleRoom>();
        room.node = ToLegacy(node);
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyPrefabs = FloorWeightedEnemyPool();
        room.SetRoomSize(vol);
        room.enemyCount = ScaleEnemyCount(vol);
        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnBossRoom(MapNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldCenter, Quaternion.identity);
        obj.name = "BossRoom";
        Vector3 vol = Volume(node);

        var room = obj.AddComponent<BossRoom>();
        room.node = ToLegacy(node);
        room.bossPrefab = PickBoss();
        room.lootPrefab = lootPrefab;
        room.portalPrefab = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.SetRoomSize(vol);

        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnEventRoom(MapNode node, GameObject stationPrefab,
                              System.Action<GameObject, Transform, MapNode> setup)
    {
        if (stationPrefab == null)
        {
            Debug.LogWarning($"[MapPopulator] Missing station prefab for {node.Type}");
            return null;
        }
        var obj = Instantiate(node.ChosenPrefab, node.WorldCenter, Quaternion.identity);
        obj.name = node.Type + "Room";
        var preset = obj.GetComponent<RoomPreset>();
        var pt = (preset?.interactableSpawnPoint != null) ? preset.interactableSpawnPoint : obj.transform;

        AddTrigger(obj, Volume(node));
        setup(obj, pt, node);
        return obj;
    }

    void SetupHeal(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<HealRoom>(); r.node = ToLegacy(n); r.healStationPrefab = healStationPrefab; r.Init(p);
    }
    void SetupShop(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<ShopRoom>(); r.node = ToLegacy(n); r.shopStationPrefab = shopStationPrefab; r.Init(p);
    }
    void SetupUpgrade(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<UpgradeRoom>(); r.node = ToLegacy(n); r.upgradeStationPrefab = upgradeStationPrefab; r.Init(p);
    }
    void SetupMerge(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<MergeRoom>(); r.node = ToLegacy(n); r.mergeStationPrefab = mergeStationPrefab; r.Init(p);
    }



    int ScaleEnemyCount(Vector3 vol)
    {
        float scale = (((vol.x * vol.z) / 60f)-1)*0.7f + 1f;


        
        return (int)((Random.Range(3, 8) + (RunManager.Instance?.CurrentFloor ?? 1)) * scale + 2);
    }

        GameObject PickBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return null;
        int idx = Mathf.Clamp((RunManager.Instance?.CurrentFloor ?? 1) - 1, 0, bossPrefabs.Length - 1);
        return bossPrefabs[idx];
    }

    GameObject[] FloorWeightedEnemyPool()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
            return new GameObject[0];

        int floor = Mathf.Clamp(RunManager.Instance?.CurrentFloor ?? 1, 1, 4);
        int count = floor switch { 1 => 2, 2 => 2, 3 => Mathf.Min(3, normalEnemyPrefabs.Length), _ => Mathf.Min(4, normalEnemyPrefabs.Length) };

        var pool = new GameObject[count];
        for (int i = 0; i < count; i++) pool[i] = normalEnemyPrefabs[i];
        return pool;
    }



    Vector3 Volume(MapNode n) => new Vector3(n.Width, triggerHeight, n.Depth);

    void AddTrigger(GameObject obj, Vector3 vol)
    {
        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = vol;
        col.center = new Vector3(0, triggerHeight / 2f, 0);
    }


    static RoomNode ToLegacy(MapNode n) => new RoomNode
    {
        Type = n.Type,
        MatrixOrigin = new Vector2Int(n.MinX, n.MinZ),
        MatrixCenter = new Vector2Int(n.CenterX, n.CenterZ),
        Size = new Vector2Int(n.Width, n.Depth),
        WorldPosition = n.WorldCenter,
        ChosenPrefab = n.ChosenPrefab,
        RoomObject = n.RoomObject,
    };
}