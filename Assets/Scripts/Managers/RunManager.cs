using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Run Stats")]
    public int CurrentFloor = 1;
    public int TotalEnemyKilled = 0;
    public int TotalEventsFound = 0;
    public int TotalBossKilled = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnEnemyKilled()
    {
        TotalEnemyKilled++;
    }

    public void OnEventRoomEntered()
    {
        TotalEventsFound++;
    }

    public void OnBossKilled()
    {
        TotalBossKilled++;
        CurrentFloor++;
        Debug.Log($"[RunManager] Boss killed! Entering floor {CurrentFloor}");
    }

    public void ResetRun()
    {
        CurrentFloor = 1;
        TotalEnemyKilled = 0;
        TotalEventsFound = 0;
        TotalBossKilled = 0;
    }
}