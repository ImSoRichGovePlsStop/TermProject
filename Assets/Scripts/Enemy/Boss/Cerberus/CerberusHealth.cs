using System.Collections;
using UnityEngine;

public class CerberusHealth : BaseBossHealth
{
    [Header("Cerberus Phase Threshold")]
    [SerializeField] private float phase2Threshold = 0.5f;
    [SerializeField] private float phase3Threshold = 0.2f;

    [Header("Cerberus References")]
    [SerializeField] private CerberusController controller;
    [SerializeField] private BossMovement movement;
    [SerializeField] private CerberusAttack attack;

    private bool phase2Triggered = false;
    private bool phase3Triggered = false;
    private PlayerCombatContext context;

    protected override void Awake()
    {
        base.Awake();

        if (controller == null) controller = GetComponent<CerberusController>();
        if (movement == null) movement = GetComponent<BossMovement>();
        if (attack == null) attack = GetComponent<CerberusAttack>();
    }

    protected override void Start()
    {
        base.Start();
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            context = playerObj.GetComponent<PlayerCombatContext>();
    }

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        // apply DamageTaken multiplier manually before phase check
        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null) damage *= entityStats.DamageTaken;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        RaiseOnDamageReceived(damage, isCrit);
        TryFlash();

        float hpPercent = currentHP / maxHP;

        if (!phase2Triggered && hpPercent <= phase2Threshold)
        {
            phase2Triggered = true;
            isHurt = false;
            attack?.ForceStopAllAttacks();
            movement?.StopMoving();
            controller?.EnterPhase2();
            return;
        }

        if (!phase3Triggered && hpPercent <= phase3Threshold)
        {
            phase3Triggered = true;
            isHurt = false;
            attack?.ForceStopAllAttacks();
            movement?.StopMoving();
            controller?.EnterPhase3();
            return;
        }

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        isHurt = true;
        OnHurtStart();
    }

    protected override void OnHurtStart()
    {
        attack?.ForceStopAllAttacks();
        movement?.StopMoving();
    }

    protected override void OnDeathStart()
    {
        controller?.Die();
        attack?.ForceStopAllAttacks();
        context?.NotifyEnemyKilled((HealthBase)this);
    }

    protected override void OnDie()
    {
        Destroy(gameObject, destroyDelay);
    }

}