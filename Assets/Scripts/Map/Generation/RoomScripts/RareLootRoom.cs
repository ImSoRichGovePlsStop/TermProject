using System.Collections.Generic;
using UnityEngine;

public class RareLootRoom : MonoBehaviour
{
    public GameObject lootPrefab;

    [HideInInspector] public RoomNode node;

    [Header("Loot Scaling")]
    [Tooltip("Number of module options offered (run modifier can add more).")]
    public int   lootOptionCount     = 1;
    [Tooltip("Base loot mean cost — should sit above a typical battle-room drop.")]
    public float lootBaseMean        = 100f;
    [Tooltip("Mean cost increase per floor.")]
    public float lootMeanPerFloor    = 25f;
    [Tooltip("Extra mean cost per boss already killed this run.")]
    public float lootMeanPerBossKill = 0f;
    [Tooltip("Base standard deviation of loot cost.")]
    public float lootBaseSd          = 30f;
    [Tooltip("SD increase per floor.")]
    public float lootSdPerFloor      = 5f;

    [Header("Enemy Scaling")]
    public float enemySpeedMin           = 0.9f;
    public float enemySpeedMax           = 1.1f;
    public float enemyHpPerFloor         = 1.20f;
    public float enemyHpPerSegment       = 1.75f;
    public float enemyDmgPerFloor        = 1.12f;
    public float enemyDmgPerSegment      = 1.4f;
    public float enemyHpPlayerDmgWeight  = 0.0f;
    public float enemyDmgPlayerHpWeight  = 0.0f;

    [Header("Room Lock")]
    public GameObject doorPrefab;
    public Material   boundaryMaterial;

    [HideInInspector] public List<Vector3>             spawnCells = new();
    [HideInInspector] public List<BattleRoom.DoorInfo> doorInfos  = new();

    private Vector3   _roomSize;
    private bool      _mimicActive;

    private readonly List<GameObject> _spawnedDoors   = new();
    private readonly List<GameObject> _invisibleWalls = new();
    private PlayerCombatContext        _combatContext;

    const float WallThickness = 0.01f;

    public void SetRoomSize(Vector3 size) => _roomSize = size;

    public void Init(Transform spawnPoint)
    {
        if (lootPrefab == null) return;
        var obj = Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation);
        var rl = obj.GetComponent<RandomLoot>();
        if (rl != null)
        {
            rl.Configure(BuildLootConfig());
            rl.EnableMimic();
            rl.OnMimicSpawned = HandleMimicSpawned;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        RunManager.Instance?.OnEventRoomEntered();
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
    }

    private StatScale ComputeStatScale()
        => BattleRoom.BuildStatScale(
            FindFirstObjectByType<PlayerStats>(), RunManager.Instance,
            enemySpeedMin,          enemySpeedMax,
            enemyHpPerFloor,        enemyHpPerSegment,
            enemyDmgPerFloor,       enemyDmgPerSegment,
            enemyHpPlayerDmgWeight, enemyDmgPlayerHpWeight);

    private void HandleMimicSpawned(GameObject mimic)
    {
        if (mimic != null && mimic.TryGetComponent<EntityStats>(out var stats))
            stats.SetStatScale(ComputeStatScale());

        _mimicActive = true;
        LockRoom();

        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _combatContext = playerObj.GetComponent<PlayerCombatContext>();
        if (_combatContext != null) _combatContext.OnEntityKilled += OnEntityKilled;

        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null) ui.isInBattle = true;
    }

    private void OnEntityKilled(HealthBase entity)
    {
        if (!_mimicActive) return;
        if (entity.GetComponent<MimicController>() == null) return;

        _mimicActive = false;
        if (_combatContext != null) _combatContext.OnEntityKilled -= OnEntityKilled;

        UnlockRoom();

        var ui = FindFirstObjectByType<UIManager>();
        if (ui != null) ui.isInBattle = false;
    }

    private void OnDestroy()
    {
        if (_combatContext != null) _combatContext.OnEntityKilled -= OnEntityKilled;
    }

    private void LockRoom()
    {
        CreateInvisibleWalls();
        SpawnDoors();
    }

    private void UnlockRoom()
    {
        RemoveInvisibleWalls();
    }

    private void SpawnDoors()
    {
        if (doorPrefab == null) return;
        int wallLayer = LayerMask.NameToLayer("Wall");
        foreach (var info in doorInfos)
        {
            var go = Instantiate(doorPrefab, info.worldPosition, info.rotation);
            if (wallLayer >= 0) SetLayerRecursive(go, wallLayer);
            _spawnedDoors.Add(go);
        }
    }

    private void RemoveInvisibleWalls()
    {
        foreach (var wall in _invisibleWalls)
            if (wall != null) Destroy(wall);
        _invisibleWalls.Clear();
        foreach (var go in _spawnedDoors)
            if (go != null) Destroy(go);
        _spawnedDoors.Clear();
    }

    private void CreateInvisibleWalls()
    {
        int wallLayer = LayerMask.NameToLayer("Wall");
        (Vector3 localPos, Vector3 size)[] configs =
        {
            (new Vector3(-_roomSize.x / 2f, _roomSize.y / 2f, 0f), new Vector3(WallThickness, _roomSize.y, _roomSize.z)),
            (new Vector3( _roomSize.x / 2f, _roomSize.y / 2f, 0f), new Vector3(WallThickness, _roomSize.y, _roomSize.z)),
            (new Vector3(0f, _roomSize.y / 2f,  _roomSize.z / 2f), new Vector3(_roomSize.x, _roomSize.y, WallThickness)),
            (new Vector3(0f, _roomSize.y / 2f, -_roomSize.z / 2f), new Vector3(_roomSize.x, _roomSize.y, WallThickness)),
        };
        foreach (var (localPos, size) in configs)
        {
            var wall = new GameObject("InvisibleWall");
            if (wallLayer >= 0) wall.layer = wallLayer;
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            wall.AddComponent<BoxCollider>().size = size;
            _invisibleWalls.Add(wall);
        }
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    LootConfig BuildLootConfig()
    {
        var rm      = RunManager.Instance;
        int floor   = rm?.CurrentFloor ?? 1;
        float mean  = lootBaseMean + floor * lootMeanPerFloor + (rm?.EffectiveLootMeanBonus ?? 0f);
        float sd    = lootBaseSd + (floor - 1) * lootSdPerFloor;
        int options = lootOptionCount + (rm?.EffectiveExtraLootOptions ?? 0);
        return new LootConfig { optionCount = options, meanCost = mean, sd = sd, allowDuplicates = false };
    }
}
