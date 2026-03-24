using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;

public class BossRoom : MonoBehaviour
{
    [Header("State")]
    public bool isLocked  = false;
    public bool isCleared = false;

    [Header("Room")]
    private Vector3          roomSize;
    private List<GameObject> invisibleWalls = new List<GameObject>();

    [Header("Boss")]
    public GameObject bossPrefab;
    public GameObject lootPrefab;
    public GameObject portalPrefab;
    public GameObject portalFinalPrefab;

    public Material boundaryMaterial;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    void Update()
    {
        if (isLocked && !isCleared)
        {
            spawnedEnemies.RemoveAll(e => e == null);
            if (spawnedEnemies.Count == 0)
            {
                UIManager _uiManager = FindFirstObjectByType<UIManager>();
                _uiManager.isInBattle = false;
                ClearRoom();
            }
        }
    }

    public void SetRoomSize(Vector3 size)
    {
        roomSize = size;
    }

    public void OnPlayerEnter()
    {
        if (!isCleared)
        {
            LockRoom();
            SpawnBoss();
            UIManager _uiManager = FindFirstObjectByType<UIManager>();
            _uiManager.isInBattle = true;
            _uiManager.CloseShop();
            if (_uiManager.IsInventoryOpen)
                _uiManager.ToggleInventory();
        }
    }

    private void LockRoom()
    {
        isLocked = true;
        CreateInvisibleWalls();
    }

    private void ClearRoom()
    {
        isCleared = true;
        isLocked  = false;
        RemoveInvisibleWalls();

        // loot slightly south
        Vector3 lootPos = transform.position + new Vector3(0f, 0f, -2f);
        Instantiate(lootPrefab, lootPos, Quaternion.identity);

        // pick portal based on next floor
        int nextFloor = (RunManager.Instance?.CurrentFloor ?? 1) + 1;
        GameObject selectedPortal = nextFloor > 3 && portalFinalPrefab != null
            ? portalFinalPrefab
            : portalPrefab;

        Vector3 portalPos = transform.position + new Vector3(0f, 0f, 2f);
        Instantiate(selectedPortal, portalPos, Quaternion.identity);

        CurrencyManager wallet = Object.FindFirstObjectByType<CurrencyManager>();
        wallet.AddCoins(Random.Range(100, 501));

        RunManager.Instance?.OnBossKilled();
    }

    private void SpawnBoss()
    {
        if (bossPrefab == null) { Debug.LogWarning("[BossRoom] Boss prefab missing!"); return; }
        var boss = Instantiate(bossPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        spawnedEnemies.Add(boss);
    }

    private void CreateInvisibleWalls()
    {
        CreateRoomBoundary();

        (Vector3 pos, Vector3 size)[] wallConfigs = new[]
        {
            (new Vector3(-roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.01f, roomSize.y, roomSize.z)),
            (new Vector3( roomSize.x / 2f, roomSize.y / 2f, 0f), new Vector3(0.01f, roomSize.y, roomSize.z)),
            (new Vector3(0f, roomSize.y / 2f,  roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, 0.01f)),
            (new Vector3(0f, roomSize.y / 2f, -roomSize.z / 2f), new Vector3(roomSize.x, roomSize.y, 0.01f)),
        };

        foreach (var (localPos, size) in wallConfigs)
        {
            var wall = new GameObject("InvisibleWall");
            wall.transform.SetParent(transform);
            wall.transform.localPosition = localPos;
            wall.AddComponent<BoxCollider>().size = size;
            invisibleWalls.Add(wall);
        }
    }

    private void CreateRoomBoundary()
    {
        ProBuilderMesh pbMesh = ProBuilderMesh.Create();
        PolyShape polyShape   = pbMesh.gameObject.AddComponent<PolyShape>();

        float halfX = (roomSize.x / 2f) + 0.01f;
        float halfZ = (roomSize.z / 2f) + 0.01f;

        polyShape.SetControlPoints(new Vector3[]
        {
            new Vector3(-halfX, 0, -halfZ),
            new Vector3(-halfX, 0,  halfZ),
            new Vector3( halfX, 0,  halfZ),
            new Vector3( halfX, 0, -halfZ),
        });

        polyShape.extrude     = 10;
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
        foreach (var wall in invisibleWalls)
            if (wall != null) Destroy(wall);
        invisibleWalls.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) OnPlayerEnter();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.up * (roomSize.y / 2f), roomSize);
    }
}
