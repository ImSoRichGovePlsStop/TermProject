using UnityEngine;

public class ShopRoom : MonoBehaviour
{
    public GameObject shopStationPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (shopStationPrefab == null) return;
        Instantiate(shopStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
    }
}
