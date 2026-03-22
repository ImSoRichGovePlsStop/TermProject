using UnityEngine;

public class WeaponPassiveManager : GenericTreeManager
{
    private WeaponPassiveData currentPassiveData;

    public void OnWeaponEquipped(WeaponPassiveData passiveData)
    {
        currentPassiveData = passiveData;
        OnConfigEquipped(passiveData, passiveData);
    }

    public bool TryUnlock(PassiveNode node, GenericTreeData tree, WeaponPassiveData data)
    {
        return TryUnlock(node, tree, (object)data);
    }

    public bool TryRefund(PassiveNode node, GenericTreeData tree, WeaponPassiveData data)
    {
        return TryRefund(node, tree, (object)data);
    }

    public void ResetPassive(WeaponPassiveData data, int totalPoints)
    {
        if (data.trees == null) return;
        foreach (var tree in data.trees)
            GetState(data, tree).Reset();
        SetAvailablePoints(data, totalPoints);
        ApplyAllEffects();
    }

    public void AddPoints(WeaponPassiveData data, int points)
    {
        AddPoints((object)data, points);
    }
}