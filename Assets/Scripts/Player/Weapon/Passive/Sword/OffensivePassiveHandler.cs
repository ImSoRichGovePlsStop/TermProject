using UnityEngine;

public class OffensivePassiveHandler : PassiveHandlerBase
{
    private StackFrenzyPassive stackFrenzy;

    public override void Init(PassiveTree tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        stackFrenzy = gameObject.AddComponent<StackFrenzyPassive>();
        stackFrenzy.Init(stats, context);
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (stackFrenzy == null) return;

        bool wasEnabled = stackFrenzy.enabled;
        bool nowEnabled = IsUnlocked(1, 0);

        if (wasEnabled && !nowEnabled)
            stackFrenzy.ForceClean();

        stackFrenzy.enabled = nowEnabled;

        stackFrenzy.maxStacks = IsUnlocked(2, 0) ? 15 : 10;
        if (stackFrenzy.CurrentStacks > stackFrenzy.maxStacks)
            stackFrenzy.ClampStacks();

        stackFrenzy.bonusPerStack = IsUnlocked(3, 1) ? 0.03f : 0.02f;
        stackFrenzy.SetStacks(stackFrenzy.CurrentStacks);

        stackFrenzy.thirdHitTripleStack = IsUnlocked(3, 2);

        stackFrenzy.frenzyRush = IsUnlocked(4, 0);

        stackFrenzy.glassCannon = IsUnlocked(5, 1);

        stackFrenzy.resilientFury = IsUnlocked(5, 2);

        stackFrenzy.apexPredator = IsUnlocked(6, 0);
    }
}