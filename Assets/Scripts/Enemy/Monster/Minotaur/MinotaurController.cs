using System.Collections;
using UnityEngine;

public class MinotaurController : EnemyBase
{
    public enum MinotaurState { Wander, Chase, Strafe, Attack }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackDamageScale = 1f;
    [SerializeField] private float attackCooldownMin = 1.5f;
    [SerializeField] private float attackCooldownMax = 3f;

    [Header("Dash Attack")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashHitRadius = 0.8f;

    private MinotaurState currentState = MinotaurState.Wander;
    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
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
        if (isAttacking) { currentState = MinotaurState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != MinotaurState.Wander)
                wander.Reset(movement, stats);
            currentState = MinotaurState.Wander;
            return;
        }

        if (currentState == MinotaurState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
            currentState = MinotaurState.Attack;
        else if (!canAttack && dist <= attackRange)
            currentState = MinotaurState.Strafe;
        else
        {
            if (currentState == MinotaurState.Strafe) strafe.Reset();
            currentState = MinotaurState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case MinotaurState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;
            case MinotaurState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;
            case MinotaurState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;
            case MinotaurState.Attack:
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

    private bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + currentAttackCooldown) return false;

        isAttacking = true;
        animator.SetTrigger("Attack");
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

        isDashing = false;
        strafe.Reset();
    }

    // Animation Event
    public void LockAttackDirection()
    {
        lockedAttackDir = TargetPosition - transform.position;
        lockedAttackDir.y = 0f;
        if (lockedAttackDir.sqrMagnitude > 0.001f) lockedAttackDir.Normalize();
    }

    // Animation Event — e.g. "4,12" = 4 frames buildup at 12fps
    public void StartFlashBuildup(string args)
    {
        var parts = args.Split(',');
        int frames = int.Parse(parts[0]);
        int fps = int.Parse(parts[1]);
        float duration = frames / (float)fps;
        health.StartFlashBuildup(Color.white, duration, 0.4f);
    }

    // Animation Event
    public void FlashWhite()
    {
        health.StopFlashBuildup();
        health.TryFlash(Color.white);
    }

    // Animation Event
    public void DashAttack()
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

        var alreadyHit = new System.Collections.Generic.HashSet<GameObject>();
        float elapsed = 0f;

        while (elapsed < dashDuration)
        {
            transform.position += dashDir * dashSpeed * stats.MoveSpeedRatio * Time.deltaTime;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, dashHitRadius, hitMask);
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
    public void FinishAttack()
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