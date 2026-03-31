using UnityEngine;

public class UpgradeRoom : MonoBehaviour
{
    public GameObject upgradeStationPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (upgradeStationPrefab == null) return;
        Instantiate(upgradeStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
    }
}
