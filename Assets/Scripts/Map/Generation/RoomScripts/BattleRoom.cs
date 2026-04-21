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
    [Tooltip("Fraction of wave enemies remaining that triggers the next wave spawn.")]
    [Range(0f, 0.5f)] public float waveNextThresholdMin = 0.2f;
    [Range(0f, 0.5f)] public float waveNextThresholdMax = 0.3f;

    [Header("Spawn Timing")]
    [Tooltip("Min delay in seconds between each enemy spawn within a wave.")]
    public float spawnDelayMin = 0.1f;
    [Tooltip("Max delay in seconds between each enemy spawn within a wave.")]
    public float spawnDelayMax = 0.7f;

    [Header("Reward")]
    [Tooltip("Chance to spawn loot instead of upgrade station on room clear.")]
    [Range(0f, 1f)]
    public float lootChance = 0.5f;

    [Header("Loot Config")]
    public int lootOptionCount = 3;
    public float lootBaseMean = 25f;
    public float lootMeanPerFloor = 20f;
    public float lootMeanPerRoom = 0f;
    public float lootBaseSd = 10f;
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
    public float coinFloorMultiplier = 0.1f;

    [Header("Enemy Scaling")]
    public float enemySpeedMin        = 0.9f;
    public float enemySpeedMax        = 1.1f;
    [Tooltip("HP multiplier applied per floor")]
    public float enemyHpPerFloor      = 1.20f;
    [Tooltip("Additional HP multiplier applied per completed segment.")]
    public float enemyHpPerSegment    = 1.75f;
    [Tooltip("Damage multiplier applied per floor (e.g. 1.12 = +12% each floor).")]
    public float enemyDmgPerFloor     = 1.12f;
    [Tooltip("Additional damage multiplier applied per completed segment.")]
    public float enemyDmgPerSegment   = 1.4f;
    public float enemyHpPlayerDmgWeight  = 0.0f;
    public float enemyDmgPlayerHpWeight  = 0.0f;

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

    protected int  _waveSpawnedCount   = 0;
    protected int  _waveClearThreshold = 0;
    protected bool _spawning           = false;
    protected bool _waveClearPending   = false;

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
        if (isCleared) return;

        if (_aliveCount > 0) _aliveCount--;

        if (!isLocked) return;   // during wave pause — don't award coins or trigger wave clear

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : fallbackCoinMin;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : fallbackCoinMax;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * coinFloorMultiplier;
        float modMult   = RunManager.Instance?.EffectiveCoinMultiplier ?? 1f;
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(coinMin, coinMax + 1) * floorMult * modMult));

        if (!_spawning && !_waveClearPending && ShouldClearWave())
            StartCoroutine(OnWaveCleared());
    }

    protected bool ShouldClearWave() => _aliveCount <= _waveClearThreshold;

    protected virtual IEnumerator OnWaveCleared()
    {
        if (_waveClearPending) yield break;
        _waveClearPending = true;

        isLocked = false;
        _currentWave++;

        if (_currentWave < waveCount)
        {
            yield return new WaitForSeconds(wavePause);
            _waveClearPending = false;   // reset before next wave starts
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
        StartCoroutine(SpawnWaveRoutine(waveIndex));
    }

    private IEnumerator SpawnWaveRoutine(int waveIndex)
    {
        _waveClearPending = false;
        _spawning         = true;

        var prefabs = BuildWavePrefabList(waveIndex);
        if (prefabs.Count == 0)
        {
            _spawning = false;
            StartCoroutine(OnWaveCleared());
            yield break;
        }

       
        var cells = new List<Vector3>(spawnCells);
        ShuffleList(cells);
        int cellIdx = 0;
        int spawned = 0;

        foreach (var prefab in prefabs)
        {
            Vector3 pos = cells.Count > 0
                ? cells[cellIdx++ % cells.Count] + Vector3.up * 0.5f
                : transform.position + Vector3.up * 0.5f;

            int n = SpawnEnemyPrefab(prefab, pos);
            _aliveCount += n;
            spawned     += n;
            yield return new WaitForSeconds(Random.Range(spawnDelayMin, spawnDelayMax));
        }

        _waveSpawnedCount = spawned;

       
        bool isLastWave = _currentWave >= waveCount - 1;
        _waveClearThreshold = isLastWave ? 0
            : Mathf.Max(1, Mathf.RoundToInt(spawned * Random.Range(waveNextThresholdMin, waveNextThresholdMax)));

        _spawning = false;

       
        if (isLocked && !isCleared && !_waveClearPending && ShouldClearWave())
            StartCoroutine(OnWaveCleared());
    }

    private List<GameObject> BuildWavePrefabList(int waveIndex)
        => BuildBudgetPrefabList(_waveBudgets[waveIndex], waveIndex);

    protected List<GameObject> BuildBudgetPrefabList(int budget, int waveIndex)
    {
        var result = new List<GameObject>();
        int safetyLimit = 200;
        bool groupSpawnerUsed = false;

        while (budget > 0 && safetyLimit-- > 0)
        {
            var affordable = new List<EnemyEntry>();
            foreach (var e in enemyEntries)
            {
                if (Mathf.Max(1, e.cost) > budget) continue;
                if (groupSpawnerUsed && IsGroupSpawnerEntry(e)) continue;
                affordable.Add(e);
            }
            if (affordable.Count == 0) break;

            var entry = affordable[Random.Range(0, affordable.Count)];
            bool useElite = _eliteBudgetsPerWave[waveIndex] > 0 && entry.elite != null;
            if (useElite) _eliteBudgetsPerWave[waveIndex]--;
            var prefab = useElite ? entry.elite : entry.normal;
            budget -= Mathf.Max(1, entry.cost);
            result.Add(prefab);
            if (prefab.GetComponent<IGroupSpawner>() != null) groupSpawnerUsed = true;
        }
        return result;
    }

    protected static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        { int j = Random.Range(0, i + 1); (list[i], list[j]) = (list[j], list[i]); }
    }

    protected int SpawnBudgetFill(int budget, int waveIndex)
    {
        int spawned = 0;
        int safetyLimit = 200;

        bool groupSpawnerUsed = false;

        while (budget > 0 && safetyLimit-- > 0)
        {
            var affordable = new List<EnemyEntry>();
            foreach (var e in enemyEntries)
            {
                if (Mathf.Max(1, e.cost) > budget) continue;
                if (groupSpawnerUsed && IsGroupSpawnerEntry(e)) continue;
                affordable.Add(e);
            }

            if (affordable.Count == 0) break;

            var entry = affordable[Random.Range(0, affordable.Count)];
            bool useElite = _eliteBudgetsPerWave[waveIndex] > 0 && entry.elite != null;
            if (useElite) _eliteBudgetsPerWave[waveIndex]--;
            var prefab = useElite ? entry.elite : entry.normal;
            budget -= Mathf.Max(1, entry.cost);
            spawned += SpawnEnemyPrefab(prefab);
            if (prefab.GetComponent<IGroupSpawner>() != null) groupSpawnerUsed = true;
        }

        return spawned;
    }

    protected int SpawnEnemyPrefab(GameObject prefab, Vector3 position)
    {
        var go = Instantiate(prefab, position, Quaternion.identity);
        var groupSpawner = go.GetComponent<IGroupSpawner>();
        if (groupSpawner != null)
        {
            groupSpawner.SetGroupStatScale(ComputeStatScale());
            groupSpawner.SetMissCallback(HandleGroupMiss);
            return groupSpawner.GetSpawnCount();
        }
        ApplyEnemyScale(go);
        return 1;
    }

    
    protected int SpawnEnemyPrefab(GameObject prefab)
        => SpawnEnemyPrefab(prefab, PickSpawnPosition());

    protected void HandleGroupMiss(int missed)
    {
        if (missed <= 0) return;
        _aliveCount = Mathf.Max(0, _aliveCount - missed);
        if (isLocked && !isCleared && !_spawning && !_waveClearPending && ShouldClearWave())
            StartCoroutine(OnWaveCleared());
       
    }

    protected static bool IsGroupSpawnerEntry(EnemyEntry e)
        => (e.normal != null && e.normal.GetComponent<IGroupSpawner>() != null)
        || (e.elite  != null && e.elite.GetComponent<IGroupSpawner>()  != null);

    protected Vector3 PickSpawnPosition()
    {
        if (spawnCells != null && spawnCells.Count > 0)
            return spawnCells[Random.Range(0, spawnCells.Count)] + Vector3.up * 0.5f;
        return transform.position + Vector3.up * 0.5f;
    }

    protected Vector3 PickLootPosition()
        => PickLootPosition(null);

    /// <param name="exclude">World position to avoid (e.g. portal). Cells within excludeRadius are skipped.</param>
    protected Vector3 PickLootPosition(Vector3? exclude, float excludeRadius = 1.5f)
    {
        if (spawnCells == null || spawnCells.Count == 0)
            return transform.position;

        float minX = transform.position.x - roomSize.x * 0.5f + lootInset;
        float maxX = transform.position.x + roomSize.x * 0.5f - lootInset;
        float minZ = transform.position.z - roomSize.z * 0.5f + lootInset;
        float maxZ = transform.position.z + roomSize.z * 0.5f - lootInset;

        var interior = new List<Vector3>();
        foreach (var cell in spawnCells)
        {
            if (cell.x < minX || cell.x > maxX || cell.z < minZ || cell.z > maxZ) continue;
            if (exclude.HasValue)
            {
                float dx = cell.x - exclude.Value.x;
                float dz = cell.z - exclude.Value.z;
                if (dx * dx + dz * dz < excludeRadius * excludeRadius) continue;
            }
            interior.Add(cell);
        }

        var pool = interior.Count > 0 ? interior : spawnCells;
        return pool[Random.Range(0, pool.Count)];
    }


    protected static StatScale BuildStatScale(
        PlayerStats player, RunManager rm,
        float speedMin,       float speedMax,
        float hpPerFloor,     float hpPerSegment,
        float dmgPerFloor,    float dmgPerSegment,
        float hpPlayerWeight, float dmgPlayerWeight)
    {
        int floor           = rm?.CurrentFloor ?? 1;
        int floorsPerSeg    = EnemyPoolManager.Instance?.floorsPerSegment ?? 3;
        int floorIndex      = Mathf.Max(0, floor - 1);          // 0 on floor 1
        int segmentIndex    = floorIndex / floorsPerSeg;         // 0 in segment 1

        float hpScale  = Mathf.Pow(hpPerFloor,  floorIndex) * Mathf.Pow(hpPerSegment,  segmentIndex);
        float dmgScale = Mathf.Pow(dmgPerFloor, floorIndex) * Mathf.Pow(dmgPerSegment, segmentIndex);

        float playerDmgRatio = player != null ? player.Damage    / Mathf.Max(1f, player.BaseDamage) : 0f;
        float playerHpRatio  = player != null ? player.MaxHealth / Mathf.Max(1f, player.BaseHealth) : 0f;

        return new StatScale
        {
            moveSpeed = Random.Range(speedMin, speedMax) * (rm?.EffectiveEnemySpeedMultiplier ?? 1f),
            hp        = hpScale  * (1f + playerDmgRatio * hpPlayerWeight)  * (rm?.EffectiveEnemyHpMultiplier     ?? 1f),
            damage    = dmgScale * (1f + playerHpRatio  * dmgPlayerWeight) * (rm?.EffectiveEnemyDamageMultiplier ?? 1f),
        };
    }

    protected StatScale ComputeStatScale()
        => BuildStatScale(
            FindFirstObjectByType<PlayerStats>(), RunManager.Instance,
            enemySpeedMin,         enemySpeedMax,
            enemyHpPerFloor,       enemyHpPerSegment,
            enemyDmgPerFloor,      enemyDmgPerSegment,
            enemyHpPlayerDmgWeight, enemyDmgPlayerHpWeight);

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

        if (area > waveReduceAreaThreshold)
            waveCount = Mathf.Max(1, waveCount - 1);
    }

    
    protected virtual void ApplyRunModifiers()
    {
        var rm = RunManager.Instance;
        if (rm == null) return;
        eliteBudget += rm.EffectiveEliteBudgetBonus;

        
        int extraWaves = rm.EffectiveExtraWaves;
        if (extraWaves > 0)
        {
            int totalWaves = waveCount + extraWaves;
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
        FindFirstObjectByType<MinimapManager>()?.OnBattleRoomCleared(node);
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
        float mean = lootBaseMean + floor * lootMeanPerFloor + roomsCleared * lootMeanPerRoom 
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
        int wallLayer = LayerMask.NameToLayer("Wall");
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
            if (wallLayer >= 0) wall.layer = wallLayer;
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
