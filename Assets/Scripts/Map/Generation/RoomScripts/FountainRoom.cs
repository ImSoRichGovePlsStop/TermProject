using UnityEngine;

public class FountainRoom : MonoBehaviour
{
    public GameObject fountainPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (fountainPrefab == null) return;
        Instantiate(fountainPrefab, spawnPoint.position, spawnPoint.rotation);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            RunManager.Instance?.OnEventRoomEntered();
            FindFirstObjectByType<MinimapManager>()?.OnPlayerEnterRoom(node);
        }
    }
}
