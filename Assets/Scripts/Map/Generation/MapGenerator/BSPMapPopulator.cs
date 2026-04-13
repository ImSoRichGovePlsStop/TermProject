using Unity.AI.Navigation;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(BSPMapGeometry))]
public class BSPMapPopulator : MonoBehaviour
{
    [Header("Interactable Prefabs")]
    public GameObject healStationPrefab;
    public GameObject shopStationPrefab;
    public GameObject costedHealPrefab;
    public GameObject costedUpgradePrefab;
    public GameObject freeUpgradeStationPrefab;
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
        GetComponent<BSPMapGeometry>().OnMapReady += PopulateRooms;
    }

    void OnDestroy()
    {
        var geo = GetComponent<BSPMapGeometry>();
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
            navMeshSurface.BuildNavMesh();
    }

    GameObject SpawnRoom(MapNode node) => node.Type switch
    {
        RoomType.Spawn    => SpawnSpawnRoom(node),
        RoomType.Battle   => SpawnBattleRoom(node),
        RoomType.Boss     => SpawnBossRoom(node),
        RoomType.Heal     => SpawnEventRoom(node, healStationPrefab, SetupHeal),
        RoomType.Shop     => SpawnEventRoom(node, shopStationPrefab, SetupShop),
        RoomType.RareLoot => SpawnEventRoom(node, lootPrefab, SetupRareLoot),
        RoomType.Merge    => SpawnEventRoom(node, mergeStationPrefab, SetupMerge),
        _                 => null
    };

    GameObject SpawnSpawnRoom(MapNode node)
    {
        var obj = MakeRoomObject(node, "SpawnRoom");
        obj.AddComponent<SpawnRoom>().node = ToLegacy(node);
        return obj;
    }

    GameObject SpawnBattleRoom(MapNode node)
    {
        var obj  = MakeRoomObject(node, "BattleRoom");
        var vol  = Volume(node);
        var room = obj.AddComponent<BattleRoom>();
        room.node                   = ToLegacy(node);
        room.lootPrefab             = lootPrefab;
        room.upgradeStationPrefab   = freeUpgradeStationPrefab;
        room.boundaryMaterial       = boundaryMaterial;
        room.enemyPrefabs           = normalEnemyPrefabs;
        room.SetRoomSize(vol);
        room.enemyCount             = room.ScaleEnemyCount(vol);
        room.spawnCells             = CollectSpawnCells(node);
        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnBossRoom(MapNode node)
    {
        var obj  = MakeRoomObject(node, "BossRoom");
        var vol  = Volume(node);
        var room = obj.AddComponent<BossRoom>();
        room.node             = ToLegacy(node);
        room.bossPrefab       = PickBoss();
        room.lootPrefab       = lootPrefab;
        room.portalPrefab     = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.spawnCells       = CollectSpawnCells(node);
        room.SetRoomSize(vol);
        AddTrigger(obj, vol);
        return obj;
    }

    GameObject SpawnEventRoom(MapNode node, GameObject stationPrefab,
                              System.Action<GameObject, Transform, MapNode> setup)
    {
        if (stationPrefab == null) return null;
        var obj    = MakeRoomObject(node, node.Type + "Room");
        var preset = obj.GetComponent<RoomPreset>();
        var pt     = (preset?.interactableSpawnPoint != null) ? preset.interactableSpawnPoint : obj.transform;
        AddTrigger(obj, Volume(node));
        setup(obj, pt, node);
        return obj;
    }

    void SetupHeal(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<HealRoom>();
        r.node = ToLegacy(n);
        r.healStationPrefab = healStationPrefab;
        r.Init(p);
    }

    void SetupShop(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<ShopRoom>();
        r.node                = ToLegacy(n);
        r.shopStationPrefab   = shopStationPrefab;
        r.costedHealPrefab    = costedHealPrefab;
        r.costedUpgradePrefab = costedUpgradePrefab;
        r.Init(p);
    }

    void SetupRareLoot(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<RareLootRoom>();
        r.node      = ToLegacy(n);
        r.lootPrefab = lootPrefab;
        r.Init(o.transform);
    }

    void SetupMerge(GameObject o, Transform p, MapNode n)
    {
        var r = o.AddComponent<MergeRoom>();
        r.node               = ToLegacy(n);
        r.mergeStationPrefab = mergeStationPrefab;
        r.Init(p);
    }

    GameObject MakeRoomObject(MapNode node, string name)
    {
        var prefab = node.ChosenPrefab;
        GameObject obj;
        if (prefab != null)
            obj = Object.Instantiate(prefab, node.WorldCenter, Quaternion.identity);
        else
        {
            obj = new GameObject();
            obj.transform.position = node.WorldCenter;
        }
        obj.name = name;
        return obj;
    }

    System.Collections.Generic.List<Vector3> CollectSpawnCells(MapNode node)
    {
        var cells  = new System.Collections.Generic.List<Vector3>();
        var roomMap = GetComponent<BSPMapGeometry>()?.RoomMapPublic;
        if (roomMap == null) return cells;
        for (int x = node.MinX; x <= node.MaxX; x++)
            for (int z = node.MinZ; z <= node.MaxZ; z++)
                if (roomMap[x, z] == node)
                    cells.Add(new Vector3(x + 0.5f, 0f, z + 0.5f));
        return cells;
    }

    GameObject PickBoss()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return null;
        int idx = Mathf.Clamp((RunManager.Instance?.CurrentFloor ?? 1) - 1, 0, bossPrefabs.Length - 1);
        return bossPrefabs[idx];
    }

    Vector3 Volume(MapNode n) => new Vector3(n.Width, triggerHeight, n.Depth);

    void AddTrigger(GameObject obj, Vector3 vol)
    {
        var col = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = vol;
        col.center    = new Vector3(0, triggerHeight / 2f, 0);
    }

    static RoomNode ToLegacy(MapNode n)
    {
        if (n.LegacyNode != null) return n.LegacyNode;
        return new RoomNode
        {
            Type          = n.Type,
            MatrixOrigin  = new Vector2Int(n.MinX, n.MinZ),
            MatrixCenter  = new Vector2Int(n.CenterX, n.CenterZ),
            Size          = new Vector2Int(n.Width, n.Depth),
            WorldPosition = n.WorldCenter,
            ChosenPrefab  = n.ChosenPrefab,
            RoomObject    = n.RoomObject,
        };
    }
}
