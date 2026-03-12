using System.Collections.Generic;
using UnityEngine;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

    [Header("Room")]
    public Vector3 roomSize = new Vector3(10f, 5f, 10f);
    private List<GameObject> invisibleWalls = new List<GameObject>();

    [Header("Enemy Spawning")]
    public GameObject[] enemyPrefabs;
    public int enemyCount = 3;
    public float spawnRadius = 3f;

    private Transform player;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

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

    private void CreateInvisibleWalls()
    {
        (Vector3 pos, Vector3 size)[] wallConfigs = new[]
        {
            (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.1f, roomSize.y, roomSize.z)),
            (new Vector3( roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.1f, roomSize.y, roomSize.z)),
            (new Vector3(0f, roomSize.y / 2f,  roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, 0.1f)),
            (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, 0.1f)),
            (new Vector3(0f, 0f, 0f),                            new Vector3(roomSize.x, 0.1f, roomSize.z)),
            (new Vector3(0f, roomSize.y, 0f),                    new Vector3(roomSize.x, 0.1f, roomSize.z)),
        };

        foreach (var (localPos, size) in wallConfigs)
        {
            GameObject wall = new GameObject("InvisibleWall");
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.size = size;
            col.isTrigger = false;
            invisibleWalls.Add(wall);
        }
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
        Debug.Log("Battle room cleared!");
    }

    private void SpawnEnemies()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No enemy prefabs assigned to BattleRoom!");
            return;
        }

        for (int i = 0; i < enemyCount; i++)
        {
            // Pick a random prefab from the array
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // Random position within spawnRadius, avoiding center
            Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(1f, spawnRadius);
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

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