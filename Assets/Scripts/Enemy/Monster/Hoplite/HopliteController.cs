using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HopliteController : EnemyBase
{
    public enum HopliteState { Wander, Chase, Attack }

    [Header("Attack")]
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float attackDamageScale = 1f;
    [SerializeField] protected float attackCooldown = 1.5f;

    [Header("Attack Dash")]
    [SerializeField] private float attackDashSpeed = 6f;
    [SerializeField] private float attackDashDuration = 0.15f;
    [SerializeField] private float attackDashHitRadius = 0.6f;

    protected HopliteState currentState = HopliteState.Wander;
    protected bool isAttacking = false;
    public bool IsAttacking => isAttacking;
    protected float lastAttackTime = -Mathf.Infinity;

    private bool isDashing = false;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(attackRange * 0.8f);
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
        currentState = dist <= attackRange ? HopliteState.Attack : HopliteState.Chase;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case HopliteState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case HopliteState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case HopliteState.Attack:
                movement.FaceTarget(TargetPosition);
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
        if (Time.time < lastAttackTime + attackCooldown) return false;

        isAttacking = true;
        animator?.SetTrigger("Attack");
        return true;
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

        Vector3 dashDir = (TargetPosition - transform.position);
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
        TriggerPostAttackDelay();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}