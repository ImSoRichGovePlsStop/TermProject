using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Healing")]
    [Tooltip("% after clearing")]
    [Range(0f, 1f)]
    public float HealPerRoom = 0.10f;

    [Header("Reroll")]
    public bool AllowReroll = true;

    [Header("Run Stats")]
    public int CurrentFloor = 1;
    public int TotalEnemyKilled = 0;
    public int TotalEventsFound = 0;
    public int TotalBossKilled = 0;
    public bool IsWin = false;

    [Header("Floor Event Tracking")]

    public List<RoomType> PreviousFloorEvents = new();
    
    public List<RoomType> CurrentFloorEvents = new();

    [Header("Current Run Tracking")]

    public int TotalCoinsCollected = 0;
 
    public float TotalDamageTaken = 0f;

    public int TotalHeals = 0;
 
    public int TotalItemsCollected = 0;
 
    public int TotalRoomsCleared = 0;
   
    public int HighestFloorReached = 1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

 
    public void RegisterEventRoomPlaced(RoomType type)
    {
        if (!CurrentFloorEvents.Contains(type))
            CurrentFloorEvents.Add(type);
    }

  
    void RotateFloorEvents()
    {
        PreviousFloorEvents = new List<RoomType>(CurrentFloorEvents);
        CurrentFloorEvents.Clear();
    }


    public bool WasMissingLastFloor(RoomType type)
    {
        return !PreviousFloorEvents.Contains(type);
    }

    

    public void OnEnemyKilled()
    {
        TotalEnemyKilled++;
    }

    public void OnEventRoomEntered()
    {
        TotalEventsFound++;
    }

    public void OnRoomCleared()
    {
        TotalRoomsCleared++;
    }

    public void OnBossKilled()
    {
        TotalBossKilled++;
        RotateFloorEvents();
        CurrentFloor++;
        HighestFloorReached = Mathf.Max(HighestFloorReached, CurrentFloor);
    }

    public void OnCoinsCollected(int amount)
    {
        TotalCoinsCollected += amount;
    }

    public void OnDamageTaken(float amount)
    {
        TotalDamageTaken += amount;
    }

    public void OnHealed()
    {
        TotalHeals++;
    }

    public void OnItemCollected()
    {
        TotalItemsCollected++;
    }

    public void ResetRun()
    {
        CurrentFloor = 1;
        TotalEnemyKilled = 0;
        TotalEventsFound = 0;
        TotalBossKilled = 0;
        TotalCoinsCollected = 0;
        TotalDamageTaken = 0f;
        TotalHeals = 0;
        TotalItemsCollected = 0;
        TotalRoomsCleared = 0;
        HighestFloorReached = 1;
        IsWin = false;
        PreviousFloorEvents.Clear();
        CurrentFloorEvents.Clear();
    }
}