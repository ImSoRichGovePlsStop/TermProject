using UnityEngine;

public class RareLootRoom : MonoBehaviour
{
    public GameObject lootPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (lootPrefab == null) return;

        var lootObj = Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation);
        var randomLoot = lootObj.GetComponent<RandomLoot>();
        if (randomLoot != null)
            randomLoot.Configure(BuildRareLootConfig());
    }

    private LootConfig BuildRareLootConfig()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        int roomsCleared = RunManager.Instance?.TotalRoomsCleared ?? 0;
        float baseMean = 50f + floor * 30f + roomsCleared * 5f + 10f;
        return new LootConfig
        {
            optionCount = 1,
            meanCost = baseMean * 2.5f,
            sd = (20f + (floor - 1) * 3f) * 0.3f,
            allowDuplicates = false,
        };
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunManager.Instance?.OnEventRoomEntered();
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        }
    }
}