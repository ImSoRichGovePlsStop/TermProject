using System.Collections;
using UnityEngine;

public class CerberusHealth : BaseBossHealth
{
    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("Cerberus Phase Threshold")]
    [SerializeField] private float phase2Threshold = 0.5f;
    [SerializeField] private float phase3Threshold = 0.2f;

    [Header("Cerberus References")]
    [SerializeField] private CerberusController controller;
    [SerializeField] private BossMovement movement;
    [SerializeField] private CerberusAttack attack;

    private bool phase2Triggered = false;
    private bool phase3Triggered = false;

    protected override void Awake()
    {
        base.Awake();

        if (controller == null)
            controller = GetComponent<CerberusController>();

        if (movement == null)
            movement = GetComponent<BossMovement>();

        if (attack == null)
            attack = GetComponent<CerberusAttack>();
    }
    private void Log(string msg)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[CerberusHealth] {msg}");
    }

    public override void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        currentHP -= damage;
        Log($"Cerberus took {damage} damage, current HP: {currentHP}/{maxHP}");
        currentHP = Mathf.Max(0f, currentHP);

        float hpPercent = currentHP / maxHP;

        if (!phase2Triggered && hpPercent <= phase2Threshold)
        {
            phase2Triggered = true;
            isHurt = false;

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
            Die();
            return;
        }

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtCoroutine());
    }

    protected override IEnumerator HurtCoroutine()
    {
        isHurt = true;

        if (attack != null)
            attack.ForceStopAllAttacks();

        if (movement != null)
            movement.StopMoving();

        if (animator != null)
            animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
        hurtRoutine = null;
    }

    protected override void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;

        if (controller != null)
            controller.Die();

        if (attack != null)
            attack.ForceStopAllAttacks();

        if (bossCollider != null)
            bossCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, destroyDelay);
    }
}