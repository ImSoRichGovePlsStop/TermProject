using UnityEngine;

public class FoundationPassiveHandler : PassiveHandlerBase
{
    private FoundationPassive foundation;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        foundation = gameObject.AddComponent<FoundationPassive>();
        foundation.Init(stats, context);
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (foundation == null) return;
        foundation.infectiousStrike = IsUnlocked(1, 0);
        foundation.swiftMinions = IsUnlocked(2, 0);
        foundation.recycledEssence = IsUnlocked(3, 0);
        foundation.sharedEssence = IsUnlocked(3, 1);
        foundation.rapidSpawn = IsUnlocked(4, 0);
    }

    public override void Cleanup()
    {
        if (foundation != null)
            foundation.enabled = false;
    }
}