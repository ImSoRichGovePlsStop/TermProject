using System.Collections.Generic;
using UnityEngine;

public class BuildingLevelManager : MonoBehaviour
{
    public static BuildingLevelManager Instance { get; private set; }

    private Dictionary<BuildingData, int> buildingLevels = new Dictionary<BuildingData, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public int GetLevel(BuildingData building)
    {
        if (!buildingLevels.ContainsKey(building))
            buildingLevels[building] = 1;
        return buildingLevels[building];
    }

    public bool CanLevelUp(BuildingData building)
    {
        return GetLevel(building) < building.maxLevel;
    }

    public bool TryLevelUp(BuildingData building)
    {
        if (!CanLevelUp(building)) return false;

        int newLevel = GetLevel(building) + 1;
        buildingLevels[building] = newLevel;

        if (building.hasPoints)
        {
            int points = building.GetPointsForLevel(newLevel);
            GamblerManager.Instance?.AddPoints(building, points);
        }

        return true;
    }

    public int GetTotalPoints(BuildingData building)
    {
        return building.GetTotalPoints(GetLevel(building));
    }

    public int GetPointsForLevel(BuildingData building, int level)
    {
        return building.GetPointsForLevel(level);
    }
}
