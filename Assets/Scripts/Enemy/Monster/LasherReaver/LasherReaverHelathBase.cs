using UnityEngine;

public class LasherReaverHealthBase : EnemyHealthBase
{
    // Phase hooks reserved for future use
    public System.Action OnPhaseTwo;
    public System.Action OnPhaseThree;

    public bool IsPhaseTwo { get; private set; }
    public bool IsPhaseThree { get; private set; }

    [SerializeField][Range(0f, 1f)] private float phaseTwoThreshold = 0.6f;
    [SerializeField][Range(0f, 1f)] private float phaseThreeThreshold = 0.3f;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        if (!IsPhaseTwo && CurrentHP / MaxHP <= phaseTwoThreshold)
        {
            IsPhaseTwo = true;
            OnPhaseTwo?.Invoke();
        }

        if (!IsPhaseThree && CurrentHP / MaxHP <= phaseThreeThreshold)
        {
            IsPhaseThree = true;
            OnPhaseThree?.Invoke();
        }
    }
}