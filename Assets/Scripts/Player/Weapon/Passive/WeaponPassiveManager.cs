using System.Collections.Generic;
using UnityEngine;

public class WeaponPassiveManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform passiveHandlerContainer;

    private Dictionary<WeaponPassiveData, PassiveTreeState> states
        = new Dictionary<WeaponPassiveData, PassiveTreeState>();

    private List<PassiveHandlerBase> currentHandlers = new List<PassiveHandlerBase>();
    private WeaponPassiveData currentPassiveData;

    public int startingPoints = 0;

    private void Start()
    {
        states.Clear();
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

    public void AddPoints(WeaponPassiveData data, int points)
    {
        GetState(data).availablePoints += points;
    }

    public void ResetPassive(WeaponPassiveData data, int totalPoints)
    {
        GetState(data).Reset(totalPoints);
        ApplyPassiveEffects();
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
}