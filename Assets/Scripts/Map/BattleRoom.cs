using System.Collections.Generic;
using UnityEngine;

public class BattleRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked = false;
    public bool isCleared = false;

 
    public Vector3 roomSize = new Vector3(10f, 5f, 10f);
    private List<GameObject> invisibleWalls = new List<GameObject>();

    public Vector3 monsterSpawnOffset = new Vector3(3f, 0f, 0f);

    private Transform player;
    private MonsterDummy spawnedMonster;

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
            if (spawnedMonster == null)
                ClearRoom();
        }
    }

    public void OnPlayerEnter()
    {
        if (!isCleared)
        {
            LockRoom();
            SpawnDummyMonster();
        }

    }



    private void CreateInvisibleWalls()
    {
        (Vector3 pos, Vector3 size)[] wallConfigs = new[]
        {
            //  invisible walls
            (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f),       new Vector3(0.1f, roomSize.y, roomSize.z)),
            (new Vector3( roomSize.x / 2f, roomSize.y / 2f, 0f),       new Vector3(0.1f, roomSize.y, roomSize.z)),
            (new Vector3(0f, roomSize.y / 2f,  roomSize.z / 2f),       new Vector3(roomSize.x, roomSize.y, 0.1f)),
            (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f),       new Vector3(roomSize.x, roomSize.y, 0.1f)),
            (new Vector3(0f, 0f, 0f),                                   new Vector3(roomSize.x, 0.1f, roomSize.z)),
            (new Vector3(0f, roomSize.y, 0f),                          new Vector3(roomSize.x, 0.1f, roomSize.z)),
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
        {
            if (wall != null)
                Destroy(wall);
        }
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
     
    }



    private void SpawnDummyMonster()
    {
        Vector3 spawnPosition = transform.position + monsterSpawnOffset;

        GameObject monsterObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        monsterObj.name = "DummyMonster";
        monsterObj.transform.position = spawnPosition;

        spawnedMonster = monsterObj.AddComponent<MonsterDummy>();

    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            OnPlayerEnter();
    }
}