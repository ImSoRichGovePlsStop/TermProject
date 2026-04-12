using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HuskController : EnemyBase
{
    public enum HuskState { Wander, Chase, Strafe, Attack }
    public enum HuskAttackType { None, Attack1, Attack2, Attack3 }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldownMin = 1.5f;
    [SerializeField] private float attackCooldownMax = 3f;

    [Header("Attack Weights")]
    [Range(0f, 1f)][SerializeField] private float attack1Weight = 0.33f;
    [Range(0f, 1f)][SerializeField] private float attack2Weight = 0.33f;

    [Header("Attack 1")]
    [SerializeField] private float attack1DashSpeed = 6f;
    [SerializeField] private float attack1DashDuration = 0.15f;
    [SerializeField] private float attack1HitRadius = 0.6f;
    [SerializeField] private float attack1DamageScale = 1f;

    [Header("Attack 2")]
    [SerializeField] private float attack2DashSpeed = 8f;
    [SerializeField] private float attack2DashDuration = 0.12f;
    [SerializeField] private float attack2HitRadius = 0.6f;
    [SerializeField] private float attack2DamageScale = 1.2f;

    [Header("Attack 3")]
    [SerializeField] private float attack3DashSpeed = 5f;
    [SerializeField] private float attack3DashDuration = 0.2f;
    [SerializeField] private float attack3HitRadius = 0.8f;
    [SerializeField] private float attack3DamageScale = 1.5f;

    private HuskState currentState = HuskState.Wander;
    private HuskAttackType currentAttackType = HuskAttackType.None;

    private bool isAttacking = false;
    private bool isDashing = false;
    private float lastAttackTime = -Mathf.Infinity;
    private float currentAttackCooldown = 0f;
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
        if (isAttacking) { currentState = HuskState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != HuskState.Wander)
                wander.Reset(movement, stats);
            currentState = HuskState.Wander;
            return;
        }

        if (currentState == HuskState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
            currentState = HuskState.Attack;
        else if (!canAttack && dist <= attackRange)
            currentState = HuskState.Strafe;
        else
        {
            if (currentState == HuskState.Strafe) strafe.Reset();
            currentState = HuskState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case HuskState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case HuskState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case HuskState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;

            case HuskState.Attack:
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

        float roll = Random.value;
        if (roll < attack1Weight)
            currentAttackType = HuskAttackType.Attack1;
        else if (roll < attack1Weight + attack2Weight)
            currentAttackType = HuskAttackType.Attack2;
        else
            currentAttackType = HuskAttackType.Attack3;

        switch (currentAttackType)
        {
            case HuskAttackType.Attack1: animator?.SetTrigger("Attack1"); break;
            case HuskAttackType.Attack2: animator?.SetTrigger("Attack2"); break;
            case HuskAttackType.Attack3: animator?.SetTrigger("Attack3"); break;
        }
    }

    public override bool CanBeInterrupted() => !isDashing;

    protected override void OnHurtTriggered()
    {
        if (!CanBeInterrupted()) return;

        isAttacking = false;
        isDashing = false;
        lockedAttackDir = Vector3.zero;
        currentAttackType = HuskAttackType.None;

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
        StartCoroutine(DashRoutine());
    }

    public void FinishAttack()
    {
        isAttacking = false;
        isDashing = false;
        lockedAttackDir = Vector3.zero;
        currentAttackType = HuskAttackType.None;
        lastAttackTime = Time.time;
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // Coroutines

    private IEnumerator DashRoutine()
    {
        isDashing = true;

        float speed, duration, hitRadius, damageScale;
        switch (currentAttackType)
        {
            case HuskAttackType.Attack2:
                speed = attack2DashSpeed; duration = attack2DashDuration;
                hitRadius = attack2HitRadius; damageScale = attack2DamageScale;
                break;
            case HuskAttackType.Attack3:
                speed = attack3DashSpeed; duration = attack3DashDuration;
                hitRadius = attack3HitRadius; damageScale = attack3DamageScale;
                break;
            default:
                speed = attack1DashSpeed; duration = attack1DashDuration;
                hitRadius = attack1HitRadius; damageScale = attack1DamageScale;
                break;
        }

        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));
        var alreadyHit = new HashSet<GameObject>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            transform.position += lockedAttackDir * speed * stats.MoveSpeedRatio * Time.deltaTime;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);
                ApplyDamage(col, stats.Damage * damageScale);
            }
            yield return null;
        }

        if (agent != null && !agent.enabled) agent.enabled = true;
        isDashing = false;
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