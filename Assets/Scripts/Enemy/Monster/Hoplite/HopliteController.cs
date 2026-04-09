using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HopliteController : EnemyBase
{
    public enum HopliteState { Wander, Chase, Strafe, Attack }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float attackDamageScale = 1f;
    [SerializeField] protected float attackCooldownMin = 1.5f;
    [SerializeField] protected float attackCooldownMax = 3f;

    [Header("Attack Dash")]
    [SerializeField] private float attackDashSpeed = 6f;
    [SerializeField] private float attackDashDuration = 0.15f;
    [SerializeField] private float attackDashHitRadius = 0.6f;

    protected HopliteState currentState = HopliteState.Wander;
    protected bool isAttacking = false;
    public bool IsAttacking => isAttacking;
    protected float lastAttackTime = -Mathf.Infinity;
    private float currentAttackCooldown = 0f;

    private bool isDashing = false;
    protected Vector3 lockedAttackDir = Vector3.zero;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Init(attackRange);
    }

    protected override void UpdateState()
    {
        if (isAttacking) { currentState = HopliteState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != HopliteState.Wander)
                wander.Reset(movement, stats);
            currentState = HopliteState.Wander;
            return;
        }

        if (currentState == HopliteState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
        {
            currentState = HopliteState.Attack;
        }
        else if (!canAttack && dist <= attackRange)
        {
            currentState = HopliteState.Strafe;
        }
        else
        {
            if (currentState == HopliteState.Strafe) ResetStrafe();
            currentState = HopliteState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case HopliteState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case HopliteState.Chase:
                TickChase();
                break;

            case HopliteState.Strafe:
                TickStrafe();
                break;

            case HopliteState.Attack:
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

    private void TickChase()
    {
        movement.MoveToTarget(TargetPosition);
    }

    private void TickStrafe()
    {
        strafe.Tick(transform, TargetPosition, movement);
    }

    private void ResetStrafe()
    {
        strafe.Reset();
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

        StopCoroutine(nameof(DashAttackRoutine));
        isDashing = false;
        ResetStrafe();
    }

    protected bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + currentAttackCooldown) return false;

        isAttacking = true;
        animator?.SetTrigger("Attack");
        return true;
    }

    // Animation Event
    public virtual void LockAttackDirection()
    {
        lockedAttackDir = TargetPosition - transform.position;
        lockedAttackDir.y = 0f;
        if (lockedAttackDir.sqrMagnitude > 0.001f) lockedAttackDir.Normalize();
    }

    // Animation Event — e.g. "4,12" = 4 frames buildup at 12fps
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
        StartCoroutine(DashAttackRoutine());
    }

    private IEnumerator DashAttackRoutine()
    {
        isDashing = true;

        Vector3 dashDir = lockedAttackDir != Vector3.zero
            ? lockedAttackDir
            : (TargetPosition - transform.position);
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude > 0.001f) dashDir.Normalize();

        var agent = movement.GetAgent();
        if (agent != null) agent.enabled = false;

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        var alreadyHit = new HashSet<GameObject>();
        float elapsed = 0f;

        while (elapsed < attackDashDuration)
        {
            transform.position += dashDir * attackDashSpeed * stats.MoveSpeedRatio * Time.deltaTime;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, attackDashHitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);

                var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (ps != null && !ps.IsDead) { ps.TakeDamage(stats.Damage * attackDamageScale, health); continue; }

                var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
                if (hb != null && !hb.IsDead) hb.TakeDamage(stats.Damage * attackDamageScale);
            }

            yield return null;
        }

        if (agent != null) agent.enabled = true;
        isDashing = false;
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