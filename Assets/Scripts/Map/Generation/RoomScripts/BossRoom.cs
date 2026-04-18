using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRoom : BattleRoom
{
    public enum BossRoomMode { MiniBoss, EliteBattle, TrueBoss }

    [Header("Portals")]
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    [Header("Boss Scaling")]
    public float bossSpeedMin           = 0.9f;
    public float bossSpeedMax           = 1.1f;
    public float bossHpPerBossKill      = 1.1f;
    public float bossHpPlayerDmgWeight  = 0.00f;
    public float bossHpEnemyKillPenalty = 0.000f;
    public float bossDmgPerBossKill     = 0.3f;
    public float bossDmgPlayerHpWeight  = 0.00f;

    [Header("Mini-Boss Scaling")]
    [Tooltip("HP multiplier applied on top of normal scaling for the guaranteed elite.")]
    public float miniBossHpScale  = 3f;
    [Tooltip("Damage multiplier applied on top of normal scaling for the guaranteed elite.")]
    public float miniBossDmgScale = 2f;

    [Header("Non-Boss Wave Budget")]
    [Tooltip("Fraction of the normal battle-room budget used for MiniBoss / EliteBattle waves. " +
             "Lower values mean fewer enemies.")]
    [Range(0.1f, 1f)]
    public float eliteWaveBudgetMult = 0.6f;

    // ── Loot (all modes share this) ───────────────────────────────────────────
    [Header("Boss Loot Config")]
    public float bossLootMeanMultiplier = 3f;
    public float bossLootPerBossKill    = 50f;
    public float bossLootSdMultiplier   = 0.2f;
    public int   bossLootOptionCount    = 1;

    [Header("Boss Clear Reward")]
    [Tooltip("Bonus coins awarded on boss clear, scaled per floor.")]
    public int bonusCoinMin = 100;
    public int bonusCoinMax = 300;
    public int maxFloor     = 9;

    private BossRoomMode          _mode;
    private List<GameObject>      _bossInstances  = new();
    private List<GameObject>      _normalEnemies  = new();
    private Coroutine             _spawnRoutine;

    const float PortalOffsetZ = 2f;


    BossRoomMode GetMode()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool  = EnemyPoolManager.Instance;
        int fps   = pool != null ? pool.floorsPerSegment : 3;
        int floorInSeg = ((floor - 1) % fps) + 1;

        if (fps == 1 || floorInSeg == fps) return BossRoomMode.TrueBoss;
        if (floorInSeg == 1)               return BossRoomMode.MiniBoss;
        return BossRoomMode.EliteBattle;
    }


    public override void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared || isLocked) return;

        _mode = GetMode();

        if (_mode == BossRoomMode.TrueBoss)
        {
            int floor  = RunManager.Instance?.CurrentFloor ?? 1;
            var config = EnemyPoolManager.Instance?.GetBossConfig(floor);

            LockRoom();
            Subscribe();
            SpawnAllBosses(config);

            if (config?.periodicEntries != null && config.periodicEntries.Length > 0)
                _spawnRoutine = StartCoroutine(PeriodicNormalSpawner(config));
        }
        else
        {
            if (_totalBudget == 0) CalculateTotalBudget(roomSize);
            _totalBudget = Mathf.Max(1, Mathf.RoundToInt(_totalBudget * eliteWaveBudgetMult));
            ApplyRunModifiers();
            LockRoom();
            BuildWaveBudgets();
            _currentWave = 0;
            _aliveCount  = 0;
            Subscribe();
            SpawnWave(0);
        }

        var ui = FindFirstObjectByType<UIManager>();
        ui.isInBattle = true;
        ui.CloseShop();
        if (ui.IsInventoryOpen) ui.ToggleInventory();
    }


    void SpawnAllBosses(SegmentBossConfig config)
    {
        if (config?.bossPrefabs == null || config.bossPrefabs.Length == 0)
        {
            _aliveCount = 0;
            return;
        }

        var player = FindFirstObjectByType<PlayerStats>();
        var rm     = RunManager.Instance;

        foreach (var prefab in config.bossPrefabs)
        {
            if (prefab == null) continue;

            var go = Instantiate(prefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            _bossInstances.Add(go);

            if (go.TryGetComponent<EntityStats>(out var stats))
            {
                var scale = new StatScale();
                scale.moveSpeed = Random.Range(bossSpeedMin, bossSpeedMax);
                scale.hp = 1f
                    + (rm?.TotalBossKilled ?? 0) * bossHpPerBossKill
                    + (player != null ? player.BaseDamage / Mathf.Max(1f, player.Damage) : 0f) * bossHpPlayerDmgWeight
                    - (rm?.TotalEnemyKilled ?? 0) * bossHpEnemyKillPenalty;
                scale.damage = 1f
                    + (rm?.TotalBossKilled ?? 0) * bossDmgPerBossKill
                    + (player != null ? player.MaxHealth / Mathf.Max(1f, player.BaseHealth) : 0f) * bossDmgPlayerHpWeight;
                stats.SetStatScale(scale);
            }
        }

        _aliveCount = _bossInstances.Count;
    }

    private IEnumerator PeriodicNormalSpawner(SegmentBossConfig config)
    {
        yield return new WaitForSeconds(config.spawnInterval);

        while (_bossInstances.Count > 0 && !isCleared)
        {
            _normalEnemies.RemoveAll(e => e == null);
            int alreadyAlive = _normalEnemies.Count;
            int toSpawn      = Random.Range(config.spawnCountMin, config.spawnCountMax + 1);

            for (int i = 0; i < toSpawn; i++)
            {
                if (alreadyAlive >= config.maxAliveNormals) break;
                var entry    = config.periodicEntries[Random.Range(0, config.periodicEntries.Length)];
                bool useElite = eliteBudget > 0 && entry.elite != null;
                if (useElite) eliteBudget--;
                var enemy = Instantiate(useElite ? entry.elite : entry.normal,
                                        PickSpawnPosition(), Quaternion.identity);
                ApplyEnemyScale(enemy);
                _normalEnemies.Add(enemy);
                alreadyAlive++;
            }

            yield return new WaitForSeconds(config.spawnInterval);
        }
    }


    protected override void SpawnWave(int waveIndex)
    {
        switch (_mode)
        {
            case BossRoomMode.MiniBoss:    SpawnMiniBossWave(waveIndex);    break;
            case BossRoomMode.EliteBattle: SpawnEliteBattleWave(waveIndex); break;
        }
    }

    void SpawnMiniBossWave(int waveIndex)
    {
        if (enemyEntries == null || enemyEntries.Length == 0)
        {
            StartCoroutine(OnWaveCleared());
            return;
        }

        int budget  = _waveBudgets[waveIndex];
        int spawned = 0;

        EnemyEntry topEntry = null;
        int        topCost  = -1;
        foreach (var e in enemyEntries)
        {
            int c = Mathf.Max(1, e.cost);
            if (e.elite != null && c > topCost) { topCost = c; topEntry = e; }
        }

        if (topEntry != null)
        {
            var go = Instantiate(topEntry.elite, PickSpawnPosition(), Quaternion.identity);
            if (go.TryGetComponent<EntityStats>(out var stats))
            {
                var scale    = ComputeStatScale();
                scale.hp    *= miniBossHpScale;
                scale.damage *= miniBossDmgScale;
                stats.SetStatScale(scale);
            }
            budget -= topCost;
            spawned++;
        }

        spawned += SpawnBudgetFill(budget, waveIndex);

        _aliveCount += spawned;
        if (spawned == 0) StartCoroutine(OnWaveCleared());
    }

    void SpawnEliteBattleWave(int waveIndex)
    {
        if (enemyEntries == null || enemyEntries.Length == 0)
        {
            StartCoroutine(OnWaveCleared());
            return;
        }

        int spawned = SpawnEliteBudget(_waveBudgets[waveIndex], waveIndex);
        _aliveCount += spawned;
        if (spawned == 0) StartCoroutine(OnWaveCleared());
    }

    int SpawnEliteBudget(int budget, int waveIndex)
    {
        int spawned    = 0;
        int safetyLimit = 200;

        while (budget > 0 && safetyLimit-- > 0)
        {
            var affordable = new List<EnemyEntry>();
            foreach (var e in enemyEntries)
                if (Mathf.Max(1, e.cost) <= budget) affordable.Add(e);

            EnemyEntry entry;
            if (affordable.Count == 0)
            {
                // Filler: cheapest entry
                entry = null;
                int lowestCost = int.MaxValue;
                foreach (var e in enemyEntries)
                {
                    int c = Mathf.Max(1, e.cost);
                    if (c < lowestCost) { lowestCost = c; entry = e; }
                }
                if (entry == null) break;
                spawned += InstantiateEliteEntry(entry);
                break; // filler always ends the loop
            }

            entry = affordable[Random.Range(0, affordable.Count)];
            budget -= Mathf.Max(1, entry.cost);
            spawned += InstantiateEliteEntry(entry);
        }

        return spawned;
    }

    int InstantiateEliteEntry(EnemyEntry entry)
    {
        var prefab = entry.elite != null ? entry.elite : entry.normal;
        return SpawnEnemyPrefab(prefab);
    }


    protected override void OnEntityKilled(HealthBase enemy)
    {
        if (!isLocked || isCleared) return;

        if (_mode != BossRoomMode.TrueBoss)
        {
            base.OnEntityKilled(enemy);
            return;
        }

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin   = enemyBase != null ? enemyBase.coinDropMin : fallbackCoinMin;
        int coinMax   = enemyBase != null ? enemyBase.coinDropMax : fallbackCoinMax;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * coinFloorMultiplier;
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(coinMin, coinMax + 1) * floorMult));

        if (_bossInstances.Remove(enemy.gameObject))
        {
            if (_bossInstances.Count == 0)
            {
                _aliveCount = 0;
                StartCoroutine(OnWaveCleared());
            }
        }
        else
        {
            _normalEnemies.RemoveAll(e => e == null);
        }
    }


    protected override IEnumerator OnWaveCleared()
    {
        if (_mode == BossRoomMode.TrueBoss)
        {
            if (_spawnRoutine != null) { StopCoroutine(_spawnRoutine); _spawnRoutine = null; }
            FindFirstObjectByType<UIManager>().isInBattle = false;
            Unsubscribe();
            ClearRoom();
            yield break;
        }

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

    protected override void ClearRoom()
    {
        isCleared = true;
        isLocked  = false;
        RemoveInvisibleWalls();

        foreach (var e in _normalEnemies)
            if (e != null) Destroy(e);
        _normalEnemies.Clear();

        SpawnLoot(PickLootPosition());

        int nextFloor  = (RunManager.Instance?.CurrentFloor ?? 1) + 1;
        var pool       = EnemyPoolManager.Instance;
        int totalFloors = pool != null ? pool.segmentCount * pool.floorsPerSegment : maxFloor;
        var portal     = (nextFloor > totalFloors && portalFinalPrefab != null)
                         ? portalFinalPrefab : portalPrefab;
        if (portal != null)
            Instantiate(portal, transform.position + new Vector3(0f, 0f, PortalOffsetZ), Quaternion.identity);

        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * coinFloorMultiplier;
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(bonusCoinMin, bonusCoinMax + 1) * floorMult));

        RunManager.Instance?.OnBossKilled();
        HealPlayerAfterRoom();
        FindFirstObjectByType<MinimapManager>()?.OnBossDefeated(node);
    }

    protected override LootConfig BuildLootConfig()
    {
        int floor        = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        int bossKills    = RunManager.Instance?.TotalBossKilled ?? 0;
        float baseMean   = lootBaseMean + floor * lootMeanPerFloor + roomsCleared * lootMeanPerRoom + lootMeanFlat;
        float sd         = lootBaseSd + (floor - 1) * lootSdPerFloor;
        return new LootConfig
        {
            optionCount    = bossLootOptionCount,
            meanCost       = baseMean * bossLootMeanMultiplier + bossKills * bossLootPerBossKill,
            sd             = sd * bossLootSdMultiplier,
            allowDuplicates = false,
        };
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
    }
}
