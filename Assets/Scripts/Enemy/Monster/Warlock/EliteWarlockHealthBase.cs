using UnityEngine;

public class EliteWarlockHealthBase : EnemyHealthBase
{
    [SerializeField][Range(0f, 1f)] private float enrageThreshold = 0.5f;

    public bool IsEnraged { get; private set; }

    public System.Action OnEnrage;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        if (!IsEnraged && CurrentHP / MaxHP <= enrageThreshold)
        {
            IsEnraged = true;
            OnEnrage?.Invoke();
        }
    }
}