using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    [Header("Player")]
    public GameObject playerPrefab;

    [Header("Room Sizes")]
    public Vector2 spawnRoomSize = new Vector2(10f, 10f);
    public Vector2 battleRoomSize = new Vector2(12f, 12f);

    [Header("Spacing")]
    public float roomSpacing = 4f;

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
        GameObject battleRoom = CreateRoom("BattleRoom", battleRoomSize, new Vector3(battleRoomX, 0, 0));
        battleRoom.AddComponent<BattleRoom>();
    }

    GameObject CreateRoom(string name, Vector2 size, Vector3 position)
    {
        
        GameObject room = GameObject.CreatePrimitive(PrimitiveType.Plane);
        room.name = name;
        room.transform.position = position;
        room.transform.localScale = new Vector3(size.x / 10f, 1f, size.y / 10f);

        Renderer rend = room.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = name == "SpawnRoom" ? new Color(0.2f, 0.8f, 0.2f, 1f)   
                                        : new Color(0.8f, 0.2f, 0.2f, 1f);  
        rend.material = mat;

        return room;
    }
}