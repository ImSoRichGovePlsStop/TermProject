using UnityEngine;

public class DetonationPassiveHandler : PassiveHandlerBase
{
    [Header("Prefabs")]
    public GameObject bomberPrefab;

    [Header("Singularity")]
    [SerializeField] private float eliteBomberChance = 0.15f;

    private FoundationPassive foundation;
    private DetonationPassive detonation;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        foundation = transform.parent.GetComponentInChildren<FoundationPassive>();

        detonation = gameObject.AddComponent<DetonationPassive>();
        detonation.bomberPrefab = bomberPrefab;
        detonation.eliteBomberChance = eliteBomberChance;
        detonation.Init(stats, context);

        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (foundation != null)
            foundation.canSpawnBomber = IsUnlocked(1, 0);

        if (detonation != null)
        {
            detonation.volatileBody = IsUnlocked(2, 0);
            detonation.overcharge = IsUnlocked(3, 1);
            detonation.wideBlast = IsUnlocked(3, 2);
            detonation.unstableCharge = IsUnlocked(4, 0);
            detonation.scorchedEarth = IsUnlocked(5, 1);
            detonation.shrapnel = IsUnlocked(5, 2);
            detonation.singularity = IsUnlocked(6, 0);
        }
    }

    public override void Cleanup()
    {
        if (detonation != null)
            detonation.enabled = false;
    }
}