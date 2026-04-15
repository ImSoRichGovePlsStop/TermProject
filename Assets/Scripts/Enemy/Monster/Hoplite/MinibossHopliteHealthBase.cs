using UnityEngine;

public class MinibossHopliteHealthBase : EnemyHealthBase
{
    [Header("Guard Block")]
    [SerializeField] private float guardHealRatio = 0.5f;
    [SerializeField] private float guardProcChance = 0.2f;
    [SerializeField] private MinibossHopliteController eliteController;

    protected override void Awake()
    {
        base.Awake();
        if (eliteController == null)
            eliteController = GetComponent<MinibossHopliteController>();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (eliteController != null && eliteController.IsGuarding)
        {
            Heal(damage * guardHealRatio);
            return;
        }

        if (eliteController != null && !eliteController.IsAttacking && !eliteController.IsCharging && Random.value <= guardProcChance)
        {
            if (eliteController.TryProcGuard())
            {
                Heal(damage * guardHealRatio);
                return;
            }
        }

        base.TakeDamage(damage, isCrit);
    }
}