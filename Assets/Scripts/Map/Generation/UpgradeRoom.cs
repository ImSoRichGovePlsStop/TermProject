using UnityEngine;

public class UpgradeRoom : MonoBehaviour
{
    public GameObject upgradeStationPrefab;

    public void Init(Transform spawnPoint)
    {
        if (upgradeStationPrefab == null) return;
        Instantiate(upgradeStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
