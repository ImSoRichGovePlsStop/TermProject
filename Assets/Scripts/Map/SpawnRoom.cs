using UnityEngine;


public class SpawnRoom : MonoBehaviour
{
    public float spawnHeightOffset = 0.5f;

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {

        Vector3 spawnPoint = GetSpawnPoint();

        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogWarning("[PlayerSpawner] Player not found!"); return; }
        player.transform.position = spawnPoint;
    }


    public Vector3 GetSpawnPoint()
    {
        return transform.position + Vector3.up * spawnHeightOffset;
    }
}
