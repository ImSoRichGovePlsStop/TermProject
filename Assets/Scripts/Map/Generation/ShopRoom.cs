using UnityEngine;

public class ShopRoom : MonoBehaviour
{
    public GameObject shopStationPrefab;

    public void Init(Transform spawnPoint)
    {
        if (shopStationPrefab == null) return;
        Instantiate(shopStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
