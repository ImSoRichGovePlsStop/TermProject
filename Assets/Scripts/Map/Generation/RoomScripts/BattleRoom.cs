using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked  = false;
    public bool isCleared = false;

    [HideInInspector] public RoomNode node;

    [Header("Enemy Spawning")]
    public EnemyEntry[] enemyEntries;
    public GameObject   lootPrefab;
    public GameObject   upgradeStationPrefab;
    public int          enemyCount = 3;
    [HideInInspector] public List<Vector3> spawnCells = new();

    public int eliteBudget = 0;

    [Header("Enemy Count Scaling")]
    [Tooltip("Area divisor used when scaling enemy count to room size.")]
    public float enemyAreaDivisor  = 60f;
    [Tooltip("How strongly room size scales enemy count beyond the base.")]
    public float enemyScaleFactor  = 0.3f;
    [Tooltip("Minimum random base enemy count.")]
    public float enemyCountMin     = 8f;
    [Tooltip("Maximum random base enemy count.")]
    public float enemyCountMax     = 12f;

    [Header("Waves")]
    [Range(1, 5)] public int waveCount     = 3;
    public float             wavePause     = 1f;
    public int               waveThreshold = 0;

    [Header("Reward")]
    [Tooltip("Chance to spawn loot instead of upgrade station on room clear.")]
    [Range(0f, 1f)]
    public float lootChance = 0.5f;

    [Header("Loot Config")]
    public int   lootOptionCount   = 3;
    public float lootBaseMean      = 50f;
    public float lootMeanPerFloor  = 30f;
    public float lootMeanPerRoom   = 5f;
    public float lootMeanFlat      = 10f;
    public float lootBaseSd        = 20f;
    public float lootSdPerFloor    = 3f;

    [Header("Coin Drop Fallback")]
    [Tooltip("Fallback coin min when enemy has no EnemyBase.")]
    public int   fallbackCoinMin     = 4;
    [Tooltip("Fallback coin max when enemy has no EnemyBase.")]
    public int   fallbackCoinMax     = 9;
    [Tooltip("Additional coin multiplier per floor beyond floor 1.")]
    public float coinFloorMultiplier = 0.3f;

    [Header("Enemy Scaling")]
    public float enemySpeedMin          = 0.9f;
    public float enemySpeedMax          = 1.1f;
    public float enemyProgressBossWeight = 0.3f;
    public float enemyProgressRoomWeight = 0.1f;
    public float enemyHpPlayerDmgWeight  = 0.15f;
    public float enemyDmgPlayerHpWeight  = 0.15f;

    public Material boundaryMaterial;

    protected Vector3        roomSize;
    protected List<GameObject> invisibleWalls = new();
    protected int            _aliveCount = 0;
    protected PlayerCombatContext _combatContext;
    protected int            _currentWave = 0;
    protected int[]          _waveSizes;

    const float TriggerInset      = 0.3f;
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
        Object.FindFirstObjectByType<CurrencyManager>()
              ?.AddCoins(Mathf.RoundToInt(Random.Range(coinMin, coinMax + 1) * floorMult));

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

    protected void BuildWaveSizes()
    {
        waveCount = Mathf.Clamp(waveCount, 1, Mathf.Max(1, enemyCount));
        _waveSizes = new int[waveCount];
        int baseCount = enemyCount / waveCount;
        int remainder = enemyCount % waveCount;
        for (int i = 0; i < waveCount; i++)
            _waveSizes[i] = baseCount + (i < remainder ? 1 : 0);
    }

    protected virtual void SpawnWave(int waveIndex)
    {
        int count = _waveSizes[waveIndex];
        _aliveCount += count;
        for (int i = 0; i < count; i++)
        {
            var entry      = enemyEntries[Random.Range(0, enemyEntries.Length)];
            bool useElite  = eliteBudget > 0 && entry.elite != null;
            if (useElite) eliteBudget--;
            var prefab     = useElite ? entry.elite : entry.normal;
            ApplyEnemyScale(Instantiate(prefab, PickSpawnPosition(), Quaternion.identity));
        }
    }

    protected Vector3 PickSpawnPosition()
    {
        if (spawnCells != null && spawnCells.Count > 0)
            return spawnCells[Random.Range(0, spawnCells.Count)] + Vector3.up * 0.5f;
        return transform.position + Vector3.up * 0.5f;
    }

    protected Vector3 PickLootPosition()
    {
        if (spawnCells != null && spawnCells.Count > 0)
            return spawnCells[Random.Range(0, spawnCells.Count)];
        return transform.position;
    }

    protected void ApplyEnemyScale(GameObject enemy)
    {
        if (!enemy.TryGetComponent<EntityStats>(out var stats)) return;
        var player = FindFirstObjectByType<PlayerStats>();
        var scale  = new StatScale();
        float progress = (RunManager.Instance?.TotalBossKilled ?? 0) * enemyProgressBossWeight
                       + (RunManager.Instance?.TotalRoomsCleared ?? 0) * enemyProgressRoomWeight;
        scale.moveSpeed = Random.Range(enemySpeedMin, enemySpeedMax);
        scale.hp        = 1f + progress + (player.Damage / Mathf.Max(1f, player.BaseDamage)) * enemyHpPlayerDmgWeight;
        scale.damage    = 1f + progress + (player.MaxHealth / Mathf.Max(1f, player.BaseHealth)) * enemyDmgPlayerHpWeight;
        stats.SetStatScale(scale);
    }

    public int ScaleEnemyCount(Vector3 vol)
    {
        float scale = (((vol.x * vol.z) / enemyAreaDivisor) - 1f) * enemyScaleFactor + 1f;
        return (int)(Random.Range(enemyCountMin, enemyCountMax) * scale);
    }

    public virtual void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared || isLocked) return;

        LockRoom();
        BuildWaveSizes();
        _currentWave = 0;
        _aliveCount  = 0;
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
        isLocked  = false;
        RemoveInvisibleWalls();

        bool firstRoom = (RunManager.Instance?.TotalRoomsCleared ?? 0) == 0;
        if (firstRoom || Random.value < lootChance)
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
        var lootObj    = Instantiate(lootPrefab, position, Quaternion.identity);
        var randomLoot = lootObj.GetComponent<RandomLoot>();
        if (randomLoot != null)
            randomLoot.Configure(BuildLootConfig());
    }

    protected virtual LootConfig BuildLootConfig()
    {
        int floor        = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        float mean = lootBaseMean + floor * lootMeanPerFloor + roomsCleared * lootMeanPerRoom + lootMeanFlat;
        float sd   = lootBaseSd  + (floor - 1) * lootSdPerFloor;
        return new LootConfig { optionCount = lootOptionCount, meanCost = mean, sd = sd, allowDuplicates = false };
    }

    protected void HealPlayerAfterRoom()
    {
        float healPercent = RunManager.Instance?.HealPerRoom ?? 0f;
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
            Vector3 flat   = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
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
