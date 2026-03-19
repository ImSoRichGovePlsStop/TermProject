using UnityEngine;
using UnityEngine.UIElements;

public class MapGenerator : MonoBehaviour
{
    [Header("Player")]
    public GameObject playerPrefab;
    public GameObject[] enemiesPrefab;
    public GameObject bossPrefab;
    public GameObject lootPrefab;

    [Header("room sizes")]
    public Vector2 spawnRoomSize = new Vector2(10f, 10f);
    public Vector2 battleRoomSize = new Vector2(10f, 10f);

    [Header("room spacing")]
    public float roomSpacing = 4f;
    public float triggerHeight = 3f;

    public Material boundaryMaterial;

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        
        CreateSpawnRoom();
 
        
        GameObject battleRoomObj = CreateBattleRoom(
            "BattleRoom",
            battleRoomSize,
            new Vector3(20, 0, 0),
            new GameObject[] { enemiesPrefab[1] }
        );


        GameObject battleRoomObj2 = CreateBattleRoom(
            "BattleRoom",
            battleRoomSize,
            new Vector3(40, 0, 0),
            new GameObject[] { enemiesPrefab[0], enemiesPrefab[1] },
            2
        );

        GameObject bossRoomObj = CreateBattleRoom(
            "BossRoom",
            battleRoomSize,
            new Vector3(60, 0, 0),
            new GameObject[] { enemiesPrefab[2] }
        );


    }

    GameObject CreateRoom(string name,  Vector3 position)
    {
        GameObject room = new GameObject(name);
        room.transform.position = position;

        return room;
    }


    GameObject CreateSpawnRoom()
    {
        GameObject roomObj = CreateRoom("SpawnRoom", new Vector3(0, 0, 0));
        SpawnRoom spawnRoom = roomObj.AddComponent<SpawnRoom>();
        return roomObj;
    }


    GameObject CreateBattleRoom(
        string name,
        Vector2 roomSize,
        Vector3 position,
        GameObject[] enemies,
        int enemyCount = 1)
    {
        GameObject roomObj = CreateRoom(name,  position);
        BattleRoom room = roomObj.AddComponent<BattleRoom>();

        room.enemyPrefabs = enemies;
        room.enemyCount = enemyCount;
        room.lootPrefab = lootPrefab;
        room.boundaryMaterial = boundaryMaterial;

        Vector3 roomVolume = new Vector3(roomSize.x, triggerHeight, roomSize.y);
        room.SetRoomSize(roomVolume);

        BoxCollider trigger = roomObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = roomVolume;
        trigger.center = new Vector3(0, triggerHeight / 2f, 0);

        return roomObj;
    }


}