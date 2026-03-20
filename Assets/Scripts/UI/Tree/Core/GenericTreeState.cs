using System.Collections.Generic;

public class GenericTreeState
{
    private HashSet<GenericTreeNode> unlockedNodes = new HashSet<GenericTreeNode>();

    public bool IsUnlocked(GenericTreeNode node)
    {
        return unlockedNodes.Contains(node);
    }

    public bool CanUnlock(GenericTreeNode node, int availablePoints)
    {
        if (IsUnlocked(node)) return false;
        if (availablePoints < node.cost) return false;
        if (node.parents == null || node.parents.Length == 0) return true;

        if (node.requireAllParents)
        {
            foreach (var parent in node.parents)
            {
                if (parent == null) continue;
                if (!IsUnlocked(parent)) return false;
            }
            return true;
        }
        else
        {
            foreach (var parent in node.parents)
            {
                if (parent == null) continue;
                if (IsUnlocked(parent)) return true;
            }
            return false;
        }
    }

    public bool TryUnlock(GenericTreeNode node, ref int availablePoints)
    {
        if (!CanUnlock(node, availablePoints)) return false;
        availablePoints -= node.cost;
        unlockedNodes.Add(node);
        return true;
    }

    public bool CanRefund(GenericTreeNode node, GenericTreeData tree)
    {
        if (!IsUnlocked(node)) return false;

        foreach (var other in tree.nodes)
        {
            if (!IsUnlocked(other)) continue;
            if (!HasParent(other, node)) continue;

            if (other.requireAllParents)
            {
                return false;
            }
            else
            {
                bool hasOtherUnlockedParent = false;
                foreach (var p in other.parents)
                {
                    if (p == node) continue;
                    if (p != null && IsUnlocked(p))
                    {
                        hasOtherUnlockedParent = true;
                        break;
                    }
                }
                if (!hasOtherUnlockedParent) return false;
            }
        }

        return true;
    }

    public bool TryRefund(GenericTreeNode node, GenericTreeData tree, ref int availablePoints)
    {
        if (!CanRefund(node, tree)) return false;
        unlockedNodes.Remove(node);
        availablePoints += node.cost;
        return true;
    }

    public void Reset()
    {
        unlockedNodes.Clear();
    }

    private bool HasParent(GenericTreeNode node, GenericTreeNode target)
    {
        if (node.parents == null) return false;
        foreach (var p in node.parents)
            if (p == target) return true;
        return false;
    }
}