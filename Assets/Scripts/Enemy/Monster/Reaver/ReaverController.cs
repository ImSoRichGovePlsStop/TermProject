using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReaverController : EnemyBase
{
    public enum ReaverState { Wander, Chase, Strafe, Attack }
    public enum ReaverAttackType { None, Dash, Charge }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float dashWeight = 0.5f;

    [Header("Dash")]
    [SerializeField] private float dashAttackRange = 1.5f;
    [SerializeField] private float dashCooldownMin = 1.5f;
    [SerializeField] private float dashCooldownMax = 3f;
    [SerializeField] private float dashSpeed = 7f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashHitRadius = 0.6f;
    [SerializeField] private float dashDamageScale = 1f;

    [Header("Charge")]
    [SerializeField] private float chargeAttackRange = 5f;
    [SerializeField] private float chargeCooldownMin = 3f;
    [SerializeField] private float chargeCooldownMax = 6f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeWindUpDuration = 1f;
    [SerializeField] private float windUpRotateSpeed = 90f;
    [SerializeField] private float chargeHitRadius = 0.7f;
    [SerializeField] private float chargeDamageScale = 1.2f;
    [SerializeField] private float chargeRotateSpeed = 30f;
    [SerializeField] private float chargeWallDetectRange = 0.5f;
    [SerializeField] private float chargeStunnedDuration = 2f;

    [Header("Charge Redirect")]
    [SerializeField] private float missDetectAngle = 120f;
    [SerializeField] private float missDetectDuration = 0.1f;
    [SerializeField] private float redirectChance = 0.25f;
    [SerializeField] private float brakeDuration = 0.1f;
    [SerializeField] private float redirectWindUpDuration = 0.5f;
    [SerializeField] private int maxRedirectCount = 1;

    private ReaverState currentState = ReaverState.Wander;
    private ReaverAttackType currentAttackType = ReaverAttackType.None;

    private bool isAttacking = false;
    private float lastDashTime = -Mathf.Infinity;
    private float lastChargeTime = -Mathf.Infinity;
    private float currentDashCooldown = 0f;
    private float currentChargeCooldown = 0f;

    // dash
    private bool isDashing = false;
    private Vector3 lockedAttackDir = Vector3.zero;

    // charge
    private bool isCharging = false;
    private bool isStunned = false;
    private Vector3 chargeDir = Vector3.zero;
    private int currentRedirectCount = 0;
    private bool hasRolledRedirect = false;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        currentDashCooldown = Random.Range(dashCooldownMin, dashCooldownMax);
        currentChargeCooldown = Random.Range(chargeCooldownMin, chargeCooldownMax);
        lastChargeTime = Time.time;
        strafe.Init(dashAttackRange);
    }

    private LayerMask GetWallMask()
    {
        return (1 << LayerMask.NameToLayer("Wall"))
             | (1 << LayerMask.NameToLayer("Barrier"));
    }

    private LayerMask GetHitMask()
    {
        return (1 << LayerMask.NameToLayer("Player"))
             | (1 << LayerMask.NameToLayer("Summoner"))
             | (1 << LayerMask.NameToLayer("Totem"));
    }

    protected override void UpdateState()
    {
        if (isAttacking) { currentState = ReaverState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != ReaverState.Wander)
                wander.Reset(movement, stats);
            currentState = ReaverState.Wander;
            return;
        }

        if (currentState == ReaverState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canDash = Time.time >= lastDashTime + currentDashCooldown;
        bool canCharge = Time.time >= lastChargeTime + currentChargeCooldown;
        bool inDashRange = dist <= dashAttackRange;
        bool inChargeRange = dist <= chargeAttackRange;

        if ((canDash && inDashRange) || (canCharge && inChargeRange))
            currentState = ReaverState.Attack;
        else if (inDashRange)
            currentState = ReaverState.Strafe;
        else
        {
            if (currentState == ReaverState.Strafe) strafe.Reset();
            currentState = ReaverState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case ReaverState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case ReaverState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case ReaverState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;

            case ReaverState.Attack:
                if (!isAttacking || lockedAttackDir == Vector3.zero)
                    movement.FaceTarget(TargetPosition);
                else
                    movement.FaceTarget(transform.position + lockedAttackDir);

                if (!isAttacking)
                {
                    var agent = movement.GetAgent();
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                        movement.MoveToTarget(TargetPosition);
                }
                else
                {
                    var agent = movement.GetAgent();
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                        movement.StopMoving();
                }

                TryAttack();
                break;
        }
    }

    private void TryAttack()
    {
        if (isAttacking) return;

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canDash = Time.time >= lastDashTime + currentDashCooldown && dist <= dashAttackRange;
        bool canCharge = Time.time >= lastChargeTime + currentChargeCooldown && dist <= chargeAttackRange;

        if (!canDash && !canCharge) return;

        isAttacking = true;

        if (canDash && canCharge)
            currentAttackType = Random.value < dashWeight ? ReaverAttackType.Dash : ReaverAttackType.Charge;
        else if (canDash)
            currentAttackType = ReaverAttackType.Dash;
        else
            currentAttackType = ReaverAttackType.Charge;

        if (currentAttackType == ReaverAttackType.Dash)
            animator?.SetTrigger("Attack1");
        else
            animator?.SetTrigger("ChargeWindUp");
    }

    public override bool CanBeInterrupted()
    {
        if (currentAttackType == ReaverAttackType.Dash)
            return !isDashing;
        if (currentAttackType == ReaverAttackType.Charge)
            return !isCharging && !isStunned;
        return true;
    }

    protected override void OnHurtTriggered()
    {
        if (!CanBeInterrupted()) return;

        isAttacking = false;
        isDashing = false;
        isCharging = false;
        isStunned = false;
        currentRedirectCount = 0;
        hasRolledRedirect = false;
        lockedAttackDir = Vector3.zero;
        chargeDir = Vector3.zero;
        currentAttackType = ReaverAttackType.None;

        health.StopFlashBuildup();
        if (animator != null) animator.speed = 1f;

        var agent = movement.GetAgent();
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            movement.SetCanMove(true);
        }

        StopCoroutine(nameof(DashRoutine));
        StopCoroutine(nameof(WindUpRoutine));
        StopCoroutine(nameof(ChargeRoutine));
        StopCoroutine(nameof(BrakeAndRedirectRoutine));
        StopCoroutine(nameof(StunnedRoutine));
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
        StartCoroutine(DashRoutine(lockedAttackDir, dashSpeed, dashDuration, dashHitRadius, dashDamageScale, () => isDashing = false));
    }

    public void StartCharge()
    {
        StartCoroutine(WindUpRoutine());
    }

    private IEnumerator WindUpRoutine()
    {
        if (HasTarget)
        {
            Vector3 toTarget = TargetPosition - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f) lockedAttackDir = toTarget.normalized;
        }

        animator?.ResetTrigger("ChargeStart");
        yield return null;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Charge_WindUp"))
            yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / chargeWindUpDuration;
        }

        float elapsed = 0f;
        while (elapsed < chargeWindUpDuration)
        {
            elapsed += Time.deltaTime;
            if (HasTarget)
            {
                Vector3 toTarget = TargetPosition - transform.position;
                toTarget.y = 0f;
                if (lockedAttackDir.sqrMagnitude > 0.001f && toTarget.sqrMagnitude > 0.001f)
                    lockedAttackDir = Vector3.RotateTowards(lockedAttackDir.normalized, toTarget.normalized, windUpRotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }
            movement.FaceTarget(transform.position + lockedAttackDir);
            yield return null;
        }

        if (!isAttacking) yield break;
        if (animator != null) animator.speed = 1f;
        chargeDir = lockedAttackDir;
        isCharging = true;
        animator?.SetTrigger("ChargeStart");
        StartCoroutine(ChargeRoutine());
    }

    public void FinishCharge()
    {
        isAttacking = false;
        isCharging = false;
        isStunned = false;
        currentRedirectCount = 0;
        hasRolledRedirect = false;
        lockedAttackDir = Vector3.zero;
        chargeDir = Vector3.zero;
        currentAttackType = ReaverAttackType.None;
        lastChargeTime = Time.time;
        currentChargeCooldown = Random.Range(chargeCooldownMin, chargeCooldownMax);
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    public void FinishAttack()
    {
        isAttacking = false;
        isDashing = false;
        isCharging = false;
        lockedAttackDir = Vector3.zero;
        chargeDir = Vector3.zero;
        currentAttackType = ReaverAttackType.None;
        lastDashTime = Time.time;
        currentDashCooldown = Random.Range(dashCooldownMin, dashCooldownMax);
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // Coroutines

    private IEnumerator ChargeRoutine()
    {
        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        LayerMask hitMask = GetHitMask();
        LayerMask wallMask = GetWallMask();
        var alreadyHit = new HashSet<GameObject>();
        float missTimer = 0f;
        bool playerHit = false;

        while (isCharging)
        {
            if (HasTarget)
            {
                Vector3 toTarget = TargetPosition - transform.position;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.001f) toTarget.Normalize();
                chargeDir = Vector3.RotateTowards(chargeDir, toTarget, chargeRotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
                chargeDir.y = 0f;
                if (chargeDir.sqrMagnitude > 0.001f) chargeDir.Normalize();
                lockedAttackDir = chargeDir;
            }

            if (Physics.Raycast(transform.position, chargeDir, chargeWallDetectRange + chargeSpeed * stats.MoveSpeedRatio * Time.deltaTime, wallMask))
            {
                isCharging = false;
                if (agent != null && !agent.enabled) agent.enabled = true;
                animator?.SetTrigger("ChargeHitWall");
                StartCoroutine(StunnedRoutine());
                yield break;
            }

            transform.position += chargeDir * chargeSpeed * stats.MoveSpeedRatio * Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, chargeHitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);
                if (col.GetComponent<PlayerStats>() != null || col.GetComponentInParent<PlayerStats>() != null)
                    playerHit = true;
                ApplyDamage(col, stats.Damage * chargeDamageScale);
            }

            if (!playerHit && !hasRolledRedirect && HasTarget && currentRedirectCount < maxRedirectCount)
            {
                Vector3 toPlayer = TargetPosition - transform.position;
                toPlayer.y = 0f;
                float angle = Vector3.Angle(chargeDir, toPlayer);
                if (angle > missDetectAngle * 0.5f)
                {
                    missTimer += Time.deltaTime;
                    if (missTimer >= missDetectDuration)
                    {
                        hasRolledRedirect = true;
                        if (Random.value < redirectChance)
                        {
                            isCharging = false;
                            if (agent != null && !agent.enabled) agent.enabled = true;
                            currentRedirectCount++;
                            StartCoroutine(BrakeAndRedirectRoutine());
                            yield break;
                        }
                    }
                }
                else
                {
                    missTimer = 0f;
                }
            }

            movement.FaceTarget(transform.position + chargeDir);
            yield return null;
        }

        if (agent != null && !agent.enabled) agent.enabled = true;
    }

    private IEnumerator BrakeAndRedirectRoutine()
    {
        LayerMask wallMask = GetWallMask();
        var agent = movement.GetAgent();

        float elapsed = 0f;
        float startSpeed = chargeSpeed;
        while (elapsed < brakeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / brakeDuration;
            float currentSpeed = Mathf.Lerp(startSpeed, 0f, 1f - Mathf.Pow(1f - t, 2f));
            transform.position += chargeDir * currentSpeed * stats.MoveSpeedRatio * Time.deltaTime;

            if (Physics.Raycast(transform.position, chargeDir, chargeWallDetectRange + chargeSpeed * stats.MoveSpeedRatio * Time.deltaTime, wallMask))
            {
                if (agent != null && !agent.enabled) agent.enabled = true;
                animator?.SetTrigger("ChargeHitWall");
                StartCoroutine(StunnedRoutine());
                yield break;
            }

            yield return null;
        }

        hasRolledRedirect = false;
        lockedAttackDir = HasTarget
            ? (TargetPosition - transform.position).normalized
            : lockedAttackDir;

        animator?.ResetTrigger("ChargeStart");
        animator?.SetTrigger("ChargeRedirectWindUp");

        yield return null;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Charge_RedirectWindUp"))
            yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / redirectWindUpDuration;
        }

        float windUpElapsed = 0f;
        while (windUpElapsed < redirectWindUpDuration)
        {
            windUpElapsed += Time.deltaTime;
            if (HasTarget)
            {
                Vector3 toTarget = TargetPosition - transform.position;
                toTarget.y = 0f;
                if (lockedAttackDir.sqrMagnitude > 0.001f && toTarget.sqrMagnitude > 0.001f)
                    lockedAttackDir = Vector3.RotateTowards(lockedAttackDir.normalized, toTarget.normalized, windUpRotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
            }
            movement.FaceTarget(transform.position + lockedAttackDir);
            yield return null;
        }

        if (!isAttacking)
        {
            if (animator != null) animator.speed = 1f;
            yield break;
        }
        if (animator != null) animator.speed = 1f;
        chargeDir = lockedAttackDir;
        isCharging = true;
        animator?.SetTrigger("ChargeStart");
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;
        StartCoroutine(ChargeRoutine());
    }

    private IEnumerator StunnedRoutine()
    {
        isStunned = true;
        yield return new WaitForSeconds(chargeStunnedDuration);
        isStunned = false;
        animator?.SetTrigger("ChargeStunnedEnd");
        FinishCharge();
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
        Gizmos.DrawWireSphere(transform.position, dashAttackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, chargeAttackRange);
        Gizmos.color = Color.cyan;
        if (chargeDir.sqrMagnitude > 0.001f)
            Gizmos.DrawRay(transform.position, chargeDir * chargeWallDetectRange);
    }
}