using UnityEngine;

public class AegisPassiveHandler : PassiveHandlerBase
{
    private CriticalShieldPassive aegis;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        aegis = gameObject.AddComponent<CriticalShieldPassive>();
        aegis.Init(stats, context);
        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (aegis == null) return;

        bool wasEnabled = aegis.enabled;
        bool nowEnabled = IsUnlocked(1, 0);

        if (wasEnabled && !nowEnabled)
            aegis.ForceClean();

        aegis.enabled = nowEnabled;

        aegis.keenEdge = IsUnlocked(2, 0);
        aegis.persistence = IsUnlocked(3, 1);
        aegis.fortifiedStrike = IsUnlocked(3, 2);
        aegis.fortify = IsUnlocked(4, 0);
        aegis.shatterBurst = IsUnlocked(5, 1);
        aegis.absorb = IsUnlocked(5, 2);
        aegis.temperedSoul = IsUnlocked(6, 0);

        aegis.OnFlagsChanged();
    }
}