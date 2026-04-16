using UnityEngine;

public class RareLootRoom : MonoBehaviour
{
    public GameObject lootPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (lootPrefab == null) return;

        Instantiate(lootPrefab, spawnPoint.position, spawnPoint.rotation);
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