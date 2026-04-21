using System.Collections.Generic;
using UnityEngine;

public class WeaponLevelManager : MonoBehaviour
{
    public static WeaponLevelManager Instance { get; private set; }

    private static readonly int[] pointsPerLevel = { 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5 };

    private Dictionary<WeaponData, int> weaponLevels = new Dictionary<WeaponData, int>();
    private WeaponPassiveManager        passiveManager;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance       = this;
        passiveManager = FindFirstObjectByType<WeaponPassiveManager>();
    }

    public int GetLevel(WeaponData weapon)
    {
        if (!weaponLevels.ContainsKey(weapon)) weaponLevels[weapon] = 1;
        return weaponLevels[weapon];
    }

    public bool CanLevelUp(WeaponData weapon) => GetLevel(weapon) < 15;

    public bool TryLevelUp(WeaponData weapon)
    {
        if (!CanLevelUp(weapon)) return false;

        int newLevel = GetLevel(weapon) + 1;
        var cost = weapon.GetLevelUpCost(newLevel);
        if (cost?.materials != null && !MaterialStorage.Instance.HasEnoughAll(cost.materials))
            return false;

        if (cost?.materials != null)
            MaterialStorage.Instance.RemoveAll(cost.materials);

        weaponLevels[weapon] = newLevel;

        int points = pointsPerLevel[newLevel];
        passiveManager?.AddPoints(weapon.passiveData, points);

        Vector2Int gridSize = weapon.GetGridSize(newLevel);
        InventoryManager.Instance?.UpgradeWeaponGrid(gridSize.x, gridSize.y);

        return true;
    }

    public void ResetLevel(WeaponData weapon)
    {
        int currentLevel = GetLevel(weapon);
        for (int level = 2; level <= currentLevel; level++)
        {
            var cost = weapon.GetLevelUpCost(level);
            if (cost?.materials != null)
                MaterialStorage.Instance.AddAll(cost.materials);
        }
        weaponLevels[weapon] = 1;
    }

    public int GetTotalPoints(WeaponData weapon)
    {
        int level = GetLevel(weapon);
        int total = passiveManager.startingPoints;
        for (int i = 2; i <= level; i++)
            total += pointsPerLevel[i];
        return total;
    }

    public int GetPointsForLevel(int level)
    {
        if (level < 0 || level >= pointsPerLevel.Length) return 0;
        return pointsPerLevel[level];
    }
}
