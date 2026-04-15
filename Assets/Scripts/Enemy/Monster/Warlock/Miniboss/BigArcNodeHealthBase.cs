using UnityEngine;

public class BigArcNodeHealthBase : HealthBase
{
    [SerializeField] private float maxHp = 100f;

    public System.Action OnDamaged;

    private bool isInvincible = false;

    protected override void Awake()
    {
        maxHP = maxHp;
        base.Awake();
        DamageNumberSpawner.Instance?.RegisterEntity(this, healthBarHeight);
    }

    public void SetMaxHp(float value)
    {
        maxHP = value;
        currentHP = value;
    }

    public void SetInvincible(bool value)
    {
        isInvincible = value;
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (isInvincible) return;
        base.TakeDamage(damage, isCrit);
        OnDamaged?.Invoke();
    }

    protected override void OnDie()
    {
        Destroy(gameObject);
    }
}