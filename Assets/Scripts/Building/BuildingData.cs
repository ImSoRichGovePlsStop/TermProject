using UnityEngine;

[CreateAssetMenu(fileName = "BuildingData", menuName = "Building/BuildingData")]
public class BuildingData : ScriptableObject
{
    [Header("Info")]
    public string buildingName;

    [Header("Level")]
    public int maxLevel = 10;

    [Header("Points (optional)")]
    public bool hasPoints = false;
    public int[] pointsPerLevel;

    public int GetPointsForLevel(int level)
    {
        if (!hasPoints) return 0;
        if (level < 0 || level >= pointsPerLevel.Length) return 0;
        return pointsPerLevel[level];
    }

    public int GetTotalPoints(int level)
    {
        if (!hasPoints) return 0;
        int total = 0;
        for (int i = 1; i <= level && i < pointsPerLevel.Length; i++)
            total += pointsPerLevel[i];
        return total;
    }
}