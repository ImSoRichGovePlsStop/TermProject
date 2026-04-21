using System.Collections.Generic;
using UnityEngine;

public class GenericTreeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handlerContainer;

    public int startingPoints = 0;

    private Dictionary<(object owner, GenericTreeData tree), GenericTreeState> treeStates
        = new Dictionary<(object, GenericTreeData), GenericTreeState>();

    private Dictionary<object, int> ownerPoints
        = new Dictionary<object, int>();

    private List<GenericTreeHandlerBase> currentHandlers = new List<GenericTreeHandlerBase>();
    private object currentOwner;
    private GenericTreeConfig currentConfig;

    public void OnConfigEquipped(GenericTreeConfig config, object owner)
    {
        if (config == currentConfig && owner == currentOwner) return;

        DestroyCurrentHandlers();
        currentConfig = config;
        currentOwner = owner;

        if (config == null || handlerContainer == null) return;

        var stats = handlerContainer.GetComponentInParent<PlayerStats>();
        var context = handlerContainer.GetComponentInParent<PlayerCombatContext>();

        foreach (var tree in config.trees)
        {
            if (tree == null || tree.handlerPrefab == null) continue;

            var handlerObj = Instantiate(tree.handlerPrefab, handlerContainer);
            handlerObj.name = $"Handler_{tree.treeName}";

            var passiveHandler = handlerObj as PassiveHandlerBase;
            if (passiveHandler != null && owner is WeaponPassiveData passiveData && stats != null)
                passiveHandler.Init(tree, passiveData, this as WeaponPassiveManager, stats, context);
            else
                handlerObj.Init(tree, this, owner);

            currentHandlers.Add(handlerObj);
        }
    }

    public void ApplyAllEffects()
    {
        foreach (var handler in currentHandlers)
            handler?.Apply();
    }

    public int GetAvailablePoints(object owner)
    {
        if (!ownerPoints.ContainsKey(owner))
            ownerPoints[owner] = startingPoints;
        return ownerPoints[owner];
    }

    public void SetAvailablePoints(object owner, int points)
    {
        ownerPoints[owner] = points;
    }

    public void AddPoints(object owner, int points)
    {
        ownerPoints[owner] = GetAvailablePoints(owner) + points;
    }

    public GenericTreeState GetState(object owner, GenericTreeData tree)
    {
        var key = (owner, tree);
        if (!treeStates.ContainsKey(key))
            treeStates[key] = new GenericTreeState();
        return treeStates[key];
    }

    public bool TryUnlock(GenericTreeNode node, GenericTreeData tree, object owner)
    {
        int pts = GetAvailablePoints(owner);
        bool result = GetState(owner, tree).TryUnlock(node, ref pts);
        if (result)
        {
            ownerPoints[owner] = pts;
            ApplyAllEffects();
        }
        return result;
    }

    public bool TryRefund(GenericTreeNode node, GenericTreeData tree, object owner)
    {
        int pts = GetAvailablePoints(owner);
        bool result = GetState(owner, tree).TryRefund(node, tree, ref pts);
        if (result)
        {
            ownerPoints[owner] = pts;
            ApplyAllEffects();
        }
        return result;
    }

    public void ResetTree(object owner, GenericTreeData tree, int totalPoints)
    {
        GetState(owner, tree).Reset();
        ownerPoints[owner] = totalPoints;
        ApplyAllEffects();
    }

    private void DestroyCurrentHandlers()
    {
        foreach (var handler in currentHandlers)
        {
            if (handler != null)
            {
                handler.Cleanup();
                Destroy(handler.gameObject);
            }
        }
        currentHandlers.Clear();
    }
}