using UnityEngine;

public class LasherReaverHealthBase : EnemyHealthBase
{
    public System.Action OnPhaseTwo;
    public System.Action OnPhaseThree;
    public System.Action OnPhaseFour;

    public bool IsPhaseTwo { get; private set; }
    public bool IsPhaseThree { get; private set; }
    public bool IsPhaseFour { get; private set; }

    [SerializeField][Range(0f, 1f)] private float phaseTwoThreshold = 0.75f;
    [SerializeField][Range(0f, 1f)] private float phaseThreeThreshold = 0.5f;
    [SerializeField][Range(0f, 1f)] private float phaseFourThreshold = 0.25f;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        float ratio = CurrentHP / MaxHP;

        if (!IsPhaseTwo && ratio <= phaseTwoThreshold)
        {
            IsPhaseTwo = true;
            OnPhaseTwo?.Invoke();
        }

        if (!IsPhaseThree && ratio <= phaseThreeThreshold)
        {
            IsPhaseThree = true;
            OnPhaseThree?.Invoke();
        }

        if (!IsPhaseFour && ratio <= phaseFourThreshold)
        {
            IsPhaseFour = true;
            OnPhaseFour?.Invoke();
        }
    }
}