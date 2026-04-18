using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class LasherReaverController : EnemyBase
{
    public enum LasherReaverForm { Lasher, Reaver }
    public enum ReaverAttackType { None, Dash, Charge }

    // Form Switch
    [Header("Form Switch")]
    [SerializeField] private float switchIntervalMin = 20f;
    [SerializeField] private float switchIntervalMax = 40f;
    [SerializeField] private GameObject switchVFXPrefab;
    [SerializeField] private float switchVFXDelay = 0.3f;

    // Shared
    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Form Scale")]
    [SerializeField] private Vector3 lasherScale = Vector3.one;
    [SerializeField] private Vector3 reaverScale = Vector3.one;

    // Lasher Form
    [Header("Lasher Attack")]
    [SerializeField] private float lasherAttackRange = 1.5f;
    [SerializeField] private float lasherCooldownMin = 1.5f;
    [SerializeField] private float lasherCooldownMax = 3f;

    [Header("Lasher Double Combo")]
    [SerializeField] private float comboDashSpeed = 7f;
    [SerializeField] private float comboDashDuration = 0.15f;
    [SerializeField] private float comboDashHitRadius = 0.6f;
    [SerializeField] private float comboDamageScale = 1f;
    [SerializeField] private float comboMaxRedirectAngle = 60f;
    [SerializeField] private float comboExtendMaxRedirectAngle = 60f;
    [SerializeField][Range(0f, 1f)] private float comboExtend3Chance = 0.5f;
    [SerializeField][Range(0f, 1f)] private float comboExtend4Chance = 0.5f;

    [Header("Lasher Hit4 Lash")]
    [SerializeField] private float lashHitOffset = 0.5f;
    [SerializeField] private float lashHitRadius = 1.2f;
    [SerializeField] private float lashDamageScale = 1.2f;
    [SerializeField] private float lashTipDelay = 0.25f;
    [SerializeField] private float lashTipOffset = 1.5f;
    [SerializeField] private int lashTipCount = 1;
    [SerializeField] private float lashTipRadius = 1f;
    [SerializeField] private float lashTipDamageScale = 0.8f;
    [SerializeField] private int lashLineCount = 1;
    [SerializeField] private float lashLineAngleStep = 30f;
    [SerializeField] private GameObject lashAoeVFX;
    [SerializeField] private float lashVFXBaseRadius = 1f;
    [SerializeField] private float lashVFXOffsetY = 0f;
    [SerializeField] private float lashVFXOffsetZMult = 0f;
    [SerializeField] private GameObject lashAoeFieldPrefab;
    [SerializeField] private float lashFieldFadeInDuration = 0.25f;
    [SerializeField] private float lashFieldStayDuration = 0.1f;
    [SerializeField] private float lashFieldFadeOutDuration = 0.2f;
    [SerializeField] private float combo2PostDelayMin = 0.4f;
    [SerializeField] private float combo2PostDelayMax = 0.8f;
    [SerializeField] private float combo3PostDelayMin = 0.4f;
    [SerializeField] private float combo3PostDelayMax = 0.8f;
    [SerializeField] private float combo4PostDelayMin = 0.4f;
    [SerializeField] private float combo4PostDelayMax = 0.8f;

    // Reaver Form
    [Header("Reaver Dash")]
    [SerializeField] private float reaverDashAttackRange = 1.5f;
    [SerializeField] private float reaverDashCooldownMin = 1.5f;
    [SerializeField] private float reaverDashCooldownMax = 3f;
    [SerializeField] private float reaverDashSpeed = 7f;
    [SerializeField] private float reaverDashDuration = 0.15f;
    [SerializeField] private float reaverDashHitRadius = 0.6f;
    [SerializeField] private float reaverDashDamageScale = 1f;
    [SerializeField] private float reaverDashPostDelayMin = 0.4f;
    [SerializeField] private float reaverDashPostDelayMax = 0.8f;

    [Header("Reaver Charge")]
    [SerializeField] private float chargeAttackRange = 5f;
    [SerializeField] private float reaverChargeCooldownMin = 3f;
    [SerializeField] private float reaverChargeCooldownMax = 6f;
    [SerializeField] private float chargeSpeed = 8f;
    [SerializeField] private float chargeWindUpDuration = 1f;
    [SerializeField] private float windUpRotateSpeed = 90f;
    [SerializeField] private float chargeHitRadius = 0.7f;
    [SerializeField] private float chargeDamageScale = 1.2f;
    [SerializeField] private float chargeRotateSpeed = 30f;
    [SerializeField] private float chargeWallDetectRange = 0.5f;
    [SerializeField] private float chargeStunnedDuration = 2f;
    [SerializeField] private float reaverChargePostDelayMin = 0.4f;
    [SerializeField] private float reaverChargePostDelayMax = 0.8f;

    [Header("Reaver Charge Redirect")]
    [SerializeField] private float missDetectAngle = 120f;
    [SerializeField] private float missDetectDuration = 0.1f;
    [SerializeField] private float redirectChance = 0.25f;
    [SerializeField] private float brakeDuration = 0.1f;
    [SerializeField] private float redirectWindUpDuration = 0.5f;
    [SerializeField] private int maxRedirectCount = 1;

    // Runtime State
    private LasherReaverForm currentForm;
    private bool isAttacking = false;
    private bool isSwitchingForm = false;
    private Vector3 lockedAttackDir = Vector3.zero;

    // Lasher state

    // Reaver state
    private ReaverAttackType currentReaverAttack = ReaverAttackType.None;
    private bool isDashing = false;
    private bool isCharging = false;
    private bool isStunned = false;
    private Vector3 chargeDir = Vector3.zero;
    private int currentRedirectCount = 0;
    private bool hasRolledRedirect = false;

    // Cooldowns (global, never reset on switch)
    private float lastLasherComboTime = -Mathf.Infinity;
    private float currentLasherCooldown = 0f;
    private float lastReaverDashTime = -Mathf.Infinity;
    private float currentReaverDashCooldown = 0f;
    private float lastReaverChargeTime = -Mathf.Infinity;
    private float currentReaverChargeCooldown = 0f;

    private enum AIState { Wander, Chase, Strafe, Attack }
    private AIState currentState = AIState.Wander;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);

        currentForm = Random.value < 0.5f ? LasherReaverForm.Lasher : LasherReaverForm.Reaver;

        currentLasherCooldown = Random.Range(lasherCooldownMin, lasherCooldownMax);
        currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
        currentReaverChargeCooldown = Random.Range(reaverChargeCooldownMin, reaverChargeCooldownMax);

        float attackRange = currentForm == LasherReaverForm.Lasher ? lasherAttackRange : reaverDashAttackRange;
        strafe.Init(attackRange);
        transform.localScale = currentForm == LasherReaverForm.Lasher ? lasherScale : reaverScale;
    }

    protected override void Start()
    {
        base.Start();
        if (currentForm == LasherReaverForm.Reaver)
            animator?.SetTrigger("ReaverBackToIdle");
        StartCoroutine(SwitchTimerRoutine());
    }

    public override bool CanBeInterrupted() => false;

    // State Machine
    protected override void UpdateState()
    {
        if (isAttacking || isSwitchingForm) { currentState = AIState.Attack; return; }

        if (!HasTarget)
        {
            if (currentState != AIState.Wander) wander.Reset(movement, stats);
            currentState = AIState.Wander;
            return;
        }

        if (currentState == AIState.Wander) wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canAttack = CanAttackNow(dist);
        float nearRange = currentForm == LasherReaverForm.Lasher ? lasherAttackRange : reaverDashAttackRange;

        if (canAttack)
            currentState = AIState.Attack;
        else if (dist <= nearRange)
            currentState = AIState.Strafe;
        else
        {
            if (currentState == AIState.Strafe) strafe.Reset();
            currentState = AIState.Chase;
        }
    }

    private bool CanAttackNow(float dist)
    {
        if (currentForm == LasherReaverForm.Lasher)
            return Time.time >= lastLasherComboTime + currentLasherCooldown && dist <= lasherAttackRange;

        bool canDash = Time.time >= lastReaverDashTime + currentReaverDashCooldown && dist <= reaverDashAttackRange;
        bool canCharge = Time.time >= lastReaverChargeTime + currentReaverChargeCooldown && dist <= chargeAttackRange;
        return canDash || canCharge;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case AIState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;

            case AIState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case AIState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;

            case AIState.Attack:
                if (isSwitchingForm) { movement.StopMoving(); return; }

                if (!isAttacking || lockedAttackDir == Vector3.zero)
                    movement.FaceTarget(TargetPosition);
                else
                    movement.FaceTarget(transform.position + lockedAttackDir);

                HandleAttackMovement();
                TryAttack();
                break;
        }
    }

    private void HandleAttackMovement()
    {
        if (currentForm == LasherReaverForm.Reaver)
        {
            if (currentReaverAttack == ReaverAttackType.Charge && isCharging && !isStunned)
                return;

            if (currentReaverAttack == ReaverAttackType.Charge && isStunned)
            {
                movement.StopMoving();
                return;
            }
        }

        if (!isAttacking)
            movement.MoveToTarget(TargetPosition);
        else if (currentForm == LasherReaverForm.Lasher && isHit4Phase && !isHit4Active)
            movement.MoveToTarget(TargetPosition);
        else
            movement.StopMoving();
    }

    // Try Attack
    private void TryAttack()
    {
        if (isAttacking) return;

        if (currentForm == LasherReaverForm.Lasher)
            TryLasherAttack();
        else
            TryReaverAttack();
    }

    private void TryLasherAttack()
    {
        if (Time.time < lastLasherComboTime + currentLasherCooldown) return;
        float dist = Vector3.Distance(transform.position, TargetPosition);
        if (dist > lasherAttackRange) return;

        isAttacking = true;
        animator?.SetTrigger("LasherAttack1Dash1");
    }

    private void TryReaverAttack()
    {
        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canDash = Time.time >= lastReaverDashTime + currentReaverDashCooldown && dist <= reaverDashAttackRange;
        bool canCharge = Time.time >= lastReaverChargeTime + currentReaverChargeCooldown && dist <= chargeAttackRange;

        if (!canDash && !canCharge) return;

        isAttacking = true;

        if (canDash && canCharge)
            currentReaverAttack = Random.value < 0.5f ? ReaverAttackType.Dash : ReaverAttackType.Charge;
        else if (canDash)
            currentReaverAttack = ReaverAttackType.Dash;
        else
            currentReaverAttack = ReaverAttackType.Charge;

        if (currentReaverAttack == ReaverAttackType.Dash)
            animator?.SetTrigger("ReaverAttack1");
        else
            animator?.SetTrigger("ChargeWindUp");
    }

    // Form Switch
    private IEnumerator SwitchTimerRoutine()
    {
        float interval = Random.Range(switchIntervalMin, switchIntervalMax);
        yield return new WaitForSeconds(interval);
        yield return new WaitWhile(() => isAttacking);
        yield return new WaitWhile(() => isPostAttackDelay);
        StartCoroutine(SwitchFormRoutine());
    }

    private IEnumerator SwitchFormRoutine()
    {
        isSwitchingForm = true;

        StopCoroutine(nameof(ChargeRoutine));
        StopCoroutine(nameof(BrakeAndRedirectRoutine));
        StopCoroutine(nameof(StunnedRoutine));
        StopCoroutine(nameof(WindUpRoutine));

        ResetAllAttackFlags();

        var agent = movement.GetAgent();
        if (agent != null && !agent.enabled) agent.enabled = true;

        movement.StopMoving();

        LasherReaverForm nextForm = currentForm == LasherReaverForm.Lasher
            ? LasherReaverForm.Reaver
            : LasherReaverForm.Lasher;

        if (switchVFXPrefab != null)
            Instantiate(switchVFXPrefab, transform.position, Quaternion.identity);

        yield return new WaitForSeconds(switchVFXDelay);

        currentForm = nextForm;
        string switchTrigger = nextForm == LasherReaverForm.Reaver ? "ReaverBackToIdle" : "LasherBackToIdle";
        animator?.SetTrigger(switchTrigger);
        float newRange = currentForm == LasherReaverForm.Lasher ? lasherAttackRange : reaverDashAttackRange;
        strafe.Init(newRange);
        transform.localScale = currentForm == LasherReaverForm.Lasher ? lasherScale : reaverScale;

        isSwitchingForm = false;

        StartCoroutine(SwitchTimerRoutine());
    }

    private void ResetAllAttackFlags()
    {
        isAttacking = false;
        pendingHit4 = false;
        lastComboHit = 2;
        isHit4Active = false;
        isHit4Phase = false;
        pendingHit4WasFromHit3 = false;
        lockedAttackDir = Vector3.zero;


        currentReaverAttack = ReaverAttackType.None;
        isDashing = false;
        isCharging = false;
        isStunned = false;
        chargeDir = Vector3.zero;
        currentRedirectCount = 0;
        hasRolledRedirect = false;

        if (animator != null) animator.speed = 1f;
        health.StopFlashBuildup();
        strafe.Reset();
    }

    // Animation Events Shared
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

    public void FinishAttack()
    {
        if (currentForm == LasherReaverForm.Lasher)
        {
            lastLasherComboTime = Time.time;
            currentLasherCooldown = Random.Range(lasherCooldownMin, lasherCooldownMax);
        }
        else
        {
            if (currentReaverAttack == ReaverAttackType.Dash)
            {
                lastReaverDashTime = Time.time;
                currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
            }
            currentReaverAttack = ReaverAttackType.None;
            isDashing = false;
        }

        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        strafe.Reset();
        if (currentForm == LasherReaverForm.Lasher)
        {
            // 4 combo = hit3 + hit4
            // 3 combo = hit3 only OR hit4 only
            // 2 combo = hit2 only
            if (lastComboHit == 4 && pendingHit4WasFromHit3)
            {
                postAttackDelayMin = combo4PostDelayMin;
                postAttackDelayMax = combo4PostDelayMax;
            }
            else if (lastComboHit == 3 || lastComboHit == 4)
            {
                postAttackDelayMin = combo3PostDelayMin;
                postAttackDelayMax = combo3PostDelayMax;
            }
            else
            {
                postAttackDelayMin = combo2PostDelayMin;
                postAttackDelayMax = combo2PostDelayMax;
            }
        }
        else
        {
            postAttackDelayMin = reaverDashPostDelayMin;
            postAttackDelayMax = reaverDashPostDelayMax;
        }
        if (currentForm == LasherReaverForm.Lasher)
            animator?.SetTrigger("LasherBackToIdle");
        else
            animator?.SetTrigger("ReaverBackToIdle");

        TriggerPostAttackDelay();
    }

    // Animation Events Lasher
    public void DashAttackFirst()
    {
        StartCoroutine(DashRoutine(lockedAttackDir, comboDashSpeed, comboDashDuration, comboDashHitRadius, comboDamageScale, null));
    }

    public void DashAttackSecond()
    {

        Vector3 newDir = TargetPosition - transform.position;
        newDir.y = 0f;
        if (newDir.sqrMagnitude > 0.001f) newDir.Normalize();

        float angle = Vector3.Angle(lockedAttackDir, newDir);
        Vector3 dash2Dir = angle <= comboMaxRedirectAngle
            ? newDir
            : Vector3.RotateTowards(lockedAttackDir, newDir, comboMaxRedirectAngle * Mathf.Deg2Rad, 0f);

        lockedAttackDir = dash2Dir;
        StartCoroutine(DashRoutine(dash2Dir, comboDashSpeed, comboDashDuration, comboDashHitRadius, comboDamageScale, null));
    }

    public void TryExtendCombo()
    {
        bool doHit3 = Random.value < comboExtend3Chance;
        bool doHit4 = Random.value < comboExtend4Chance;

        if (doHit3)
        {
            TriggerExtendCombo(doHit4);
        }
        else if (doHit4)
        {
            lastComboHit = 4;
            animator?.SetTrigger("LasherAttack1Lash");
        }
        else
        {
            lastComboHit = 2;
            FinishAttack();
        }
    }

    private bool pendingHit4 = false;
    private bool pendingHit4WasFromHit3 = false;
    private int lastComboHit = 2;
    private bool isHit4Active = false;
    private bool isHit4Phase = false;

    private void TriggerExtendCombo(bool doHit4)
    {
        if (doHit4)
            pendingHit4 = true;
    }

    public void DashAttackThird()
    {
        Vector3 newDir = TargetPosition - transform.position;
        newDir.y = 0f;
        if (newDir.sqrMagnitude > 0.001f) newDir.Normalize();

        float angle = Vector3.Angle(lockedAttackDir, newDir);
        Vector3 dash3Dir = angle <= comboExtendMaxRedirectAngle
            ? newDir
            : Vector3.RotateTowards(lockedAttackDir, newDir, comboExtendMaxRedirectAngle * Mathf.Deg2Rad, 0f);

        lockedAttackDir = dash3Dir;
        StartCoroutine(DashRoutine(dash3Dir, comboDashSpeed, comboDashDuration, comboDashHitRadius, comboDamageScale, null));
    }

    public void AfterHit3()
    {
        if (pendingHit4)
        {
            pendingHit4 = false;
            lastComboHit = 2;
            isHit4Active = false;
            lastComboHit = 4;
            animator?.SetTrigger("LasherAttack1Lash");
        }
        else
        {
            lastComboHit = 3;
            FinishAttack();
        }
    }

    public void Hit4Begin()
    {
        isHit4Phase = true;
    }

    public void Hit4StopMoving()
    {
        isHit4Active = true;
        movement.StopMoving();
        movement.SetCanMove(false);
    }

    public void Hit4LashHit()
    {
        lastComboHit = 4;
        StartCoroutine(Hit4LashHitRoutine());
    }

    private IEnumerator Hit4LashHitRoutine()
    {
        Vector3 baseForward = lockedAttackDir.sqrMagnitude > 0.001f ? lockedAttackDir : transform.forward;
        baseForward.y = 0f;
        if (baseForward.sqrMagnitude > 0.001f) baseForward.Normalize();

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        int count = Mathf.Max(1, lashLineCount);
        float startAngle = -((count - 1) * 0.5f) * lashLineAngleStep;

        for (int line = 0; line < count; line++)
        {
            float angle = startAngle + line * lashLineAngleStep;
            Vector3 forward = Quaternion.Euler(0f, angle, 0f) * baseForward;

            Vector3 currentPos = transform.position + forward * lashHitOffset;
            SpawnLashField(currentPos, lashHitRadius, stats.Damage * lashDamageScale, 0f, hitMask);

            currentPos = transform.position + forward * lashHitOffset;
            for (int i = 0; i < lashTipCount; i++)
            {
                currentPos += forward * lashTipOffset;
                SpawnLashField(currentPos, lashTipRadius, stats.Damage * lashTipDamageScale, lashTipDelay * (i + 1), hitMask);
            }
        }

        float totalDuration = lashTipDelay * lashTipCount + lashFieldFadeInDuration;
        yield return new WaitForSeconds(totalDuration);

        movement.SetCanMove(true);
    }

    private void SpawnLashField(Vector3 pos, float radius, float damage, float startDelay, LayerMask targetLayers)
    {
        if (lashAoeFieldPrefab == null) return;
        var go = Instantiate(lashAoeFieldPrefab, pos, Quaternion.identity);
        go.GetComponent<LashAoeField>()?.Initialize(radius, damage, startDelay, lashFieldFadeInDuration, lashFieldStayDuration, lashFieldFadeOutDuration, targetLayers, health, lashAoeVFX, lashVFXBaseRadius, lashVFXOffsetY, lashVFXOffsetZMult);
    }

    // Animation Events Reaver
    public void ReaverDashAttack()
    {
        if (isDashing) return;
        isDashing = true;
        StartCoroutine(DashRoutine(lockedAttackDir, reaverDashSpeed, reaverDashDuration, reaverDashHitRadius, reaverDashDamageScale, () => isDashing = false));
    }

    public void StartCharge()
    {
        StartCoroutine(WindUpRoutine());
    }

    public void FinishCharge()
    {
        lastReaverChargeTime = Time.time;
        currentReaverChargeCooldown = Random.Range(reaverChargeCooldownMin, reaverChargeCooldownMax);

        isAttacking = false;
        isCharging = false;
        isStunned = false;
        currentRedirectCount = 0;
        hasRolledRedirect = false;
        lockedAttackDir = Vector3.zero;
        chargeDir = Vector3.zero;
        currentReaverAttack = ReaverAttackType.None;
        strafe.Reset();
        postAttackDelayMin = reaverChargePostDelayMin;
        postAttackDelayMax = reaverChargePostDelayMax;
        TriggerPostAttackDelay();
    }

    // Reaver Charge Coroutines
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
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Reaver_ChargeWindUp"))
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

    private IEnumerator ChargeRoutine()
    {
        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));
        LayerMask wallMask = (1 << LayerMask.NameToLayer("Wall"))
                           | (1 << LayerMask.NameToLayer("Barrier"));

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
                else missTimer = 0f;
            }

            movement.FaceTarget(transform.position + chargeDir);
            yield return null;
        }

        if (agent != null && !agent.enabled) agent.enabled = true;
    }

    private IEnumerator BrakeAndRedirectRoutine()
    {
        LayerMask wallMask = (1 << LayerMask.NameToLayer("Wall")) | (1 << LayerMask.NameToLayer("Barrier"));
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
        lockedAttackDir = HasTarget ? (TargetPosition - transform.position).normalized : lockedAttackDir;

        animator?.ResetTrigger("ChargeStart");
        animator?.SetTrigger("ChargeRedirectWindUp");

        yield return null;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("Reaver_ChargeRedirectWindUp"))
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

        if (!isAttacking) { if (animator != null) animator.speed = 1f; yield break; }
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

    public override void OnDeath()
    {
        base.OnDeath();
        if (currentForm == LasherReaverForm.Lasher)
            animator?.SetTrigger("LasherDie");
        else
            animator?.SetTrigger("ReaverDie");
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
        Gizmos.DrawWireSphere(transform.position, lasherAttackRange);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, reaverDashAttackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chargeAttackRange);
    }
}