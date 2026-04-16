using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRoom : BattleRoom
{
    [Header("Boss")]
    public GameObject bossPrefab;
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    [Header("Normal Enemy Spawns")]
    public float spawnInterval = 12f;
    public int spawnCountMin = 1;
    public int spawnCountMax = 3;
    public int maxAliveNormals = 6;

    [Header("Boss Scaling")]
    public float bossSpeedMin = 0.9f;
    public float bossSpeedMax = 1.1f;
    public float bossHpPerBossKill = 1.1f;
    public float bossHpPlayerDmgWeight = 0.05f;
    public float bossHpEnemyKillPenalty = 0.001f;
    public float bossDmgPerBossKill = 0.3f;
    public float bossDmgPlayerHpWeight = 0.05f;

    [Header("Boss Loot Config")]
    public float bossLootMeanMultiplier = 3f;
    public float bossLootPerBossKill = 50f;
    public float bossLootSdMultiplier = 0.2f;
    public int bossLootOptionCount = 1;

    [Header("Boss Clear Reward")]
    [Tooltip("Bonus coins awarded on boss clear, scaled same as enemy coin drop.")]
    public int bonusCoinMin = 100;
    public int bonusCoinMax = 300;
    public int maxFloor = 4;

    private GameObject _bossInstance;
    private List<GameObject> _normalEnemies = new();
    private Coroutine _spawnRoutine;

    const float LootOffsetZ = -2f;
    const float PortalOffsetZ = 2f;

    public override void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared || isLocked) return;

        LockRoom();
        Subscribe();
        SpawnBoss();

        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
            _spawnRoutine = StartCoroutine(PeriodicNormalSpawner());

        var ui = FindFirstObjectByType<UIManager>();
        ui.isInBattle = true;
        ui.CloseShop();
        if (ui.IsInventoryOpen) ui.ToggleInventory();
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null) return;

        _bossInstance = Instantiate(bossPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        _aliveCount = 1;

        if (_bossInstance.TryGetComponent<EntityStats>(out var stats))
        {
            var player = FindFirstObjectByType<PlayerStats>();
            var scale = new StatScale();
            scale.moveSpeed = Random.Range(bossSpeedMin, bossSpeedMax);
            scale.hp = 1f + (RunManager.Instance?.TotalBossKilled ?? 0) * bossHpPerBossKill
                              + (player.BaseDamage / Mathf.Max(1f, player.Damage)) * bossHpPlayerDmgWeight
                              - (RunManager.Instance?.TotalEnemyKilled ?? 0) * bossHpEnemyKillPenalty;
            scale.damage = 1f + (RunManager.Instance?.TotalBossKilled ?? 0) * bossDmgPerBossKill
                              + (player.MaxHealth / Mathf.Max(1f, player.BaseHealth)) * bossDmgPlayerHpWeight;
            stats.SetStatScale(scale);
        }
    }

    protected override void OnEntityKilled(HealthBase enemy)
    {
        if (!isLocked || isCleared) return;

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : fallbackCoinMin;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : fallbackCoinMax;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * coinFloorMultiplier;
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(coinMin, coinMax + 1) * floorMult));

        if (enemy.gameObject == _bossInstance)
        {
            _aliveCount = 0;
            StartCoroutine(OnWaveCleared());
        }
        else
        {
            _normalEnemies.RemoveAll(e => e == null);
        }
    }

    protected override IEnumerator OnWaveCleared()
    {
        if (_spawnRoutine != null) { StopCoroutine(_spawnRoutine); _spawnRoutine = null; }
        FindFirstObjectByType<UIManager>().isInBattle = false;
        Unsubscribe();
        ClearRoom();
        yield break;
    }

    private IEnumerator PeriodicNormalSpawner()
    {
        yield return new WaitForSeconds(spawnInterval);

        while (_bossInstance != null && !isCleared)
        {
            _normalEnemies.RemoveAll(e => e == null);
            int alreadyAlive = _normalEnemies.Count;
            int toSpawn = Random.Range(spawnCountMin, spawnCountMax + 1);

            for (int i = 0; i < toSpawn; i++)
            {
                if (alreadyAlive >= maxAliveNormals) break;
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                var enemy = Instantiate(prefab, PickSpawnPosition(), Quaternion.identity);
                ApplyEnemyScale(enemy);
                _normalEnemies.Add(enemy);
                alreadyAlive++;
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    protected override void ClearRoom()
    {
        isCleared = true;
        isLocked = false;
        RemoveInvisibleWalls();

        foreach (var e in _normalEnemies)
            if (e != null) Destroy(e);
        _normalEnemies.Clear();

        SpawnLoot(PickLootPosition());

        int nextFloor = (RunManager.Instance?.CurrentFloor ?? 1) + 1;
        var portal = nextFloor > maxFloor && portalFinalPrefab != null ? portalFinalPrefab : portalPrefab;
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
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        int bossKills = RunManager.Instance?.TotalBossKilled ?? 0;
        float baseMean = lootBaseMean + floor * lootMeanPerFloor + roomsCleared * lootMeanPerRoom + lootMeanFlat;
        float sd = lootBaseSd + (floor - 1) * lootSdPerFloor;
        return new LootConfig
        {
            optionCount = bossLootOptionCount,
            meanCost = baseMean * bossLootMeanMultiplier + bossKills * bossLootPerBossKill,
            sd = sd * bossLootSdMultiplier,
            allowDuplicates = false,
        };
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
    }
}