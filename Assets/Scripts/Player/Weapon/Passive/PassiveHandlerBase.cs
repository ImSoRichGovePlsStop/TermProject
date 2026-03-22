using UnityEngine;

public abstract class PassiveHandlerBase : GenericTreeHandlerBase
{
    protected WeaponPassiveData passiveData;
    protected PlayerStats stats;
    protected PlayerCombatContext context;

    public virtual void Init(GenericTreeData tree, WeaponPassiveData data,
                             WeaponPassiveManager manager,
                             PlayerStats stats, PlayerCombatContext context)
    {
        this.passiveData = data;
        this.stats = stats;
        this.context = context;
        base.Init(tree, manager, data);
    }

    protected bool IsUnlocked(int layer, int branch)
    {
        var state = manager.GetState(treeOwner, tree);
        foreach (var node in tree.nodes)
        {
            var passiveNode = node as PassiveNode;
            if (passiveNode != null && passiveNode.layer == layer && passiveNode.branch == branch)
                return state.IsUnlocked(node);
        }
        return false;
    }
}