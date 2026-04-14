using UnityEngine;

public class DummyHealthBase : EnemyHealthBase
{
    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (IsInvincible) return;
        if (damage <= 0f) return;

        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null)
            damage *= entityStats.DamageTaken;

        float newHP = currentHP - damage;

        if (newHP <= 0f)
        {
            currentHP = maxHP;
            RaiseOnDamageReceived(damage, isCrit);
            TryFlash();
            return;
        }

        base.TakeDamage(damage, isCrit);
    }
}