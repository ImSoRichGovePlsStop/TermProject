using UnityEngine;

public class MinibossHarpyHealthBase : EnemyHealthBase
{
    [Header("Enrage")]
    [SerializeField] private float enrageThreshold = 0.5f;

    public System.Action OnEnrage;

    private bool isEnraged = false;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        base.TakeDamage(damage, isCrit);

        if (!isEnraged && CurrentHP / MaxHP <= enrageThreshold)
        {
            isEnraged = true;
            OnEnrage?.Invoke();
        }
    }
}