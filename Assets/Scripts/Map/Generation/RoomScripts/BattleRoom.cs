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

    public Material boundaryMaterial;


    private Transform _player;
    private List<GameObject> _activeEnemies = new();
    private int _currentWave = 0;  
    private int[] _waveSizes;        

 

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
    }

    void Update()
    {
        if (!isLocked || isCleared) return;

        int before = _activeEnemies.Count;
        _activeEnemies.RemoveAll(e => e == null);
        int killed = before - _activeEnemies.Count;

        for (int i = 0; i < killed; i++)
            RunManager.Instance?.OnEnemyKilled();

        if (_activeEnemies.Count == 0)
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
     

        for (int i = 0; i < count; i++)
        {
            var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
            Vector2 circle = Random.insideUnitCircle.normalized * Random.Range(1f, spawnRadius);
            Vector3 pos = transform.position + new Vector3(circle.x, 0.5f, circle.y);
            _activeEnemies.Add(Instantiate(prefab, pos, Quaternion.identity));
        }
    }

    public void OnPlayerEnter()
    {
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);

        if (isCleared) return;

        LockRoom();

        BuildWaveSizes();
        _currentWave = 0;
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

        int baseCoins = Random.Range(25, 75);
        int bonusCoins = ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * 25;
        Object.FindFirstObjectByType<CurrencyManager>()?.AddCoins(baseCoins + bonusCoins);

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