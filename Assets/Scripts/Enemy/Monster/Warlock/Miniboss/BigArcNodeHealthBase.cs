using UnityEngine;

public class BigArcNodeHealthBase : HealthBase
{

    public System.Action OnDamaged;

    private BigArcNode bigArcNode;

    protected override void Awake()
    {
        base.Awake();
        bigArcNode = GetComponent<BigArcNode>();
        DamageNumberSpawner.Instance?.RegisterEntity(this, healthBarHeight);
    }

    public void SetMaxHp(float value)
    {
        maxHP = value;
        currentHP = value;
    }

    public void SetInvincible(bool value)
    {
        IsInvincible = value;
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (IsInvincible) return;
        base.TakeDamage(damage, isCrit);
        OnDamaged?.Invoke();
    }

    protected override void OnDie()
    {
        if (bigArcNode != null && bigArcNode.IsPhaseThree)
        {
            OnDamaged?.Invoke();
            return;
        }
        Destroy(gameObject);
    }
}