using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private GameObject gameManagerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition = new Vector3(0f, 0.25f, 0f);

    void Awake()
    {
        GameManager existingManager = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);

        if (existingManager == null)
            Instantiate(gameManagerPrefab);

        // Restore saved data (materials, weapon levels, passives, station levels)
        // into the managers. Must run after GameManager is confirmed present so all
        // child managers have completed their own Awake().
        SaveManager.Instance?.Apply();

        // Re-apply station passive effects (HP bonus, revive hook, luck modifiers)
        // using the now-restored levels so they're active for the coming run.
        HealthStationManager.Instance?.ResetRun();
        LuckStationManager.Instance?.ResetRun();

        MovePlayerToStart();
    }

    private void MovePlayerToStart()
    {
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            player.transform.position = playerSpawnPosition;
        }

    }
}