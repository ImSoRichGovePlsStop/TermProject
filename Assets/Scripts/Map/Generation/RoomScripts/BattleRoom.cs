using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

    [HideInInspector] public RoomNode node;

    [System.Serializable]
    public struct DoorInfo
    {
        public Vector3 worldPosition;
        public Quaternion rotation;
    }

    [Header("Enemy Spawning")]
    public EnemyEntry[] enemyEntries;
    public GameObject lootPrefab;
    public GameObject upgradeStationPrefab;
    public GameObject doorPrefab;
    [HideInInspector] public List<Vector3> spawnCells = new();
    [HideInInspector] public List<DoorInfo> doorInfos = new();

    public int eliteBudget = 0;

    [Header("Budget Spawning")]
    [Tooltip("Base spawn budget before area scaling.")]
    public float baseBudget = 80f;
    [Tooltip("Budget added per unit of floor area.")]
    public float budgetPerArea = 0.43f;
    [Tooltip("Rooms with area above this threshold get waveCount reduced by 1.")]
    public int waveReduceAreaThreshold = 100;

    [Header("Waves")]
    [Range(1, 5)] public int waveCount = 3;
    public float wavePause = 1f;
    public int waveThreshold = 0;

    [Header("Reward")]
    [Tooltip("Chance to spawn loot instead of upgrade station on room clear.")]
    [Range(0f, 1f)]
    public float lootChance = 0.5f;

    [Header("Loot Config")]
    public int lootOptionCount = 3;
    public float lootBaseMean = 50f;
    public float lootMeanPerFloor = 30f;
    public float lootMeanPerRoom = 5f;
    public float lootMeanFlat = 10f;
    public float lootBaseSd = 20f;
    public float lootSdPerFloor = 3f;

    [Header("Reward Placement")]
    [Tooltip("Minimum cell distance from room wall when spawning loot/upgrade rewards.")]
    public int lootInset = 2;

    [Header("Coin Drop Fallback")]
    [Tooltip("Fallback coin min when enemy has no EnemyBase.")]
    public int fallbackCoinMin = 4;
    [Tooltip("Fallback coin max when enemy has no EnemyBase.")]
    public int fallbackCoinMax = 9;
    [Tooltip("Additional coin multiplier per floor beyond floor 1.")]
    public float coinFloorMultiplier = 0.3f;

    [Header("Enemy Scaling")]
    public float enemySpeedMin = 0.9f;
    public float enemySpeedMax = 1.1f;
    public float enemyProgressBossWeight = 0.3f;
    public float enemyProgressRoomWeight = 0.1f;
    public float enemyHpPlayerDmgWeight = 0.15f;
    public float enemyDmgPlayerHpWeight = 0.15f;

    public Material boundaryMaterial;

    protected Vector3 roomSize;
    protected List<GameObject> invisibleWalls = new();
    private List<GameObject> _spawnedDoors = new();
    protected int _aliveCount = 0;
    protected PlayerCombatContext _combatContext;
    protected int _currentWave = 0;
    protected int _totalBudget;
    protected int[] _waveBudgets;
    protected int[] _eliteBudgetsPerWave;

    const float TriggerInset = 0.3f;
    const float InvisibleWallThickness = 0.01f;

    protected virtual void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _combatContext = playerObj.GetComponent<PlayerCombatContext>();
    }

    protected void Subscribe()
    {
        if (_combatContext == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) _combatContext = playerObj.GetComponent<PlayerCombatContext>();
        }
        if (_combatContext != null) _combatContext.OnEntityKilled += OnEntityKilled;
    }

    protected void Unsubscribe()
    {
        if (_combatContext != null) _combatContext.OnEntityKilled -= OnEntityKilled;
    }

    protected virtual void OnDestroy() => Unsubscribe();

    protected virtual void OnEntityKilled(HealthBase enemy)
    {
        if (!isLocked || isCleared) return;
        _aliveCount = Mathf.Max(0, _aliveCount - 1);

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : fallbackCoinMin;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : fallbackCoinMax;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * coinFloorMultiplier;
        float modMult   = RunManager.Instance?.EffectiveCoinMultiplier ?? 1f;
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(coinMin, coinMax + 1) * floorMult * modMult));

        if (_aliveCount <= waveThreshold)
            StartCoroutine(OnWaveCleared());
    }

    protected virtual IEnumerator OnWaveCleared()
    {
        isLocked = false;
        _currentWave++;

        if (_currentWave < waveCount)
        {
            yield return new WaitForSeconds(wavePause);
            isLocked = true;
            SpawnWave(_currentWave);
        }
        else
        {
            FindFirstObjectByType<UIManager>().isInBattle = false;
            Unsubscribe();
            ClearRoom();
        }
    }

    protected void BuildWaveBudgets()
    {
        waveCount = Mathf.Max(1, waveCount);
        _waveBudgets = new int[waveCount];
        int baseWaveBudget = _totalBudget / waveCount;
        int remainder      = _totalBudget % waveCount;
        for (int i = 0; i < waveCount; i++)
            _waveBudgets[i] = baseWaveBudget + (i < remainder ? 1 : 0);

        _eliteBudgetsPerWave = new int[waveCount];
        for (int i = 0; i < eliteBudget; i++)
            _eliteBudgetsPerWave[Random.Range(0, waveCount)]++;
    }

    protected virtual void SpawnWave(int waveIndex)
    {
        if (enemyEntries == null || enemyEntries.Length == 0)
        {
            StartCoroutine(OnWaveCleared());
            return;
        }

        int budget = _waveBudgets[waveIndex];
        int spawned = 0;
        int safetyLimit = 200;

        while (budget > 0 && safetyLimit-- > 0)
        {
            // Filter entries that fit in remaining budget
            var affordable = new List<EnemyEntry>();
            int cheapest = int.MaxValue;
            foreach (var e in enemyEntries)
            {
                int c = Mathf.Max(1, e.cost);
                if (c < cheapest) cheapest = c;
                if (c <= budget) affordable.Add(e);
            }

            if (affordable.Count == 0) break; // nothing fits

            var entry = affordable[Random.Range(0, affordable.Count)];
            bool useElite = _eliteBudgetsPerWave[waveIndex] > 0 && entry.elite != null;
            if (useElite) _eliteBudgetsPerWave[waveIndex]--;
            var prefab = useElite ? entry.elite : entry.normal;
            budget -= Mathf.Max(1, entry.cost);

            var go = Instantiate(prefab, PickSpawnPosition(), Quaternion.identity);

            var groupSpawner = go.GetComponent<IGroupSpawner>();
            if (groupSpawner != null)
            {
                spawned += groupSpawner.GetSpawnCount();
                groupSpawner.SetGroupStatScale(ComputeStatScale());
            }
            else
            {
                spawned++;
                ApplyEnemyScale(go);
            }
        }

        _aliveCount += spawned;
        if (spawned == 0)
            StartCoroutine(OnWaveCleared());
    }

    protected Vector3 PickSpawnPosition()
    {
        if (spawnCells != null && spawnCells.Count > 0)
            return spawnCells[Random.Range(0, spawnCells.Count)] + Vector3.up * 0.5f;
        return transform.position + Vector3.up * 0.5f;
    }

    protected Vector3 PickLootPosition()
    {
        if (spawnCells == null || spawnCells.Count == 0)
            return transform.position;

        // Only consider cells that are at least lootInset cells away from every wall.
        float minX = transform.position.x - roomSize.x * 0.5f + lootInset;
        float maxX = transform.position.x + roomSize.x * 0.5f - lootInset;
        float minZ = transform.position.z - roomSize.z * 0.5f + lootInset;
        float maxZ = transform.position.z + roomSize.z * 0.5f - lootInset;

        var interior = new List<Vector3>();
        foreach (var cell in spawnCells)
            if (cell.x >= minX && cell.x <= maxX && cell.z >= minZ && cell.z <= maxZ)
                interior.Add(cell);

        // Fall back to any cell if the room is too small for the inset
        var pool = interior.Count > 0 ? interior : spawnCells;
        return pool[Random.Range(0, pool.Count)];
    }

    protected StatScale ComputeStatScale()
    {
        var player = FindFirstObjectByType<PlayerStats>();
        var rm = RunManager.Instance;
        var scale = new StatScale();
        float progress = (rm?.TotalBossKilled ?? 0) * enemyProgressBossWeight
                       + (rm?.TotalRoomsCleared ?? 0) * enemyProgressRoomWeight;
        scale.moveSpeed = Random.Range(enemySpeedMin, enemySpeedMax) * (rm?.EffectiveEnemySpeedMultiplier ?? 1f);
        scale.hp        = (1f + progress + (player.Damage / Mathf.Max(1f, player.BaseDamage)) * enemyHpPlayerDmgWeight)
                          * (rm?.EffectiveEnemyHpMultiplier ?? 1f);
        scale.damage    = (1f + progress + (player.MaxHealth / Mathf.Max(1f, player.BaseHealth)) * enemyDmgPlayerHpWeight)
                          * (rm?.EffectiveEnemyDamageMultiplier ?? 1f);
        return scale;
    }

    protected void ApplyEnemyScale(GameObject enemy)
    {
        if (!enemy.TryGetComponent<EntityStats>(out var stats)) return;
        stats.SetStatScale(ComputeStatScale());
    }

    public void CalculateTotalBudget(Vector3 vol)
    {
        float area    = vol.x * vol.z;
        float modMult = RunManager.Instance?.EffectiveEnemyCountMult ?? 1f;
        _totalBudget  = Mathf.RoundToInt((baseBudget + area * budgetPerArea) * modMult);

        // Large rooms get one fewer wave so each wave has more enemies (feels fuller)
        if (area > waveReduceAreaThreshold)
            waveCount = Mathf.Max(1, waveCount - 1);
    }

    // Apply run modifier bonuses once when the room is first entered
    protected virtual void ApplyRunModifiers()
    {
        var rm = RunManager.Instance;
        if (rm == null) return;
        eliteBudget += rm.EffectiveEliteBudgetBonus;

        // Extra waves add proportional budget instead of splitting the same pool
        int extraWaves = rm.EffectiveExtraWaves;
        if (extraWaves > 0)
        {
            int totalWaves = waveCount + extraWaves;
            // Scale total budget so each wave keeps roughly the same budget
            _totalBudget = Mathf.RoundToInt(_totalBudget * ((float)totalWaves / Mathf.Max(1, waveCount)));
            waveCount = totalWaves;
        }
    }

    public virtual void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared || isLocked) return;

        ApplyRunModifiers();
        LockRoom();
        BuildWaveBudgets();
        _currentWave = 0;
        _aliveCount = 0;
        Subscribe();
        SpawnWave(0);

        var ui = FindFirstObjectByType<UIManager>();
        ui.isInBattle = true;
        ui.CloseShop();
        if (ui.IsInventoryOpen) ui.ToggleInventory();
    }

    protected virtual void ClearRoom()
    {
        isCleared = true;
        isLocked = false;
        RemoveInvisibleWalls();

        bool firstRoom = (RunManager.Instance?.TotalRoomsCleared ?? 0) == 0;
        float effectiveLootChance = Mathf.Clamp01(lootChance + (RunManager.Instance?.EffectiveLootChanceBias ?? 0f));
        if (firstRoom || Random.value < effectiveLootChance)
            SpawnLoot(PickLootPosition());
        else if (upgradeStationPrefab != null)
            Instantiate(upgradeStationPrefab, PickLootPosition(), Quaternion.identity);
        else
            SpawnLoot(PickLootPosition());

        RunManager.Instance?.OnRoomCleared();
        HealPlayerAfterRoom();
    }

    protected void SpawnLoot(Vector3 position)
    {
        var lootObj = Instantiate(lootPrefab, position, Quaternion.identity);
        var randomLoot = lootObj.GetComponent<RandomLoot>();
        if (randomLoot != null)
            randomLoot.Configure(BuildLootConfig());
    }

    protected virtual LootConfig BuildLootConfig()
    {
        var rm           = RunManager.Instance;
        int floor        = rm?.CurrentFloor ?? 1;
        int roomsCleared = rm?.TotalRoomsCleared ?? 0;
        float mean = lootBaseMean + floor * lootMeanPerFloor + roomsCleared * lootMeanPerRoom + lootMeanFlat
                   + (rm?.EffectiveLootMeanBonus ?? 0f);
        float sd      = lootBaseSd + (floor - 1) * lootSdPerFloor;
        int   options = lootOptionCount + (rm?.EffectiveExtraLootOptions ?? 0);
        return new LootConfig { optionCount = options, meanCost = mean, sd = sd, allowDuplicates = false };
    }

    protected void HealPlayerAfterRoom()
    {
        var rm = RunManager.Instance;
        float healPercent = (rm?.HealPerRoom ?? 0f) + (rm?.EffectiveHealPerRoomBonus ?? 0f);
        if (healPercent <= 0f) return;
        var player = FindFirstObjectByType<PlayerStats>();
        player?.HealPercent(healPercent);
        RunManager.Instance?.OnHealed();
    }

    public void SetRoomSize(Vector3 size) => roomSize = size;

    protected void LockRoom()
    {
        isLocked = true;
        CreateInvisibleWalls();
        SpawnDoors();
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

    private void RemoveDoors()
    {
        foreach (var go in _spawnedDoors)
            if (go != null) Destroy(go);
        _spawnedDoors.Clear();
    }

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform child in go.transform)
            SetLayerRecursive(child.gameObject, layer);
    }

    protected void CreateInvisibleWalls()
    {
        float t = InvisibleWallThickness;
        (Vector3 pos, Vector3 size)[] configs =
        {
            (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(t, roomSize.y, roomSize.z)),
            (new Vector3( roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(t, roomSize.y, roomSize.z)),
            (new Vector3(0f, roomSize.y / 2f,  roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, t)),
            (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, t)),
        };

        foreach (var (localPos, size) in configs)
        {
            var wall = new GameObject("InvisibleWall");
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            wall.AddComponent<BoxCollider>().size = size;
            invisibleWalls.Add(wall);
        }
    }

    protected void RemoveInvisibleWalls()
    {
        foreach (var wall in invisibleWalls)
            if (wall != null) Destroy(wall);
        invisibleWalls.Clear();
        RemoveDoors();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StartCoroutine(WaitForPlayerInside(other.transform));
    }

    private IEnumerator WaitForPlayerInside(Transform playerTransform)
    {
        float inset = TriggerInset;
        while (true)
        {
            Vector3 flat = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            Vector3 minBnd = transform.position - new Vector3(roomSize.x / 2f - inset, 0, roomSize.z / 2f - inset);
            Vector3 maxBnd = transform.position + new Vector3(roomSize.x / 2f - inset, 0, roomSize.z / 2f - inset);

            if (flat.x >= minBnd.x && flat.x <= maxBnd.x &&
                flat.z >= minBnd.z && flat.z <= maxBnd.z) break;

            if (Vector3.Distance(flat, transform.position) > Mathf.Max(roomSize.x, roomSize.z))
                yield break;

            yield return null;
        }
        OnPlayerEnter();
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
    }
}
