using UnityEngine;


public class SpawnRoom : MonoBehaviour
{
    [Header("Player")]
    public GameObject playerPrefab;

    public float spawnHeightOffset = 0.3f;

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {

        Vector3 spawnPoint = GetSpawnPoint();
        Instantiate(playerPrefab, spawnPoint, Quaternion.identity);
    }


    public Vector3 GetSpawnPoint()
    {
        return transform.position + Vector3.up * spawnHeightOffset;
    }
}
