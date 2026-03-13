using System.Collections.Generic;
using UnityEngine;

public class WeaponPassiveManager : MonoBehaviour
{
    private Dictionary<WeaponPassiveData, PassiveTreeState> states
        = new Dictionary<WeaponPassiveData, PassiveTreeState>();

    private Dictionary<WeaponPassiveData, int> weaponLevels
        = new Dictionary<WeaponPassiveData, int>();

    public int startingPoints = 0;

    private static readonly int[] pointsPerLevel = { 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5 };

    private void Start()
    {
        states.Clear();
        weaponLevels.Clear();
    }

    public PassiveTreeState GetState(WeaponPassiveData data)
    {
        if (!states.ContainsKey(data))
        {
            var newState = new PassiveTreeState();
            newState.availablePoints = startingPoints;
            states[data] = newState;
        }
        return states[data];
    }

    public int GetLevel(WeaponPassiveData data)
    {
        if (!weaponLevels.ContainsKey(data))
            weaponLevels[data] = 1;
        return weaponLevels[data];
    }

    public bool CanLevelUp(WeaponPassiveData data)
    {
        return GetLevel(data) < 15;
    }

    public bool TryLevelUp(WeaponPassiveData data)
    {
        if (!CanLevelUp(data)) return false;
        int newLevel = GetLevel(data) + 1;
        weaponLevels[data] = newLevel;
        int pointsGained = pointsPerLevel[newLevel];
        GetState(data).availablePoints += pointsGained;
        return true;
    }

    public bool TryUnlock(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        return GetState(data).TryUnlock(node, tree);
    }

    public bool TryRefund(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        return GetState(data).TryRefund(node, tree);
    }

    public void ResetPassive(WeaponPassiveData data)
    {
        int level = GetLevel(data);
        int totalPoints = startingPoints;
        for (int i = 2; i <= level; i++)
            totalPoints += pointsPerLevel[i];
        GetState(data).Reset(totalPoints);
    }
}