using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HarpyController : EnemyBase
{
    public enum HarpyState { Wander, Chase, Strafe, Attack }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float attackDamageScale = 1f;
    [SerializeField] protected float attackCooldownMin = 1.5f;
    [SerializeField] protected float attackCooldownMax = 3f;

    [Header("Attack Dash")]
    [SerializeField] protected float attackDashSpeed = 6f;
    [SerializeField] protected float attackDashDuration = 0.15f;
    [SerializeField] protected float attackDashHitRadius = 0.6f;

    protected HarpyState currentState = HarpyState.Wander;
    protected bool isAttacking = false;
    public bool IsAttacking => isAttacking;
    protected float lastAttackTime = -Mathf.Infinity;
    private float currentAttackCooldown = 0f;
    private bool isDashing = false;
    private Vector3 lockedAttackDir = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Init(attackRange);
    }

    protected override void UpdateState()
    {
        if (isAttacking) { currentState = HarpyState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != HarpyState.Wander)
                wander.Reset(movement, stats);
            currentState = HarpyState.Wander;
            return;
        }

        if (currentState == HarpyState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
            currentState = HarpyState.Attack;
        else if (!canAttack && dist <= attackRange)
            currentState = HarpyState.Strafe;
        else
        {
            if (currentState == HarpyState.Strafe) strafe.Reset();
            currentState = HarpyState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case HarpyState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;
            case HarpyState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;
            case HarpyState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;
            case HarpyState.Attack:
                if (!isAttacking || lockedAttackDir == Vector3.zero)
                    movement.FaceTarget(TargetPosition);
                else
                    movement.FaceTarget(transform.position + lockedAttackDir);
                if (!TryAttack())
                    movement.MoveToTarget(TargetPosition);
                else
                    movement.StopMoving();
                break;
        }
    }

    protected bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + currentAttackCooldown) return false;

        isAttacking = true;
        animator?.SetTrigger("Attack");
        return true;
    }

    public override bool CanBeInterrupted() => !isDashing;

    protected override void OnHurtTriggered()
    {
        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        health.StopFlashBuildup();

        var agent = movement.GetAgent();
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            movement.SetCanMove(true);
        }

        StopCoroutine(nameof(DashRoutine));
        isDashing = false;
        strafe.Reset();
    }

    // Animation Event
    public virtual void LockAttackDirection()
    {
        lockedAttackDir = TargetPosition - transform.position;
        lockedAttackDir.y = 0f;
        if (lockedAttackDir.sqrMagnitude > 0.001f) lockedAttackDir.Normalize();
    }

    public virtual void StartFlashBuildup(string args)
    {
        var parts = args.Split(',');
        int frames = int.Parse(parts[0]);
        int fps = int.Parse(parts[1]);
        float duration = frames / (float)fps;
        health.StartFlashBuildup(Color.white, duration, 0.4f);
    }

    // Animation Event
    public virtual void FlashWhite()
    {
        health.StopFlashBuildup();
        health.TryFlash(Color.white);
    }

    // Animation Event
    public virtual void DashAttack()
    {
        if (isDashing) return;
        isDashing = true;
        Vector3 dashDir = lockedAttackDir != Vector3.zero
            ? lockedAttackDir
            : (TargetPosition - transform.position);
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude > 0.001f) dashDir.Normalize();
        StartCoroutine(DashRoutine(dashDir, attackDashSpeed, attackDashDuration, attackDashHitRadius, attackDamageScale, () => isDashing = false));
    }

    // Animation Event
    public virtual void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        lockedAttackDir = Vector3.zero;
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}