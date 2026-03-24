using UnityEngine;

public class MergeRoom : MonoBehaviour
{
    public GameObject mergeStationPrefab;

    public void Init(Transform spawnPoint)
    {
        if (mergeStationPrefab == null) return;
        Instantiate(mergeStationPrefab, spawnPoint.position, spawnPoint.rotation);
    }
}
