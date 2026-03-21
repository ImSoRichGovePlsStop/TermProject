using UnityEngine;

public class FrenzyPassiveHandler : PassiveHandlerBase
{
    private StackFrenzyPassive stackFrenzy;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
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

        bool newFrenzyRush = IsUnlocked(4, 0);
        if (!newFrenzyRush && stackFrenzy.frenzyRush)
            stackFrenzy.RemoveFrenzyRush();
        stackFrenzy.frenzyRush = newFrenzyRush;

        bool newGlassCannon = IsUnlocked(5, 1);
        if (!newGlassCannon && stackFrenzy.glassCannon)
            stackFrenzy.RemoveGlassCannon();
        stackFrenzy.glassCannon = newGlassCannon;

        stackFrenzy.thirdHitTripleStack = IsUnlocked(3, 2);
        stackFrenzy.resilientFury = IsUnlocked(5, 2);
        stackFrenzy.apexPredator = IsUnlocked(6, 0);
    }
}