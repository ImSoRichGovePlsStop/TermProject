using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LasherController : EnemyBase
{
    public enum LasherState { Wander, Chase, Strafe, Attack }
    public enum LasherAttackType { None, DoubleCombo, Lash }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackCooldownMin = 1.5f;
    [SerializeField] private float attackCooldownMax = 3f;

    [Header("Attack Weights")]
    [Range(0f, 1f)]
    [SerializeField] private float doubleComboWeight = 0.6f;

    [Header("Double Combo")]
    [SerializeField] private float comboDashSpeed = 7f;
    [SerializeField] private float comboDashDuration = 0.15f;
    [SerializeField] private float comboDashHitRadius = 0.6f;
    [SerializeField] private float comboDamageScale = 1f;
    [SerializeField] private float comboMaxRedirectAngle = 60f;
    [SerializeField] private float comboDashGapDuration = 0.2f;

    [Header("Lash")]
    [SerializeField] private float lashSpeedPenalty = -0.6f;
    [SerializeField] private float lashChargeExitRange = 1.2f;
    [SerializeField] private float lashChargeMaxDuration = 3f;
    [SerializeField] private float lashHitRadius = 1.2f;
    [SerializeField] private float lashDamageScale = 1.2f;
    [SerializeField] private float lashTipDelay = 0.25f;
    [SerializeField] private float lashTipOffset = 1.5f;
    [SerializeField] private float lashTipRadius = 1f;
    [SerializeField] private float lashTipDamageScale = 0.8f;

    private LasherState currentState = LasherState.Wander;
    private LasherAttackType currentAttackType = LasherAttackType.None;

    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    private float currentAttackCooldown = 0f;

    // combo
    private bool isInComboGap = false;
    private Vector3 lockedAttackDir = Vector3.zero;

    // lash
    private bool isLashActive = false;
    private bool isLashCharging = false;
    private bool isLashHitPhase = false;
    private float lashChargeTimer = 0f;
    private EntityStatModifier lashSpeedMod;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Init(attackRange);
        lashSpeedMod = new EntityStatModifier { moveSpeed = lashSpeedPenalty };
    }

    protected override void UpdateState()
    {
        if (isAttacking) { currentState = LasherState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != LasherState.Wander)
                wander.Reset(movement, stats);
            currentState = LasherState.Wander;
            return;
        }

        if (currentState == LasherState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = Time.time >= lastAttackTime + currentAttackCooldown;

        if (canAttack && dist <= attackRange)
            currentState = LasherState.Attack;
        else if (!canAttack && dist <= attackRange)
            currentState = LasherState.Strafe;
        else
        {
            if (currentState == LasherState.Strafe) strafe.Reset();
            currentState = LasherState.Chase;
        }
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case LasherState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case LasherState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case LasherState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;

            case LasherState.Attack:
                if (!isAttacking || lockedAttackDir == Vector3.zero)
                    movement.FaceTarget(TargetPosition);
                else
                    movement.FaceTarget(transform.position + lockedAttackDir);

                if (currentAttackType == LasherAttackType.Lash && isLashActive && !isLashHitPhase)
                {
                    movement.MoveToTarget(TargetPosition);

                    if (isLashCharging)
                    {
                        lashChargeTimer += Time.deltaTime;
                        float dist = Vector3.Distance(transform.position, TargetPosition);
                        if (dist <= lashChargeExitRange || lashChargeTimer >= lashChargeMaxDuration)
                        {
                            isLashCharging = false;
                            animator?.SetTrigger("Attack2End");
                        }
                    }
                }
                else if (!isAttacking)
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
        currentAttackType = Random.value < doubleComboWeight
            ? LasherAttackType.DoubleCombo
            : LasherAttackType.Lash;

        if (currentAttackType == LasherAttackType.DoubleCombo)
            animator?.SetTrigger("Attack1");
        else
            animator?.SetTrigger("Attack2");
    }

    public override bool CanBeInterrupted()
    {
        if (currentAttackType == LasherAttackType.DoubleCombo)
            return isInComboGap;
        if (currentAttackType == LasherAttackType.Lash)
            return !isLashHitPhase;
        return true;
    }

    protected override void OnHurtTriggered()
    {
        if (!CanBeInterrupted()) return;

        isAttacking = false;
        isLashActive = false;
        isLashCharging = false;
        isLashHitPhase = false;
        lashChargeTimer = 0f;
        isInComboGap = false;
        lockedAttackDir = Vector3.zero;
        currentAttackType = LasherAttackType.None;

        stats.RemoveMultiplierModifier(lashSpeedMod);
        health.StopFlashBuildup();

        var agent = movement.GetAgent();
        if (agent != null && !agent.enabled)
        {
            agent.enabled = true;
            movement.SetCanMove(true);
        }

        StopAllCoroutines();
        strafe.Reset();
    }

    // ??? Animation Events ???????????????????????????????????????????

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

    // Attack1_Dash1
    public void DashAttackFirst()
    {
        isInComboGap = false;
        StartCoroutine(SingleDash(lockedAttackDir));
    }

    // Attack1_Dash1 ???? clip
    public void OnComboGap()
    {
        isInComboGap = true;
        StartCoroutine(ComboGapRoutine());
    }

    // Attack1_Dash2
    public void DashAttackSecond()
    {
        isInComboGap = false;

        Vector3 newDir = TargetPosition - transform.position;
        newDir.y = 0f;
        if (newDir.sqrMagnitude > 0.001f) newDir.Normalize();

        float angle = Vector3.Angle(lockedAttackDir, newDir);
        Vector3 dash2Dir = angle <= comboMaxRedirectAngle
            ? newDir
            : Vector3.RotateTowards(lockedAttackDir, newDir, comboMaxRedirectAngle * Mathf.Deg2Rad, 0f);

        StartCoroutine(SingleDash(dash2Dir));
    }

    // Attack2_Start ???? clip
    public void StartLashCharge()
    {
        isLashActive = true;
        isLashCharging = true;
        lashChargeTimer = 0f;
        stats.AddMultiplierModifier(lashSpeedMod);
    }

    // Attack2_End
    public void LashHit()
    {
        isLashHitPhase = true;
        movement.StopMoving();
        StartCoroutine(LashHitRoutine());
    }

    public void FinishAttack()
    {
        isAttacking = false;
        isLashActive = false;
        isLashCharging = false;
        isLashHitPhase = false;
        lashChargeTimer = 0f;
        isInComboGap = false;
        lockedAttackDir = Vector3.zero;
        currentAttackType = LasherAttackType.None;
        stats.RemoveMultiplierModifier(lashSpeedMod);
        lastAttackTime = Time.time;
        currentAttackCooldown = Random.Range(attackCooldownMin, attackCooldownMax);
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // ??? Coroutines ??????????????????????????????????????????????????

    private IEnumerator ComboGapRoutine()
    {
        yield return new WaitForSeconds(comboDashGapDuration);
        isInComboGap = false;
        animator?.SetTrigger("Attack1Dash2");
    }

    private IEnumerator SingleDash(Vector3 dashDir)
    {
        var agent = movement.GetAgent();
        if (agent != null) agent.enabled = false;

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));
        var alreadyHit = new HashSet<GameObject>();

        float elapsed = 0f;
        while (elapsed < comboDashDuration)
        {
            transform.position += dashDir * comboDashSpeed * stats.MoveSpeedRatio * Time.deltaTime;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, comboDashHitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);
                ApplyDamage(col, stats.Damage * comboDamageScale);
            }
            yield return null;
        }

        if (agent != null) agent.enabled = true;
    }

    private IEnumerator LashHitRoutine()
    {
        Vector3 hitPos = transform.position;
        DealAoeDamage(hitPos, lashHitRadius, lashDamageScale);

        yield return new WaitForSeconds(lashTipDelay);

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.001f) forward.Normalize();
        DealAoeDamage(hitPos + forward * lashTipOffset, lashTipRadius, lashTipDamageScale);

        isLashHitPhase = false;
    }

    // ??? Helpers ?????????????????????????????????????????????????????

    private void ApplyDamage(Collider col, float damage)
    {
        var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
        if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, health); return; }

        var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
        if (hb != null && !hb.IsDead && hb != health) hb.TakeDamage(damage);
    }

    private void DealAoeDamage(Vector3 center, float radius, float damageScale)
    {
        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        var alreadyHit = new HashSet<GameObject>();
        Collider[] hits = Physics.OverlapSphere(center, radius, hitMask);
        foreach (var col in hits)
        {
            if (alreadyHit.Contains(col.gameObject)) continue;
            alreadyHit.Add(col.gameObject);
            ApplyDamage(col, stats.Damage * damageScale);
        }
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = new Color(1f, 0.4f, 0f);
        Gizmos.DrawWireSphere(transform.position, lashHitRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + transform.forward * lashTipOffset, lashTipRadius);
    }
}