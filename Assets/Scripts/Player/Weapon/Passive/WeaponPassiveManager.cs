using System.Collections.Generic;
using UnityEngine;

public class WeaponPassiveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform passiveHandlerContainer;

    private Dictionary<WeaponPassiveData, PassiveTreeState> states
        = new Dictionary<WeaponPassiveData, PassiveTreeState>();
    private Dictionary<WeaponPassiveData, int> weaponLevels
        = new Dictionary<WeaponPassiveData, int>();

    private List<PassiveHandlerBase> currentHandlers = new List<PassiveHandlerBase>();
    private WeaponPassiveData currentPassiveData;

    public int startingPoints = 0;
    private static readonly int[] pointsPerLevel = { 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5 };

    private void Start()
    {
        states.Clear();
        weaponLevels.Clear();
    }

    public void OnWeaponEquipped(WeaponPassiveData passiveData)
    {
        if (passiveData == currentPassiveData) return;

        DestroyCurrentHandlers();
        currentPassiveData = passiveData;

        if (passiveData == null || passiveHandlerContainer == null) return;

        var stats = passiveHandlerContainer.GetComponentInParent<PlayerStats>();
        var context = passiveHandlerContainer.GetComponentInParent<PlayerCombatContext>();

        foreach (var tree in passiveData.trees)
        {
            if (tree == null || tree.handlerPrefab == null) continue;

            var handlerObj = Instantiate(tree.handlerPrefab, passiveHandlerContainer);
            handlerObj.name = $"Handler_{tree.treeName}";
            handlerObj.Init(tree, passiveData, this, stats, context);
            currentHandlers.Add(handlerObj);
        }
    }

    public void ApplyPassiveEffects()
    {
        foreach (var handler in currentHandlers)
            handler?.Apply();
    }

    private void DestroyCurrentHandlers()
    {
        foreach (var handler in currentHandlers)
            if (handler != null) Destroy(handler.gameObject);
        currentHandlers.Clear();
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

    public bool CanLevelUp(WeaponPassiveData data) => GetLevel(data) < 15;

    public bool TryLevelUp(WeaponPassiveData data)
    {
        if (!CanLevelUp(data)) return false;
        int newLevel = GetLevel(data) + 1;
        weaponLevels[data] = newLevel;
        GetState(data).availablePoints += pointsPerLevel[newLevel];
        return true;
    }

    public bool TryUnlock(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        bool result = GetState(data).TryUnlock(node, tree);
        if (result) ApplyPassiveEffects();
        return result;
    }

    public bool TryRefund(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        bool result = GetState(data).TryRefund(node, tree);
        if (result) ApplyPassiveEffects();
        return result;
    }

    public void ResetPassive(WeaponPassiveData data)
    {
        int level = GetLevel(data);
        int totalPoints = startingPoints;
        for (int i = 2; i <= level; i++)
            totalPoints += pointsPerLevel[i];
        GetState(data).Reset(totalPoints);
        ApplyPassiveEffects();
    }
}