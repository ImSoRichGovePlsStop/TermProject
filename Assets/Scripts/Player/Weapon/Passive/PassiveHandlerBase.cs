using UnityEngine;

public abstract class PassiveHandlerBase : MonoBehaviour
{
    protected PassiveTree tree;
    protected WeaponPassiveData passiveData;
    protected WeaponPassiveManager manager;
    protected PlayerStats stats;
    protected PlayerCombatContext context;

    public virtual void Init(PassiveTree tree, WeaponPassiveData data,
                              WeaponPassiveManager manager,
                              PlayerStats stats, PlayerCombatContext context)
    {
        this.tree = tree;
        this.passiveData = data;
        this.manager = manager;
        this.stats = stats;
        this.context = context;
        Apply();
    }

    public abstract void Apply();

    protected bool IsUnlocked(int layer, int branch)
    {
        var state = manager.GetState(passiveData);
        foreach (var node in tree.nodes)
            if (node.layer == layer && node.branch == branch)
                return state.IsUnlocked(node);
        return false;
    }
}