using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SkullController : EnemyBase
{
    public enum SkullState { Wander, Chase, Strafe, Attack }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldownMin = 1f;
    [SerializeField] private float attackCooldownMax = 2f;

    [Header("Attack - Dash")]
    [SerializeField] private float attackDashSpeed = 7f;
    [SerializeField] private float attackDashDuration = 0.12f;
    [SerializeField] private float attackHitRadius = 0.5f;
    [SerializeField] private float attackDamageScale = 1f;

    private SkullState currentState = SkullState.Wander;

    private bool isAttacking = false;
    private bool isDashing = false;
    private float lastAttackTime = -Mathf.Infinity;
    private float currentAttackCooldown = 0f;
    private Vector3 lockedAttackDir = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        skipSpawnEffect = true;
        movement.SetStopDistance(0.1f);
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Init(attackRange);
    }

    protected override void UpdateState()
    {
        if (isAttacking) { currentState = SkullState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != SkullState.Wander)
                wander.Reset(movement, stats);
            currentState = SkullState.Wander;
            return;
        }

        if (currentState == SkullState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
            currentState = SkullState.Attack;
        else if (!canAttack && dist <= attackRange)
            currentState = SkullState.Strafe;
        else
        {
            if (currentState == SkullState.Strafe) strafe.Reset();
            currentState = SkullState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case SkullState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case SkullState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case SkullState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;

            case SkullState.Attack:
                if (!isAttacking || lockedAttackDir == Vector3.zero)
                    movement.FaceTarget(TargetPosition);
                else
                    movement.FaceTarget(transform.position + lockedAttackDir);

                if (!isAttacking)
                    movement.MoveToTarget(TargetPosition);
                else
                    movement.StopMoving();

                TryAttack();
                break;
        }
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (Time.time < lastAttackTime + currentAttackCooldown) return;

        isAttacking = true;
        animator?.SetTrigger("Attack");
    }

    public override bool CanBeInterrupted() => !isDashing;

    protected override void OnHurtTriggered()
    {
        if (!CanBeInterrupted()) return;

        isAttacking = false;
        isDashing = false;
        lockedAttackDir = Vector3.zero;

        health.StopFlashBuildup();

        var agent = movement.GetAgent();
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            movement.SetCanMove(true);
        }

        StopCoroutine(nameof(DashRoutine));
        strafe.Reset();
    }

    // Animation Events

    public void LockAttackDirection()
    {
        lockedAttackDir = TargetPosition - transform.position;
        lockedAttackDir.y = 0f;
        if (lockedAttackDir.sqrMagnitude > 0.001f) lockedAttackDir.Normalize();
    }

    public void StartFlashBuildup(string args)
    {
        var parts = args.Split(',');
        int frames = int.Parse(parts[0]);
        int fps = int.Parse(parts[1]);
        health.StartFlashBuildup(Color.white, frames / (float)fps, 0.4f);
    }

    public void FlashWhite()
    {
        health.StopFlashBuildup();
        health.TryFlash(Color.white);
    }

    public void DashAttack()
    {
        if (isDashing) return;
        isDashing = true;
        StartCoroutine(DashRoutine(lockedAttackDir, attackDashSpeed, attackDashDuration, attackHitRadius, attackDamageScale, () => isDashing = false));
    }

    public void FinishAttack()
    {
        isAttacking = false;
        isDashing = false;
        lockedAttackDir = Vector3.zero;
        lastAttackTime = Time.time;
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // Helpers

    private void ApplyDamage(Collider col, float damage)
    {
        var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
        if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, health); return; }

        var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
        if (hb != null && !hb.IsDead && hb != health) hb.TakeDamage(damage);
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}