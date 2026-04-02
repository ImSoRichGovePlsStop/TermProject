using UnityEngine;

public class SpawnRoom : MonoBehaviour
{
    public float spawnHeightOffset = 0.3f;

    [HideInInspector] public RoomNode node;

    void Start()
    {
        SpawnPlayer();
        // Reveal spawn room on minimap immediately
        FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
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
