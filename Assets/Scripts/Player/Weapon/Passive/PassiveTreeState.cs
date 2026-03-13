using System.Collections.Generic;
using UnityEngine;

public class PassiveTreeState
{
    public int availablePoints;
    private HashSet<PassiveNode> unlockedNodes = new HashSet<PassiveNode>();

    public bool IsUnlocked(PassiveNode node)
    {
        return unlockedNodes.Contains(node);
    }

    public bool IsTreeUnlocked(PassiveTree tree)
    {
        foreach (var node in tree.nodes)
        {
            if (node.layer == 1 && IsUnlocked(node))
                return true;
        }
        return false;
    }

    public bool CanUnlock(PassiveNode node, PassiveTree tree)
    {
        if (IsUnlocked(node)) return false;
        if (availablePoints < node.Cost) return false;
        if (node.layer == 1) return true;

        foreach (var n in tree.nodes)
        {
            if (n.layer == node.layer - 1 && IsUnlocked(n))
                return true;
        }
        return false;
    }

    public bool TryUnlock(PassiveNode node, PassiveTree tree)
    {
        if (!CanUnlock(node, tree)) return false;
        availablePoints -= node.Cost;
        unlockedNodes.Add(node);
        return true;
    }

    public void Reset(WeaponPassiveData data)
    {
        foreach (var node in unlockedNodes)
            availablePoints += node.Cost;
        unlockedNodes.Clear();
    }
}