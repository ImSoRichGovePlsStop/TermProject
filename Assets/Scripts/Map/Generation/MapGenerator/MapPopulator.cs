using UnityEngine;
using Random = UnityEngine.Random;

// ── MapPopulator ──────────────────────────────────────────────────────────────
// Responsible for:
//   • Instantiating room GameObjects and attaching room-script components
//   • Injecting enemy prefabs into BattleRooms (floor-scaled pool)
//   • Injecting boss prefabs into BossRooms (floor-indexed)
//   • Setting up interactable stations for event rooms (Heal/Shop/Upgrade/Merge)
//   • Wiring portal prefabs for Boss/final rooms
//
// Requires MapGeometry on the same GameObject (or assigned via inspector).
// Listens to MapGeometry.OnMapReady — no coupling to generation internals.

[RequireComponent(typeof(MapGeometry))]
public class MapPopulator : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Interactable Prefabs")]
    public GameObject healStationPrefab;
    public GameObject shopStationPrefab;
    public GameObject upgradeStationPrefab;
    public GameObject mergeStationPrefab;

    [Header("Portals")]
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    [Header("Enemy / Loot")]
    [Tooltip("Normal enemies — floor 1 uses first 2, floor 4 uses all")]
    public GameObject[] normalEnemyPrefabs;
    [Tooltip("One boss per floor — index 0 = floor 1, index 1 = floor 2, etc.")]
    public GameObject[] bossPrefabs;
    public GameObject lootPrefab;

    [Header("Visual")]
    public Material boundaryMaterial;

    [Header("Trigger")]
    public float triggerHeight = 3f;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    void Awake()
    {
        var geometry = GetComponent<MapGeometry>();
        geometry.OnMapReady += PopulateRooms;
    }

    // ── Population entry point ────────────────────────────────────────────────

    void PopulateRooms(System.Collections.Generic.IReadOnlyList<RoomNode> rooms)
    {
        foreach (var node in rooms)
            node.RoomObject = SpawnRoom(node);
    }

    // ── Room dispatch ─────────────────────────────────────────────────────────

    GameObject SpawnRoom(RoomNode node) => node.Type switch
    {
        RoomType.Spawn   => SpawnSpawnRoom(node),
        RoomType.Battle  => SpawnBattleRoom(node),
        RoomType.Boss    => SpawnBossRoom(node),
        RoomType.Heal    => SpawnEventRoom(node, healStationPrefab,    SetupHealRoom),
        RoomType.Shop    => SpawnEventRoom(node, shopStationPrefab,    SetupShopRoom),
        RoomType.Upgrade => SpawnEventRoom(node, upgradeStationPrefab, SetupUpgradeRoom),
        RoomType.Merge   => SpawnEventRoom(node, mergeStationPrefab,   SetupMergeRoom),
        _                => null
    };

    // ── Spawn room ────────────────────────────────────────────────────────────

    GameObject SpawnSpawnRoom(RoomNode node)
    {
        var obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name = "SpawnRoom";
        var sr  = obj.AddComponent<SpawnRoom>();
        sr.node = node;
        return obj;
    }

    // ── Battle room ───────────────────────────────────────────────────────────

    GameObject SpawnBattleRoom(RoomNode node)
    {
        var    obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name   = "BattleRoom";
        Vector3 vol = RoomVolume(node);

        var room              = obj.AddComponent<BattleRoom>();
        room.node             = node;
        room.lootPrefab       = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.enemyCount       = ScaleEnemyCount();
        room.enemyPrefabs     = PickFloorWeightedEnemyPrefabs();
        room.SetRoomSize(vol);

        AddTrigger(obj, vol);
        return obj;
    }

    /// <summary>Enemy count: base 1–3 plus current floor bonus.</summary>
    int ScaleEnemyCount()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        return Random.Range(1, 4) + floor;
    }

    // ── Boss room ─────────────────────────────────────────────────────────────

    GameObject SpawnBossRoom(RoomNode node)
    {
        var    obj = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name   = "BossRoom";
        Vector3 vol = RoomVolume(node);

        var room              = obj.AddComponent<BossRoom>();
        room.node             = node;
        room.bossPrefab       = PickBossPrefab();
        room.lootPrefab       = lootPrefab;
        room.portalPrefab     = portalPrefab;
        room.portalFinalPrefab = portalFinalPrefab;
        room.boundaryMaterial = boundaryMaterial;
        room.SetRoomSize(vol);

        AddTrigger(obj, vol);
        return obj;
    }

    /// <summary>Select boss by floor index; clamps safely if fewer bosses than floors.</summary>
    GameObject PickBossPrefab()
    {
        if (bossPrefabs == null || bossPrefabs.Length == 0) return null;
        int floor = Mathf.Clamp((RunManager.Instance?.CurrentFloor ?? 1) - 1, 0, bossPrefabs.Length - 1);
        return bossPrefabs[floor];
    }

    // ── Event rooms ───────────────────────────────────────────────────────────

    GameObject SpawnEventRoom(RoomNode node, GameObject stationPrefab,
                              System.Action<GameObject, Transform, RoomNode> setup)
    {
        if (stationPrefab == null)
        {
            Debug.LogWarning($"[MapPopulator] Missing station prefab for {node.Type}");
            return null;
        }

        var obj    = Instantiate(node.ChosenPrefab, node.WorldPosition, Quaternion.identity);
        obj.name   = node.Type + "Room";
        var preset = obj.GetComponent<RoomPreset>();
        var pt     = preset?.interactableSpawnPoint != null
                     ? preset.interactableSpawnPoint : obj.transform;

        AddTrigger(obj, RoomVolume(node));
        setup(obj, pt, node);
        return obj;
    }

    void SetupHealRoom(GameObject o, Transform p, RoomNode n)
    {
        var r = o.AddComponent<HealRoom>();
        r.node = n; r.healStationPrefab = healStationPrefab; r.Init(p);
    }

    void SetupShopRoom(GameObject o, Transform p, RoomNode n)
    {
        var r = o.AddComponent<ShopRoom>();
        r.node = n; r.shopStationPrefab = shopStationPrefab; r.Init(p);
    }

    void SetupUpgradeRoom(GameObject o, Transform p, RoomNode n)
    {
        var r = o.AddComponent<UpgradeRoom>();
        r.node = n; r.upgradeStationPrefab = upgradeStationPrefab; r.Init(p);
    }

    void SetupMergeRoom(GameObject o, Transform p, RoomNode n)
    {
        var r = o.AddComponent<MergeRoom>();
        r.node = n; r.mergeStationPrefab = mergeStationPrefab; r.Init(p);
    }

    // ── Enemy pool scaling ────────────────────────────────────────────────────

    /// <summary>
    /// Returns the subset of normalEnemyPrefabs available on the current floor.
    /// Floor 1-2 → first 2; Floor 3 → first 3; Floor 4+ → all (up to array length).
    /// </summary>
    GameObject[] PickFloorWeightedEnemyPrefabs()
    {
        if (normalEnemyPrefabs == null || normalEnemyPrefabs.Length == 0)
            return new GameObject[0];

        int floor = Mathf.Clamp(RunManager.Instance?.CurrentFloor ?? 1, 1, 4);
        int count = floor switch
        {
            1 => 2,
            2 => 2,
            3 => Mathf.Min(3, normalEnemyPrefabs.Length),
            _ => Mathf.Min(4, normalEnemyPrefabs.Length)
        };

        var pool = new GameObject[count];
        for (int i = 0; i < count; i++)
            pool[i] = normalEnemyPrefabs[i];
        return pool;
    }

    // ── Shared helpers ────────────────────────────────────────────────────────

    Vector3 RoomVolume(RoomNode node) =>
        new Vector3(node.Size.x, triggerHeight, node.Size.y);

    void AddTrigger(GameObject obj, Vector3 vol)
    {
        var col       = obj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size      = vol;
        col.center    = new Vector3(0, triggerHeight / 2f, 0);
    }
}
