using System;
using UnityEngine;

public enum EnemyTier { Normal, Elite, Miniboss, Boss }

public class EnemyHealthBase : HealthBase
{
    [Header("Tier")]
    [SerializeField] private EnemyTier tier = EnemyTier.Normal;
    public EnemyTier Tier => tier;
    public Transform groundPoint;

    private EnemyBase controller;
    private PlayerCombatContext context;

    // Shield
    public float CurrentShield { get; private set; }
    public float MaxShield { get; private set; }
    public bool HasShield => CurrentShield > 0f;
    public event Action OnShieldChanged;

    protected override void Awake()
    {
        if (groundPoint == null)
        {
            var t = transform.Find("Visual/GroundPoint");
            if (t == null) t = transform.Find("GroundPoint");
            groundPoint = t;
        }
        base.Awake();
        controller = GetComponent<EnemyBase>();
    }

    protected override void Start()
    {
        base.Start();
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            context = playerObj.GetComponent<PlayerCombatContext>();

        DamageNumberSpawner.Instance?.RegisterEntity(this, healthBarHeight);

        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null && entityStats.ShieldPercent > 0f)
            GainShield(maxHP * entityStats.ShieldPercent / 100f);
    }

    public void ResetHP()
    {
        isDead = false;
        currentHP = maxHP;
        CurrentShield = 0f;
        MaxShield = 0f;
    }

    public void GainShield(float value)
    {
        if (value <= 0f) return;
        MaxShield = value;
        CurrentShield = value;
        OnShieldChanged?.Invoke();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (IsInvincible) return;
        if (damage <= 0f) return;

        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null)
            damage *= entityStats.DamageTaken;

        float realDamage = damage;

        if (CurrentShield > 0f)
        {
            float absorbed = Mathf.Min(CurrentShield, damage);
            CurrentShield -= absorbed;
            CurrentShield = Mathf.Max(CurrentShield, 0f);
            damage -= absorbed;
            OnShieldChanged?.Invoke();
        }

        if (damage <= 0f)
        {
            RaiseOnDamageReceived(realDamage, isCrit);
            TryFlash();
            return;
        }

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        RaiseOnDamageReceived(realDamage, isCrit);
        OnDamageTaken(damage, isCrit);

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        isHurt = true;
        OnHurtStart();
    }

    public void TakeDamage(float damage, HealthBase attacker, bool isCrit = false, bool silent = false)
    {
        suppressHurt = silent;
        TakeDamage(damage, isCrit);
        suppressHurt = false;
    }

    private bool suppressHurt = false;

    protected override void OnDamageTaken(float damage, bool isCrit)
    {
        AudioManager.Instance?.PlayEnemyHit();
        base.OnDamageTaken(damage, isCrit);
    }

    protected override void OnHurtStart()
    {
        if (suppressHurt) return;
        if (controller != null && controller.CanBeInterrupted())
            controller.TriggerHurt();
    }

    protected override void OnDeathStart()
    {
        controller?.OnDeath();
        context?.NotifyEnemyKilled((HealthBase)this);
        GetComponent<MaterialDropHandler>()?.TriggerDrop();
    }
}