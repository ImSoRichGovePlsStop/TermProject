using UnityEngine;

public class DetonationPassiveHandler : PassiveHandlerBase
{
    private FoundationPassive foundation;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        foundation = transform.parent.GetComponentInChildren<FoundationPassive>();
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (foundation == null) return;
        foundation.canSpawnBomber = IsUnlocked(1, 0);
    }

    public override void Cleanup() { }
}