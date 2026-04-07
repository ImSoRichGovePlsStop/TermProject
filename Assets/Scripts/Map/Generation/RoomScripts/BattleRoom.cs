using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

    [HideInInspector] public RoomNode node;

    [Header("Room")]
    private Vector3 roomSize;
    private List<GameObject> invisibleWalls = new();

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public GameObject lootPrefab;
    public int enemyCount = 3;
    public float spawnRadius = 3f;

    [Header("Waves")]
    [Tooltip("How many waves to split the enemies into. 1 = original behaviour.")]
    [Range(1, 5)]
    public int waveCount = 3;
    public float wavePause = 1f;
    [Tooltip("Trigger next wave when alive enemies drop to or below this")]
    public int waveThreshold = 0;

    public Material boundaryMaterial;

    private int _aliveCount = 0;
    private PlayerCombatContext _combatContext;
    private int _currentWave = 0;
    private int[] _waveSizes;



    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _combatContext = playerObj.GetComponent<PlayerCombatContext>();
    }

    private void Subscribe() => _combatContext.OnEntityKilled += OnEntityKilled;
    private void Unsubscribe() => _combatContext.OnEntityKilled -= OnEntityKilled;
    private void OnDestroy() => Unsubscribe();

    private void OnEntityKilled(HealthBase enemy)
    {
        if (!isLocked || isCleared) return;
        _aliveCount = Mathf.Max(0, _aliveCount - 1);

        RunManager.Instance?.OnEnemyKilled();
        var enemyBase = enemy.GetComponent<EnemyBase>();
        int coinMin = enemyBase != null ? enemyBase.coinDropMin : 4;
        int coinMax = enemyBase != null ? enemyBase.coinDropMax : 9;
        int baseCoins = Random.Range(coinMin, coinMax + 1);
        float floorMultiplier = 1f + ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * 0.3f;
        Object.FindFirstObjectByType<CurrencyManager>()?.AddCoins(Mathf.RoundToInt(baseCoins * floorMultiplier));

        if (_aliveCount <= waveThreshold)
            StartCoroutine(OnWaveCleared());
    }



    IEnumerator OnWaveCleared()
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


    void BuildWaveSizes()
    {
        int clampedWaves = Mathf.Clamp(waveCount, 1, Mathf.Max(1, enemyCount));
        waveCount = clampedWaves;
        _waveSizes = new int[waveCount];

        int baseCount = enemyCount / waveCount;
        int remainder = enemyCount % waveCount;

        for (int i = 0; i < waveCount; i++)
            _waveSizes[i] = baseCount + (i < remainder ? 1 : 0);
    }

    void SpawnWave(int waveIndex)
    {


        int count = _waveSizes[waveIndex];
        _aliveCount += count;

        for (int i = 0; i < count; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(1f, spawnRadius);
            Vector3 pos = transform.position + new Vector3(circle.x, 0.5f, circle.y);
            GameObject currentEnemy = Instantiate(prefab, pos, Quaternion.identity);
            if (currentEnemy.TryGetComponent<EntityStats>(out EntityStats entityStats))
            {
                PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
                StatScale scale = new StatScale();
                scale.moveSpeed = Random.Range(0.9f, 1.1f);
                float statScale = (RunManager.Instance?.TotalBossKilled ?? 0) * 0.3f + (RunManager.Instance?.TotalRoomsCleared ?? 0) * 0.1f;
                scale.hp = 1f + statScale + (playerStats.Damage) / (playerStats.BaseDamage) * 0.15f;
                scale.damage = 1f + statScale + (playerStats.MaxHealth) / (playerStats.BaseHealth) * 0.15f;
                entityStats.SetStatScale(scale);
            }
        }
    }

    public void OnPlayerEnter()
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


    private void ClearRoom()
    {
        isCleared = true;
        isLocked = false;
        RemoveInvisibleWalls();

        var lootObj = Instantiate(lootPrefab, transform.position, Quaternion.identity);
        var randomLoot = lootObj.GetComponent<RandomLoot>();
        if (randomLoot != null)
        {
            int floor = RunManager.Instance?.CurrentFloor ?? 1;
            int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
            randomLoot.Configure(floor, roomsCleared);
        }



        RunManager.Instance?.OnRoomCleared();
    }



    public void SetRoomSize(Vector3 size) => roomSize = size;

    private void LockRoom()
    {
        isLocked = true;
        CreateInvisibleWalls();
    }

    private void CreateInvisibleWalls()
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

    private void RemoveInvisibleWalls()
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



    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}