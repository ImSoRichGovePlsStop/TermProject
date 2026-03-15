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

    protected override void CacheComponents()
    {
        base.CacheComponents();

        if (controller == null)
            controller = GetComponent<CerberusController>();

        if (movement == null)
            movement = GetComponent<BossMovement>();

        if (attack == null)
            attack = GetComponent<CerberusAttack>();
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        float finalDamage = ModifyIncomingDamage(damage);
        if (finalDamage <= 0f) return;

        currentHP -= finalDamage;
        OnDamageTaken(finalDamage);

        float hpPercent = currentHP / maxHP;

        if (!phase2Triggered && hpPercent <= phase2Threshold)
        {
            phase2Triggered = true;
            isHurt = false;

            if (hurtCoroutine != null)
            {
                StopCoroutine(hurtCoroutine);
                hurtCoroutine = null;
            }

            if (attack != null)
                attack.ForceStopAllAttacks();

            if (movement != null)
                movement.StopMoving();

            if (controller != null)
                controller.EnterPhase2();

            return;
        }

        if (!phase3Triggered && hpPercent <= phase3Threshold)
        {
            phase3Triggered = true;
            isHurt = false;

            if (hurtCoroutine != null)
            {
                StopCoroutine(hurtCoroutine);
                hurtCoroutine = null;
            }

            if (attack != null)
                attack.ForceStopAllAttacks();

            if (movement != null)
                movement.StopMoving();

            if (controller != null)
                controller.EnterPhase3();

            return;
        }

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            Die();
            return;
        }

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    protected override void OnHurtStart()
    {
        if (attack != null)
            attack.ForceStopAllAttacks();

        if (movement != null)
            movement.StopMoving();
    }

    protected override void OnHurtEnd()
    {
        base.OnHurtEnd();
    }

    protected override void OnDeathStart()
    {
        if (controller != null)
            controller.Die();

        if (attack != null)
            attack.ForceStopAllAttacks();

        base.OnDeathStart();
    }

    protected override void HandleRigidbodyOnDeath()
    {
        if (rb == null) return;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }
}