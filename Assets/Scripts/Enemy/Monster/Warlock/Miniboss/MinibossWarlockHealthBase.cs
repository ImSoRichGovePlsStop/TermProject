using UnityEngine;

public class MinibossWarlockHealthBase : EnemyHealthBase
{
    [SerializeField][Range(0f, 1f)] private float phaseTwoThreshold = 0.7f;

    public bool IsPhaseTwo { get; private set; }

    public System.Action OnPhaseTwo;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        if (!IsPhaseTwo && CurrentHP / MaxHP <= phaseTwoThreshold)
        {
            IsPhaseTwo = true;
            OnPhaseTwo?.Invoke();
        }
    }
}