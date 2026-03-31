using UnityEngine;

public class EliteHarpyHealthBase : EnemyHealthBase
{
    [Header("Air Phase")]
    [SerializeField] private float airPhaseHPThreshold = 0.5f;
    [SerializeField] private EliteHarpyController eliteController;

    private bool airPhaseTriggered = false;

    protected override void Awake()
    {
        base.Awake();
        if (eliteController == null)
            eliteController = GetComponent<EliteHarpyController>();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        if (!airPhaseTriggered && CurrentHP / MaxHP <= airPhaseHPThreshold)
        {
            airPhaseTriggered = true;
            eliteController?.UnlockAirPhase();
        }
    }
}