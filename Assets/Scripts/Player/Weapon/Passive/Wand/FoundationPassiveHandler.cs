using UnityEngine;

public class FoundationPassiveHandler : PassiveHandlerBase
{
    [Header("Prefabs")]
    public GameObject brawlerPrefab;
    public GameObject zapperPrefab;
    public GameObject bomberPrefab;

    private FoundationPassive foundation;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        foundation = gameObject.AddComponent<FoundationPassive>();
        foundation.brawlerPrefab = brawlerPrefab;
        foundation.zapperPrefab = zapperPrefab;
        foundation.bomberPrefab = bomberPrefab;
        foundation.Init(stats, context);
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (foundation == null) return;
        foundation.infectiousStrike = IsUnlocked(1, 0);
        foundation.swiftMinions = IsUnlocked(2, 0);
        foundation.recycledEssence = IsUnlocked(3, 1);
        foundation.sharedEssence = IsUnlocked(3, 2);
        foundation.rapidSpawn = IsUnlocked(4, 0);
        foundation.manaFeedback = IsUnlocked(5, 1);
        foundation.greatConjunction = IsUnlocked(5, 2);
        foundation.warlord = IsUnlocked(6, 0);
        foundation.ApplyTotemConfig();
    }

    public override void Cleanup()
    {
        if (foundation != null)
            foundation.enabled = false;
    }
}