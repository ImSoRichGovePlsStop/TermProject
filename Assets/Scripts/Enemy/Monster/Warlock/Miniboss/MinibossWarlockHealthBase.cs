using UnityEngine;

public class MinibossWarlockHealthBase : EnemyHealthBase
{
    [SerializeField][Range(0f, 1f)] private float phaseTwoThreshold = 0.7f;
    [SerializeField][Range(0f, 1f)] private float phaseThreeThreshold = 0.3f;

    public bool IsPhaseTwo { get; private set; }
    public bool IsPhaseThree { get; private set; }

    public System.Action OnPhaseTwo;
    public System.Action OnPhaseThree;

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