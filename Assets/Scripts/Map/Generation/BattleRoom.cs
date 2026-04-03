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
    private List<GameObject> invisibleWalls = new List<GameObject>();

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public GameObject lootPrefab;
    public int enemyCount = 3;
    public float spawnRadius = 3f;

    private Transform player;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    public Material boundaryMaterial;




    void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    void Update()
    {
        if (isLocked && !isCleared)
        {
            int before = spawnedEnemies.Count;
            spawnedEnemies.RemoveAll(e => e == null);
            int killed = before - spawnedEnemies.Count;

            for (int i = 0; i < killed; i++)
                RunManager.Instance?.OnEnemyKilled();

            if (spawnedEnemies.Count == 0)
            {
                UIManager _uiManager = FindFirstObjectByType<UIManager>();
                _uiManager.isInBattle = false;
                ClearRoom();
            }
        }
    }

    public void OnPlayerEnter()
    {
        
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);

        if (!isCleared)
        {
            LockRoom();
            SpawnEnemies();
            UIManager _uiManager = FindFirstObjectByType<UIManager>();
            _uiManager.isInBattle = true;
            _uiManager.CloseShop();

            if (_uiManager.IsInventoryOpen)
                _uiManager.ToggleInventory();
        }
    }


    public void SetRoomSize(Vector3 size)
    {
        roomSize = size;
    }
    private void CreateInvisibleWalls()
    {
        //CreateRoomBoundary();

        (Vector3 pos, Vector3 size)[] wallConfigs = new[]
        {
        // Left
        (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.01f, roomSize.y, roomSize.z)),
        // Right
        (new Vector3(roomSize.x / 2f, roomSize.y / 2f, 0f),  new Vector3(0.01f, roomSize.y, roomSize.z)),
        // Front
        (new Vector3(0f, roomSize.y / 2f, roomSize.z / 2f),  new Vector3(roomSize.x, roomSize.y, 0.01f)),
        // Back
        (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, 0.01f)),
    };

        foreach (var (localPos, size) in wallConfigs)
        {
            GameObject wall = new GameObject("InvisibleWall");
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;

            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = size;

            invisibleWalls.Add(wall);
        }
    }

    private void CreateRoomBoundary()
    {
        ProBuilderMesh pbMesh = ProBuilderMesh.Create();
        PolyShape polyShape = pbMesh.gameObject.AddComponent<PolyShape>();

        float halfX = (roomSize.x / 2f) + 0.01f;
        float halfZ = (roomSize.z / 2f) + 0.01f;

        polyShape.SetControlPoints(new Vector3[]
        {
        new Vector3(-halfX, 0, -halfZ),
        new Vector3(-halfX, 0,  halfZ),
        new Vector3( halfX, 0,  halfZ),
        new Vector3( halfX, 0, -halfZ),
        });

        polyShape.extrude = 10;
        polyShape.flipNormals = true;

        pbMesh.CreateShapeFromPolygon(polyShape.controlPoints, polyShape.extrude, polyShape.flipNormals);
        pbMesh.transform.SetParent(transform);
        pbMesh.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        pbMesh.ToMesh();
        pbMesh.Refresh();

        if (boundaryMaterial != null)
            pbMesh.GetComponent<Renderer>().material = boundaryMaterial;

        invisibleWalls.Add(pbMesh.gameObject);
    }

    private void RemoveInvisibleWalls()
    {
        foreach (GameObject wall in invisibleWalls)
            if (wall != null) Destroy(wall);
        invisibleWalls.Clear();
    }

    private void LockRoom()
    {
        isLocked = true;
        CreateInvisibleWalls();
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
            int roomsCleared = RunManager.Instance?.TotalEnemyKilled ?? 0;
            randomLoot.Configure(floor, roomsCleared);
        }

        int baseCoins = Random.Range(25, 75);
        int bonusCoins = ((RunManager.Instance?.CurrentFloor ?? 1) - 1) * 25;

        CurrencyManager wallet = Object.FindFirstObjectByType<CurrencyManager>();
        wallet.AddCoins(baseCoins + bonusCoins);
        RunManager.Instance.OnRoomCleared();
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("enemy prefabs missing");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {

            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];


            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1f, spawnRadius);
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0.5f, randomCircle.y);

            GameObject enemy = Instantiate(prefab, spawnPosition, Quaternion.identity);
            spawnedEnemies.Add(enemy);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        StartCoroutine(WaitForPlayerInside(other.transform));
    }

    private System.Collections.IEnumerator WaitForPlayerInside(Transform playerTransform)
    {
        
        float inset = 0.3f;
        while (true)
        {
            Vector3 playerFlat = new Vector3(playerTransform.position.x, transform.position.y, playerTransform.position.z);
            Vector3 roomMin = transform.position - new Vector3(roomSize.x / 2f - inset, 0, roomSize.z / 2f - inset);
            Vector3 roomMax = transform.position + new Vector3(roomSize.x / 2f - inset, 0, roomSize.z / 2f - inset);

            bool insideX = playerFlat.x >= roomMin.x && playerFlat.x <= roomMax.x;
            bool insideZ = playerFlat.z >= roomMin.z && playerFlat.z <= roomMax.z;

            if (insideX && insideZ) break;

            float distFromCenter = Vector3.Distance(playerFlat, transform.position);
            float maxDist = Mathf.Max(roomSize.x, roomSize.z);
            if (distFromCenter > maxDist) yield break;

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