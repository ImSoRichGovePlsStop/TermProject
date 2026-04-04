using UnityEngine;

public class ConductionPassiveHandler : PassiveHandlerBase
{
    [Header("Prefabs")]
    public GameObject zapperPrefab;

    [Header("Unstable Conductor")]
    [SerializeField] private float eliteZapperChance = 0.15f;

    private FoundationPassive foundation;
    private ConductionPassive conduction;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        foundation = transform.parent.GetComponentInChildren<FoundationPassive>();

        conduction = gameObject.AddComponent<ConductionPassive>();
        conduction.zapperPrefab = zapperPrefab;
        conduction.eliteZapperChance = eliteZapperChance;
        conduction.Init(stats, context);

        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (foundation != null)
            foundation.canSpawnZapper = IsUnlocked(1, 0);

        if (conduction != null)
        {
            conduction.stableCharge = IsUnlocked(2, 0);
            conduction.residualCurrent = IsUnlocked(3, 1);
            conduction.highVoltage = IsUnlocked(3, 2);
            conduction.lightningRod = IsUnlocked(4, 0);
            conduction.arcPulse = IsUnlocked(5, 1);
            conduction.cardiacArrest = IsUnlocked(5, 2);
            conduction.unstableConductor = IsUnlocked(6, 0);
        }
    }

    public override void Cleanup()
    {
        if (conduction != null)
            conduction.enabled = false;
    }
}