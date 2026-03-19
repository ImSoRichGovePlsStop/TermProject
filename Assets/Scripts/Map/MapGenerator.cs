using UnityEngine;

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
        

        GameObject spawnRoomObj = CreateRoom("SpawnRoom", spawnRoomSize, new Vector3(0, 0, 0));
        SpawnRoom spawnRoom = spawnRoomObj.AddComponent<SpawnRoom>();
        spawnRoom.playerPrefab = playerPrefab;

        float battleRoomX = (spawnRoomSize.x / 2f) + roomSpacing + (battleRoomSize.x / 2f);
        GameObject battleRoomObj = CreateRoom("BattleRoom", battleRoomSize, new Vector3(battleRoomX, 0, 0));
        battleRoomObj.AddComponent<BattleRoom>();
        battleRoomObj.GetComponent<BattleRoom>().enemyPrefabs = new GameObject[] { enemiesPrefab[1] };
        battleRoomObj.GetComponent<BattleRoom>().lootPrefab = lootPrefab;
        battleRoomObj.GetComponent<BattleRoom>().boundaryMaterial = boundaryMaterial;

        BoxCollider trigger = battleRoomObj.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = new Vector3(battleRoomSize.x, triggerHeight, battleRoomSize.y);
        trigger.center = new Vector3(0, triggerHeight / 2f, 0);

        GameObject battleRoomObj2 = CreateRoom("BattleRoom", battleRoomSize, new Vector3(40, 0, 0));
        battleRoomObj2.AddComponent<BattleRoom>();
        battleRoomObj2.GetComponent<BattleRoom>().enemyPrefabs =  new GameObject[] { enemiesPrefab[0],enemiesPrefab[1] };
        battleRoomObj2.GetComponent<BattleRoom>().enemyCount = 2;
        battleRoomObj2.GetComponent<BattleRoom>().lootPrefab = lootPrefab;
        battleRoomObj2.GetComponent<BattleRoom>().boundaryMaterial = boundaryMaterial;

        BoxCollider trigger1 = battleRoomObj2.AddComponent<BoxCollider>();
        trigger1.isTrigger = true;
        trigger1.size = new Vector3(battleRoomSize.x, triggerHeight, battleRoomSize.y);
        trigger1.center = new Vector3(0, triggerHeight / 2f, 0);



        
        GameObject bossRoomObj = CreateRoom("BossRoom", battleRoomSize, new Vector3(60, 0, 0));
        bossRoomObj.AddComponent<BattleRoom>();
        bossRoomObj.GetComponent<BattleRoom>().enemyPrefabs = new GameObject[] { enemiesPrefab[2] };
        bossRoomObj.GetComponent<BattleRoom>().enemyCount=1;
        bossRoomObj.GetComponent<BattleRoom>().lootPrefab = lootPrefab;
        bossRoomObj.GetComponent<BattleRoom>().boundaryMaterial = boundaryMaterial;

        BoxCollider trigger3 = bossRoomObj.AddComponent<BoxCollider>();
        trigger3.isTrigger = true;
        trigger3.size = new Vector3(battleRoomSize.x, triggerHeight, battleRoomSize.y);
        trigger3.center = new Vector3(0, triggerHeight / 2f, 0);




        battleRoomObj.GetComponent<BattleRoom>().SetRoomSize(new Vector3(
                battleRoomSize.x,
                triggerHeight,
                battleRoomSize.y
            ));

        battleRoomObj2.GetComponent<BattleRoom>().SetRoomSize(new Vector3(
                battleRoomSize.x,
                triggerHeight,
                battleRoomSize.y
            ));


    }

    GameObject CreateRoom(string name, Vector2 size, Vector3 position)
    {
        GameObject room = new GameObject(name);
        room.transform.position = position;

 
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        //floor.transform.SetParent(room.transform);
        //floor.transform.localPosition = Vector3.zero;
        //floor.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);

        return room;
    }
}