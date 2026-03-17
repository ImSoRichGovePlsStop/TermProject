using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

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
            spawnedEnemies.RemoveAll(e => e == null);
            if (spawnedEnemies.Count == 0)
                ClearRoom();
        }
    }

    public void OnPlayerEnter()
    {
        if (!isCleared)
        {
            LockRoom();
            SpawnEnemies();
        }
    }


    public void SetRoomSize(Vector3 size)
    {
        roomSize = size;
    }
    private void CreateInvisibleWalls()
    {
        CreateRoomBoundary();

        (Vector3 pos, Vector3 size)[] wallConfigs = new[]
        {
        // Left
        (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.01f, roomSize.y, 2)),
        // Right
        (new Vector3(roomSize.x / 2f, roomSize.y / 2f, 0f),  new Vector3(0.01f, roomSize.y, 2)),
        // Front
        (new Vector3(0f, roomSize.y / 2f, roomSize.z / 2f),  new Vector3(2, roomSize.y, 0.01f)),
        // Back
        (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f), new Vector3(2, roomSize.y, 0.01f)),
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
        pbMesh.transform.localPosition = new Vector3(0f, - 0.01f, 0f);
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
        GameObject lootbox = Instantiate(lootPrefab, transform.position, Quaternion.identity);

        CurrencyManager wallet = Object.FindFirstObjectByType<CurrencyManager>();
        wallet.AddCoins(Random.Range(50, 251));

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
        if (other.CompareTag("Player"))
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