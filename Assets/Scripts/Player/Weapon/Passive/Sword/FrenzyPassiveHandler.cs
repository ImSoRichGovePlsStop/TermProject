using UnityEngine;

public class FrenzyPassiveHandler : PassiveHandlerBase
{
    [Header("UI Display")]
    public Sprite iconStack;
    public Sprite iconApex;

    private StackFrenzyPassive stackFrenzy;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        stackFrenzy = gameObject.AddComponent<StackFrenzyPassive>();
        stackFrenzy.Init(stats, context);

        stackFrenzy.iconStack = iconStack;
        stackFrenzy.iconApex = iconApex;

        stackFrenzy.RegisterHUD();
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (stackFrenzy == null) return;

        bool wasEnabled = stackFrenzy.enabled;
        bool nowEnabled = IsUnlocked(1, 0);

        if (wasEnabled && !nowEnabled)
        {
            stackFrenzy.ForceClean();
            PlayerStatusHUD.Instance.Unregister("frenzy");
        }

        if (!wasEnabled && nowEnabled)
            stackFrenzy.RegisterHUD();

        stackFrenzy.enabled = nowEnabled;
        stackFrenzy.maxStacks = IsUnlocked(2, 0) ? 15 : 10;
        if (stackFrenzy.CurrentStacks > stackFrenzy.maxStacks)
            stackFrenzy.ClampStacks();

        stackFrenzy.bonusPerStack = IsUnlocked(3, 1) ? 0.3f : 0.2f;
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

    public override void Cleanup()
    {
        stackFrenzy?.ForceClean();
        PlayerStatusHUD.Instance?.Unregister("frenzy");
    }
}