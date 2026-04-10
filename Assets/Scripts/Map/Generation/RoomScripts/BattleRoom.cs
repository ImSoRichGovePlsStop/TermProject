using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

    [HideInInspector] public RoomNode node;

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public GameObject lootPrefab;
    public GameObject upgradeStationPrefab;
    public int enemyCount = 3;
    [HideInInspector] public System.Collections.Generic.List<Vector3> spawnCells = new();

    [Header("Waves")]
    [Range(1, 5)] public int waveCount = 3;
    public float wavePause = 1f;
    public int waveThreshold = 0;

    public Material boundaryMaterial;

    protected Vector3 roomSize;
    protected List<GameObject> invisibleWalls = new();
    protected int _aliveCount = 0;
    protected PlayerCombatContext _combatContext;
    protected int _currentWave = 0;
    protected int[] _waveSizes;

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
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : 4;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : 9;
        float floorMult = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * 0.3f;
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
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector3 pos = PickSpawnPosition();
            ApplyEnemyScale(Instantiate(prefab, pos, Quaternion.identity));
        }
    }

    protected Vector3 PickSpawnPosition()
    {
        if (spawnCells != null && spawnCells.Count > 0)
            return spawnCells[Random.Range(0, spawnCells.Count)] + Vector3.up * 0.5f;
        return transform.position + Vector3.up * 0.5f;
    }

    protected void ApplyEnemyScale(GameObject enemy)
    {
        if (!enemy.TryGetComponent<EntityStats>(out var stats)) return;
        var player = FindFirstObjectByType<PlayerStats>();
        var scale = new StatScale();
        float progress = (RunManager.Instance?.TotalBossKilled ?? 0) * 0.3f
                       + (RunManager.Instance?.TotalRoomsCleared ?? 0) * 0.1f;
        scale.moveSpeed = Random.Range(0.9f, 1.1f);
        scale.hp = 1f + progress + (player.Damage / Mathf.Max(1f, player.BaseDamage)) * 0.15f;
        scale.damage = 1f + progress + (player.MaxHealth / Mathf.Max(1f, player.BaseHealth)) * 0.15f;
        stats.SetStatScale(scale);
    }

    public virtual void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        if (isCleared) return;

        LockRoom();
        BuildWaveSizes();
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

        if (Random.value < 0.70f)
            SpawnLoot(PickSpawnPosition());
        else if (upgradeStationPrefab != null)
            Instantiate(upgradeStationPrefab, PickSpawnPosition(), Quaternion.identity);
        else
            SpawnLoot(PickSpawnPosition());

        RunManager.Instance?.OnRoomCleared();
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
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        float mean = 50f + floor * 30f + roomsCleared * 5f + 10f;
        float sd = 20f + (floor - 1) * 3f;
        return new LootConfig { optionCount = 3, meanCost = mean, sd = sd, allowDuplicates = false };
    }

    public void SetRoomSize(Vector3 size) => roomSize = size;

    protected void LockRoom()
    {
        isLocked = true;
        CreateInvisibleWalls();
    }

    protected void CreateInvisibleWalls()
    {
        (Vector3 pos, Vector3 size)[] configs =
        {
            (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f),  new Vector3(0.01f, roomSize.y, roomSize.z)),
            (new Vector3( roomSize.x / 2f, roomSize.y / 2f, 0f),  new Vector3(0.01f, roomSize.y, roomSize.z)),
            (new Vector3(0f, roomSize.y / 2f,  roomSize.z / 2f),  new Vector3(roomSize.x, roomSize.y, 0.01f)),
            (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f),  new Vector3(roomSize.x, roomSize.y, 0.01f)),
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
        float inset = 0.3f;
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