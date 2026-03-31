using UnityEngine;

public class HealRoom : MonoBehaviour
{
    public GameObject healStationPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (healStationPrefab == null) return;
        Instantiate(healStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
    }
}
