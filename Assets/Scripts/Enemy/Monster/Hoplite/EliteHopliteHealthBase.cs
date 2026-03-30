using UnityEngine;

public class EliteHopliteHealthBase : EnemyHealthBase
{
    [Header("Guard Block")]
    [SerializeField] private float blockedDamage = 0.1f;
    [SerializeField] private float guardProcChance = 0.2f;
    [SerializeField] private EliteHopliteController eliteController;

    protected override void Awake()
    {
        base.Awake();
        if (eliteController == null)
            eliteController = GetComponent<EliteHopliteController>();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (eliteController != null && eliteController.IsGuarding)
        {
            base.TakeDamage(blockedDamage, false);
            return;
        }

        if (eliteController != null && !eliteController.IsAttacking && Random.value <= guardProcChance)
        {
            eliteController.TryProcGuard();
            base.TakeDamage(blockedDamage, false);
            return;
        }

        base.TakeDamage(damage, isCrit);
    }
}