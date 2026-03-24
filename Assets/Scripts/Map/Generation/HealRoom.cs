using UnityEngine;

public class HealRoom : MonoBehaviour
{
    public GameObject healStationPrefab;

    public void Init(Transform spawnPoint)
    {
        if (healStationPrefab == null) return;
        Instantiate(healStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
