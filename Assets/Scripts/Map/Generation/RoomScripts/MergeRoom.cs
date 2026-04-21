using UnityEngine;

public class MergeRoom : MonoBehaviour
{
    public GameObject mergeStationPrefab;

    [HideInInspector] public RoomNode node;

    public void Init(Transform spawnPoint)
    {
        if (mergeStationPrefab == null) return;
        Instantiate(mergeStationPrefab, spawnPoint.position, spawnPoint.rotation);
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
