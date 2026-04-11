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

    private GameObject _bossInstance;
    private List<GameObject> _normalEnemies = new();
    private Coroutine _spawnRoutine;

    public override void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared) return;

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
            scale.moveSpeed = Random.Range(0.9f, 1.1f);
            scale.hp = 1f + (RunManager.Instance?.TotalBossKilled ?? 0) * 1.1f
                                 + (player.BaseDamage / Mathf.Max(1f, player.Damage)) * 0.05f
                                 - (RunManager.Instance?.TotalEnemyKilled ?? 0) * 0.001f;
            scale.damage = 1f + (RunManager.Instance?.TotalBossKilled ?? 0) * 0.3f
                                 + (player.MaxHealth / Mathf.Max(1f, player.BaseHealth)) * 0.05f;
            stats.SetStatScale(scale);
        }
    }

    protected override void OnEntityKilled(HealthBase enemy)
    {
        if (!isLocked || isCleared) return;

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : 4;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : 9;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * 0.3f;
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

        SpawnLoot(transform.position + new Vector3(0f, 0f, -2f));

        int nextFloor = (RunManager.Instance?.CurrentFloor ?? 1) + 1;
        var portal = nextFloor > 4 && portalFinalPrefab != null ? portalFinalPrefab : portalPrefab;
        Instantiate(portal, transform.position + new Vector3(0f, 0f, 2f), Quaternion.identity);

        Object.FindFirstObjectByType<CurrencyManager>()?.AddCoins(Random.Range(100, 300));
        RunManager.Instance?.OnBossKilled();
    }

    protected override LootConfig BuildLootConfig()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        int bossKills = RunManager.Instance?.TotalBossKilled ?? 0;
        float baseMean = 50f + floor * 30f + roomsCleared * 5f + 10f;
        return new LootConfig
        {
            optionCount = 1,
            meanCost = baseMean * 3f + bossKills * 50f,
            sd = (20f + (floor - 1) * 3f) * 0.2f,
            allowDuplicates = false,
        };
    }

    protected override void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
    }
}