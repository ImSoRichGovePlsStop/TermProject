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
        {
            Instantiate(gameManagerPrefab);
        }

      
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