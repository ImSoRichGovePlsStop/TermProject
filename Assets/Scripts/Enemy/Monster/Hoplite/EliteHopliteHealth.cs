using UnityEngine;

public class EliteHopliteHealth : DefaultEnemyHealth
{
    [Header("Elite Guard")]
    [SerializeField] private EliteHopliteGuard guard;
    [SerializeField] private float blockedDamage = 0.1f;

    protected override void CacheComponents()
    {
        base.CacheComponents();

        if (guard == null)
            guard = GetComponent<EliteHopliteGuard>();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        // ถ้ากำลัง guard อยู่ -> รับ chip damage แต่ไม่เข้า hurt
        if (guard != null && guard.IsGuarding)
        {
            float finalDamage = base.ModifyIncomingDamage(blockedDamage);

            if (finalDamage <= 0f) return;

            currentHP -= finalDamage;
            OnDamageTaken(finalDamage);
            RaiseDamageReceived(finalDamage, isCrit);

            if (currentHP <= 0f)
            {
                currentHP = 0f;
                Die();
            }

            Debug.Log("[EliteHopliteHealth] Guard blocked most damage");
            return;
        }

        // ปกติใช้ flow เดิม
        base.TakeDamage(damage, isCrit);
    }
}