using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class SmashConfig
{
    public float riseHeight;
    public float riseDurationMin;
    public float riseDurationMax;
    public float stayDurationMin;
    public float stayDurationMax;
    public float fallDurationMin;
    public float fallDurationMax;
}

public class LasherReaverController : EnemyBase
{
    public enum LasherReaverForm { Lasher, Reaver }
    public enum ReaverAttackType { None, Dash, Charge, Projectile, Jump }

    // Form Switch
    [Header("Form")]
    [SerializeField][Range(0f, 1f)] private float lasherFormChance = 0.5f;

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
    [SerializeField][Range(0f, 1f)] private float comboReaverProjectileChance = 0.3f;

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

    [Header("Lasher Combo Post Delay")]
    [SerializeField] private float combo2PostDelayMin = 0.4f;
    [SerializeField] private float combo2PostDelayMax = 0.8f;
    [SerializeField] private float combo3PostDelayMin = 0.4f;
    [SerializeField] private float combo3PostDelayMax = 0.8f;
    [SerializeField] private float combo4PostDelayMin = 0.4f;
    [SerializeField] private float combo4PostDelayMax = 0.8f;
    [SerializeField] private float combo5PostDelayMin = 0.4f;
    [SerializeField] private float combo5PostDelayMax = 0.8f;

    [Header("Lasher Anchor")]
    [SerializeField] private GameObject anchorProjectilePrefab;
    [SerializeField] private float anchorAttackRange = 8f;
    [SerializeField] private float anchorMinRange = 2f;
    [SerializeField] private float anchorCooldownMin = 6f;
    [SerializeField] private float anchorCooldownMax = 10f;
    [SerializeField] private float anchorHitRadius = 0.4f;
    [SerializeField] private float anchorDamageScale = 1f;
    [SerializeField] private float anchorHitInterval = 0.3f;
    [SerializeField] private float anchorSpinSpeed = 1f;
    [SerializeField] private float anchorCurveWidth = 0.5f;
    [SerializeField] private float anchorProjectileSpeed = 8f;
    [SerializeField] private float anchorPostDelayMin = 0.8f;
    [SerializeField] private float anchorPostDelayMax = 1.2f;
    [Range(0f, 1f)]
    [SerializeField] private float anchorWeight = 0.4f;

    [Header("Lasher Smash Ritual")]
    [SerializeField] private Vector2 ritualPositionNormalized = new Vector2(0.5f, 0.6f);
    [SerializeField] private Vector2 cameraLockPositionNormalized = new Vector2(0.5f, 0.5f);
    [SerializeField] private SmashConfig[] smashConfigs = new SmashConfig[3];
    [SerializeField] private float smashLandPauseDuration = 0.5f;
    [SerializeField] private float smashShockwaveDamageScale = 1.5f;
    [SerializeField] private GameObject smashShockwavePrefab;
    [SerializeField] private float smashCooldownMin = 15f;
    [SerializeField] private float smashCooldownMax = 25f;
    [SerializeField] private float smashPostDelayMin = 1f;
    [SerializeField] private float smashPostDelayMax = 1.5f;
    [SerializeField][Range(0f, 1f)] private float smashWeight = 0.3f;

    [Header("Lasher Smash Ritual Camera")]
    [SerializeField] private float smashZoomOutAmount = 2f;
    [SerializeField] private float smashZoomDuration = 0.5f;

    [Header("Lasher Smash Ritual Lighting")]
    [SerializeField] private float ritualGlobalLightIntensity = 0.2f;
    [SerializeField] private float ritualSpotLightIntensity = 3f;
    [SerializeField] private float ritualSpotLightRange = 20f;
    [SerializeField] private float ritualSpotLightAngle = 60f;
    [SerializeField] private float ritualSpotLightInnerAngle = 30f;
    [SerializeField] private Color ritualSpotLightColor = Color.white;
    [SerializeField] private float lightTransitionDuration = 1.5f;

    [Header("Reaver Projectile")]
    [SerializeField] private GameObject reaverProjectilePrefab;
    [SerializeField] private float reaverProjectileAttackRange = 6f;
    [SerializeField] private float reaverProjectileMinRange = 2f;
    [SerializeField] private float reaverProjectileCooldownMin = 4f;
    [SerializeField] private float reaverProjectileCooldownMax = 8f;
    [SerializeField] private float reaverProjectileDamageScale = 1f;
    [Header("Reaver Dash Combo")]
    [SerializeField][Range(0f, 1f)] private float comboDashProjChance = 0.5f;
    [SerializeField][Range(0f, 1f)] private float comboDashAnchorChance = 0.3f;
    [SerializeField][Range(0f, 1f)] private float comboDashJumpChance = 0.3f;
    [SerializeField] private float reaverProjectilePostDelayMin = 0.4f;
    [SerializeField] private float reaverProjectilePostDelayMax = 0.8f;

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
    [SerializeField] private GameObject chargeWarningPrefab;
    [SerializeField] private float chargeWarningOffset = 1f;
    [SerializeField] private float chargeWarningOffsetZ = 0f;
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

    [Header("Reaver Jump Attack")]
    [SerializeField] private float jumpAttackRange = 6f;
    [SerializeField] private float jumpAttackMinRange = 2f;
    [SerializeField] private float jumpAttackCooldownMin = 8f;
    [SerializeField] private float jumpAttackCooldownMax = 14f;
    [SerializeField] private float jumpRiseHeight = 3f;
    [SerializeField] private float jumpRiseDuration = 0.5f;
    [SerializeField] private float jumpFallDuration = 0.3f;
    [SerializeField] private float jumpDamageScale = 1.5f;
    [SerializeField] private float jumpLandRadius = 1.5f;
    [SerializeField] private int jumpTipCount = 3;
    [SerializeField] private float jumpTipBaseDelay = 0.15f;
    [SerializeField] private float jumpTipBaseDistance = 1.5f;
    [SerializeField] private float jumpTipDistanceStep = 1f;
    [SerializeField] private float jumpTipBaseRadius = 0.8f;
    [SerializeField] private float jumpTipRadiusStep = 0.3f;
    [SerializeField] private float jumpTipDamageScale = 0.8f;
    [SerializeField] private GameObject jumpAoeVFX;
    [SerializeField] private float jumpVFXBaseRadius = 1f;
    [SerializeField] private float jumpVFXOffsetY = 0f;
    [SerializeField] private float jumpVFXOffsetZMult = 0f;
    [SerializeField] private GameObject jumpAoeFieldPrefab;
    [SerializeField] private float jumpFieldFadeInDuration = 0.25f;
    [SerializeField] private float jumpFieldStayDuration = 0.1f;
    [SerializeField] private float jumpFieldFadeOutDuration = 0.2f;
    [SerializeField] private float jumpTargetRandomOffset = 0.5f;
    [SerializeField] private float jumpLandTriggerHeight = 0.5f;
    [SerializeField] private float jumpLandFinishDelay = 0.5f;
    [SerializeField] private float jumpPostDelayMin = 0.5f;
    [SerializeField] private float jumpPostDelayMax = 1f;
    [SerializeField][Range(0f, 1f)] private float jumpWeight = 0.3f;
    [SerializeField][Range(0f, 1f)] private float jumpChanceAfterProjectile = 0.4f;

    // Runtime State
    private LasherReaverForm currentForm;
    private bool isAttacking = false;
    private bool isSwitchingForm = false;
    private Vector3 lockedAttackDir = Vector3.zero;

    // Lasher state

    // Reaver state
    private ReaverAttackType currentReaverAttack = ReaverAttackType.None;
    private bool isDashing = false;
    private bool isReaverProjectilePhase = false;
    private bool isComboAnchorPhase = false;
    private Queue<string> dashComboQueue = new Queue<string>();
    private bool isJumpAttackPhase = false;
    private Vector3 jumpTargetPosition = Vector3.zero;
    private float lastReaverJumpTime = 0f;
    private float currentReaverJumpCooldown = 0f;
    private float lastReaverProjectileTime = 0f;
    private float currentReaverProjectileCooldown = 0f;
    private bool isCharging = false;
    private bool isStunned = false;
    private Vector3 chargeDir = Vector3.zero;
    private int currentRedirectCount = 0;
    private bool hasRolledRedirect = false;

    // Cooldowns (global, never reset on switch)
    private float lastLasherComboTime = 0f;
    private float currentLasherCooldown = 0f;
    private float lastLasherSmashTime = 0f;
    private float currentLasherSmashCooldown = 0f;
    private bool isSmashRitual = false;

    private const string SmashRiseTrigger = "SmashRise";
    private const string SmashStayTrigger = "SmashStay";
    private const string SmashFallTrigger = "SmashFall";
    private const string SmashLandTrigger = "SmashLand";
    private float lastLasherAnchorTime = 0f;
    private float currentLasherAnchorCooldown = 0f;

    private Vector3 anchorTargetPosition = Vector3.zero;
    private LasherReaverAnchorProjectile activeAnchor = null;
    private float lastReaverDashTime = 0f;
    private float currentReaverDashCooldown = 0f;
    private float lastReaverChargeTime = 0f;
    private float currentReaverChargeCooldown = 0f;

    private enum AIState { Wander, Chase, Strafe, Attack }
    private AIState currentState = AIState.Wander;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);

        currentForm = Random.value < lasherFormChance ? LasherReaverForm.Lasher : LasherReaverForm.Reaver;

        currentLasherCooldown = Random.Range(lasherCooldownMin, lasherCooldownMax);
        currentLasherAnchorCooldown = Random.Range(anchorCooldownMin, anchorCooldownMax);
        currentLasherSmashCooldown = Random.Range(smashCooldownMin, smashCooldownMax);
        currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
        currentReaverChargeCooldown = Random.Range(reaverChargeCooldownMin, reaverChargeCooldownMax);
        currentReaverProjectileCooldown = Random.Range(reaverProjectileCooldownMin, reaverProjectileCooldownMax);
        currentReaverJumpCooldown = Random.Range(jumpAttackCooldownMin, jumpAttackCooldownMax);

        // Combo and dash ready immediately, anchor and charge start at half cooldown
        lastLasherComboTime = -currentLasherCooldown;
        lastReaverDashTime = -currentReaverDashCooldown;
        lastLasherAnchorTime = -currentLasherAnchorCooldown * 0.5f;
        lastReaverChargeTime = -currentReaverChargeCooldown * 0.5f;
        lastReaverProjectileTime = -currentReaverProjectileCooldown * 0.5f;
        lastReaverJumpTime = -currentReaverJumpCooldown * 0.5f;

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
        {
            bool canCombo = Time.time >= lastLasherComboTime + currentLasherCooldown && dist <= lasherAttackRange;
            bool canAnchor = Time.time >= lastLasherAnchorTime + currentLasherAnchorCooldown && dist <= anchorAttackRange;
            bool canSmash = Time.time >= lastLasherSmashTime + currentLasherSmashCooldown;
            return canCombo || canAnchor || canSmash;
        }

        bool canDash = Time.time >= lastReaverDashTime + currentReaverDashCooldown && dist <= reaverDashAttackRange;
        bool canCharge = Time.time >= lastReaverChargeTime + currentReaverChargeCooldown && dist <= chargeAttackRange;
        bool canProjectile = Time.time >= lastReaverProjectileTime + currentReaverProjectileCooldown && dist >= reaverProjectileMinRange && dist <= reaverProjectileAttackRange && reaverProjectilePrefab != null;
        bool canJump = Time.time >= lastReaverJumpTime + currentReaverJumpCooldown && dist >= jumpAttackMinRange && dist <= jumpAttackRange;
        return canDash || canCharge || canProjectile || canJump;
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
        if (isSmashRitual) return;

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
        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canCombo = Time.time >= lastLasherComboTime + currentLasherCooldown && dist <= lasherAttackRange;
        bool canAnchor = Time.time >= lastLasherAnchorTime + currentLasherAnchorCooldown && dist <= anchorAttackRange;
        bool canSmash = Time.time >= lastLasherSmashTime + currentLasherSmashCooldown;

        if (!canCombo && !canAnchor && !canSmash) return;

        isAttacking = true;

        // Build list of available attacks with weights
        float totalWeight = 0f;
        if (canCombo) totalWeight += 1f - anchorWeight - smashWeight;
        if (canAnchor) totalWeight += anchorWeight;
        if (canSmash) totalWeight += smashWeight;

        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        if (canSmash) { cumulative += smashWeight; if (roll < cumulative) { StartSmashRitual(); return; } }
        if (canAnchor) { cumulative += anchorWeight; if (roll < cumulative) { StartAnchorAttack(); return; } }
        if (canCombo) { StartComboAttack(); return; }

        // Fallback
        if (canCombo) StartComboAttack();
        else if (canAnchor) StartAnchorAttack();
        else StartSmashRitual();
    }

    private void StartComboAttack()
    {
        animator?.SetTrigger("LasherAttack1Dash1");
    }

    private void StartAnchorAttack()
    {
        animator?.SetTrigger("LasherAnchorThrow");
    }

    private void TryReaverAttack()
    {
        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canDash = Time.time >= lastReaverDashTime + currentReaverDashCooldown && dist <= reaverDashAttackRange;
        bool canCharge = Time.time >= lastReaverChargeTime + currentReaverChargeCooldown && dist <= chargeAttackRange;
        bool canProjectile = Time.time >= lastReaverProjectileTime + currentReaverProjectileCooldown && dist >= reaverProjectileMinRange && dist <= reaverProjectileAttackRange && reaverProjectilePrefab != null;
        bool canJump = Time.time >= lastReaverJumpTime + currentReaverJumpCooldown && dist >= jumpAttackMinRange && dist <= jumpAttackRange;

        if (!canDash && !canCharge && !canProjectile && !canJump) return;

        isAttacking = true;

        // Weighted random selection
        float totalW = 0f;
        if (canDash) totalW += 1f;
        if (canCharge) totalW += 1f;
        if (canProjectile) totalW += 1f;
        if (canJump) totalW += jumpWeight;

        float rollW = Random.value * totalW;
        float cumW = 0f;
        currentReaverAttack = ReaverAttackType.Dash;
        if (canDash) { cumW += 1f; if (rollW < cumW) { currentReaverAttack = ReaverAttackType.Dash; goto selected; } }
        if (canCharge) { cumW += 1f; if (rollW < cumW) { currentReaverAttack = ReaverAttackType.Charge; goto selected; } }
        if (canProjectile) { cumW += 1f; if (rollW < cumW) { currentReaverAttack = ReaverAttackType.Projectile; goto selected; } }
        if (canJump) { currentReaverAttack = ReaverAttackType.Jump; }
    selected:;

        if (currentReaverAttack == ReaverAttackType.Dash)
            animator?.SetTrigger("ReaverAttack1");
        else if (currentReaverAttack == ReaverAttackType.Charge)
            animator?.SetTrigger("ChargeWindUp");
        else if (currentReaverAttack == ReaverAttackType.Projectile)
            animator?.SetTrigger("ReaverProjectileAttack");
        else
        {
            animator?.SetTrigger("ReaverJumpRise");
        }
    }

    // Form Switch
    private IEnumerator SwitchTimerRoutine()
    {
        switchTimerRemain = Random.Range(switchIntervalMin, switchIntervalMax);
        while (switchTimerRemain > 0f)
        {
            switchTimerRemain -= Time.deltaTime;
            yield return null;
        }
        switchTimerRemain = 0f;
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
            Instantiate(switchVFXPrefab, transform.position + Vector3.forward * -0.1f, Quaternion.identity);

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
        isCrossFormCombo = false;
        hadCrossFormInCombo = false;
        comboHadDash3 = false;
        pendingReaverProjectile = false;
        pendingHit4AfterProjectile = false;
        anchorTargetPosition = Vector3.zero;
        isSmashRitual = false;
        lockedAttackDir = Vector3.zero;


        currentReaverAttack = ReaverAttackType.None;
        isDashing = false;
        isReaverProjectilePhase = false;
        isJumpAttackPhase = false;
        jumpTargetPosition = Vector3.zero;
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
                if (dashComboQueue.Count == 0)
                {
                    bool doProj = Random.value < comboDashProjChance && reaverProjectilePrefab != null;
                    bool doAnchor = Random.value < comboDashAnchorChance && anchorProjectilePrefab != null;
                    bool doJump = Random.value < comboDashJumpChance;

                    var actions = new List<string>();
                    if (doProj) actions.Add("Proj");
                    if (doJump) actions.Add("Jump");
                    if (doAnchor) actions.Insert(Random.Range(0, actions.Count + 1), "Anchor");

                    foreach (var a in actions)
                        dashComboQueue.Enqueue(a);
                }

                if (dashComboQueue.Count > 0)
                {
                    ExecuteNextDashCombo();
                    return;
                }

                lastReaverDashTime = Time.time;
                currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
            }
            currentReaverAttack = ReaverAttackType.None;
            isDashing = false;
            isReaverProjectilePhase = false;
        }

        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        strafe.Reset();
        if (currentForm == LasherReaverForm.Lasher)
        {
            // 4 combo = hit3 + hit4
            // 3 combo = hit3 only OR hit4 only
            // 2 combo = hit2 only
            if (lastComboHit == 4 && pendingHit4WasFromHit3 && hadCrossFormInCombo)
            {
                // 1-2-3-Proj-Lash
                postAttackDelayMin = combo5PostDelayMin;
                postAttackDelayMax = combo5PostDelayMax;
            }
            else if (lastComboHit == 4 && hadCrossFormInCombo)
            {
                // 1-2-Proj-Lash
                postAttackDelayMin = combo4PostDelayMin;
                postAttackDelayMax = combo4PostDelayMax;
            }
            else if (lastComboHit == 4 && pendingHit4WasFromHit3)
            {
                // 1-2-3-Lash
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
        bool doReaverProjectile = Random.value < comboReaverProjectileChance && reaverProjectilePrefab != null;
        bool doHit4 = Random.value < comboExtend4Chance;

        // Store what will happen after hit3
        pendingReaverProjectile = doReaverProjectile;
        pendingHit4AfterProjectile = doHit4 && doReaverProjectile;

        if (doHit3)
        {
            TriggerExtendCombo(doHit4 && !doReaverProjectile);
        }

        if (!doHit3 && doReaverProjectile)
        {
            StartCrossFormProjectile();
            return;
        }

        if (!doHit3 && !doReaverProjectile && doHit4)
        {
            lastComboHit = 4;
            animator?.SetTrigger("LasherAttack1Lash");
            return;
        }

        if (!doHit3 && !doReaverProjectile && !doHit4)
        {
            lastComboHit = 2;
            FinishAttack();
        }
    }

    private bool pendingHit4 = false;
    private bool pendingHit4WasFromHit3 = false;
    private int lastComboHit = 2;
    private bool isHit4Active = false;
    private bool pendingReaverProjectile = false;
    private bool pendingHit4AfterProjectile = false;
    private bool isHit4Phase = false;
    private bool isCrossFormCombo = false;
    private bool hadCrossFormInCombo = false;
    private bool comboHadDash3 = false;

    private void TriggerExtendCombo(bool doHit4)
    {
        if (doHit4)
            pendingHit4 = true;
    }

    public void DashAttackThird()
    {
        comboHadDash3 = true;
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

        if (pendingReaverProjectile)
        {
            pendingReaverProjectile = false;
            if (comboHadDash3)
                pendingHit4WasFromHit3 = true;
            StartCrossFormProjectile();
            return;
        }

        if (pendingHit4)
        {
            pendingHit4 = false;
            pendingHit4WasFromHit3 = true;
            lastComboHit = 4;
            animator?.SetTrigger("LasherAttack1Lash");
        }
        else
        {
            lastComboHit = 3;
            FinishAttack();
        }
    }

    private void StartCrossFormProjectile()
    {
        isCrossFormCombo = true;
        hadCrossFormInCombo = true;
        currentForm = LasherReaverForm.Reaver;
        transform.localScale = reaverScale;
        if (switchVFXPrefab != null)
            Instantiate(switchVFXPrefab, transform.position + Vector3.forward * -0.1f, Quaternion.identity);
        animator?.SetTrigger("ReaverProjectileAttack");
    }

    public void FinishCrossFormProjectile()
    {
        if (pendingHit4AfterProjectile)
        {
            pendingHit4AfterProjectile = false;
            // Switch back to Lasher
            currentForm = LasherReaverForm.Lasher;
            transform.localScale = lasherScale;
            if (switchVFXPrefab != null)
                Instantiate(switchVFXPrefab, transform.position + Vector3.forward * -0.1f, Quaternion.identity);
            lastComboHit = 4;
            animator?.SetTrigger("LasherAttack1Lash");
            return;
        }

        // End with Reaver form - half switch cooldown
        isCrossFormCombo = false;
        float newRange = reaverDashAttackRange;
        strafe.Init(newRange);

        // If remaining time is less than half interval, set it to half
        float halfInterval = Random.Range(switchIntervalMin, switchIntervalMax) * 0.5f;
        if (switchTimerRemain < halfInterval)
            switchTimerRemain = halfInterval;

        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        postAttackDelayMin = reaverProjectilePostDelayMin;
        postAttackDelayMax = reaverProjectilePostDelayMax;
        animator?.SetTrigger("ReaverBackToIdle");
        strafe.Reset();
        TriggerPostAttackDelay();
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
        // If cross-form combo, switch back to Lasher
        if (isCrossFormCombo)
        {
            isCrossFormCombo = false;
            currentForm = LasherReaverForm.Lasher;
            transform.localScale = lasherScale;
            strafe.Init(lasherAttackRange);
        }
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

    // Animation Events Lasher Anchor
    public void AnchorLockTarget()
    {
        if (!HasTarget)
        {
            anchorTargetPosition = transform.position + transform.forward * anchorMinRange;
            anchorTargetPosition.y = transform.position.y;
            return;
        }

        Vector3 dir = TargetPosition - transform.position;
        dir.y = 0f;
        float dist = Mathf.Clamp(dir.magnitude, anchorMinRange, anchorAttackRange);
        anchorTargetPosition = transform.position + dir.normalized * dist;
        anchorTargetPosition.y = transform.position.y;
        movement.FaceTarget(anchorTargetPosition);
    }

    public void AnchorThrow()
    {
        if (anchorProjectilePrefab == null) return;
        var go = Instantiate(anchorProjectilePrefab, transform.position, Quaternion.identity);
        var proj = go.GetComponent<LasherReaverAnchorProjectile>();
        if (proj != null)
        {
            activeAnchor = proj;
            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(anchorTargetPosition.x, 0f, anchorTargetPosition.z));
            float anchorDuration = dist / Mathf.Max(0.1f, anchorProjectileSpeed);
            proj.Initialize(transform.position, anchorTargetPosition, anchorDuration,
                anchorDamageScale, anchorHitRadius, anchorHitInterval, anchorSpinSpeed, anchorCurveWidth, health, OnAnchorReturned);
        }
    }

    private void OnAnchorReturned()
    {
        activeAnchor = null;
        if (isComboAnchorPhase)
            FinishComboAnchor();
        else
            FinishAnchorAttack();
    }

    public void FinishAnchorAttack()
    {
        if (isComboAnchorPhase) return;
        isAttacking = false;
        anchorTargetPosition = Vector3.zero;
        activeAnchor = null;
        lastLasherAnchorTime = Time.time;
        currentLasherAnchorCooldown = Random.Range(anchorCooldownMin, anchorCooldownMax);
        postAttackDelayMin = anchorPostDelayMin;
        postAttackDelayMax = anchorPostDelayMax;
        animator?.SetTrigger("LasherBackToIdle");
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // Lasher Smash Ritual
    private Light cachedGlobalLight;
    private Light ritualSpotLight;
    private float originalGlobalLightIntensity;

    private void StartSmashRitual()
    {
        isSmashRitual = true;
        health.StopFlashBuildup();
        if (animator != null) animator.speed = 1f;
        StartCoroutine(SmashRitualRoutine());
    }

    private Vector3 GetNormalizedRoomPosition(Vector2 normalized)
    {
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        Vector3 origin = transform.position;

        float left = 0f, right = 0f, back = 0f, forward = 0f;
        if (Physics.Raycast(origin, Vector3.left, out RaycastHit h, 100f, wallMask)) left = h.distance;
        if (Physics.Raycast(origin, Vector3.right, out h, 100f, wallMask)) right = h.distance;
        if (Physics.Raycast(origin, Vector3.back, out h, 100f, wallMask)) back = h.distance;
        if (Physics.Raycast(origin, Vector3.forward, out h, 100f, wallMask)) forward = h.distance;

        float x = Mathf.Lerp(origin.x - left, origin.x + right, normalized.x);
        float z = Mathf.Lerp(origin.z - back, origin.z + forward, normalized.y);
        return new Vector3(x, origin.y, z);
    }

    private Vector3 GetSmashRitualPosition()
    {
        Vector3 target = GetNormalizedRoomPosition(ritualPositionNormalized);
        if (UnityEngine.AI.NavMesh.SamplePosition(target, out UnityEngine.AI.NavMeshHit navHit, 2f, UnityEngine.AI.NavMesh.AllAreas))
            return navHit.position;
        return transform.position;
    }

    private IEnumerator SmashRitualRoutine()
    {
        // 1. Move to ritual position via NavMesh
        Vector3 ritualPos = GetSmashRitualPosition();
        movement.MoveToTarget(ritualPos);
        while (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z),
                                new Vector3(ritualPos.x, 0f, ritualPos.z)) > 0.3f)
            yield return null;

        movement.StopMoving();
        movement.SetCanMove(false);
        var smashAgent = movement.GetAgent();
        if (smashAgent != null && smashAgent.isOnNavMesh) smashAgent.enabled = false;

        // 2. Invincible + lights + zoom + camera lock
        health.IsInvincible = true;
        StartCoroutine(TransitionLights(true));
        if (CameraController.Instance != null)
        {
            Vector3 camLockPos = GetNormalizedRoomPosition(cameraLockPositionNormalized);
            StartCoroutine(CameraController.Instance.LockToPositionSmooth(camLockPos, smashZoomDuration));
            StartCoroutine(CameraController.Instance.ZoomOut(smashZoomOutAmount, smashZoomDuration));
        }

        // 3. Smash loop
        Vector3 groundPos = transform.position;

        for (int i = 0; i < smashConfigs.Length; i++)
        {
            var config = smashConfigs[i];
            float riseDuration = Random.Range(config.riseDurationMin, config.riseDurationMax);
            float stayDuration = Random.Range(config.stayDurationMin, config.stayDurationMax);
            float fallDuration = Random.Range(config.fallDurationMin, config.fallDurationMax);
            Vector3 risePos = groundPos + Vector3.up * config.riseHeight;

            // Rise
            animator?.SetTrigger(SmashRiseTrigger);

            float elapsed = 0f;
            while (elapsed < riseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / riseDuration);
                transform.position = Vector3.Lerp(groundPos, risePos, t);
                yield return null;
            }
            transform.position = risePos;

            // Stay
            animator?.SetTrigger(SmashStayTrigger);
            yield return new WaitForSeconds(stayDuration);

            // Fall
            animator?.SetTrigger(SmashFallTrigger);

            yield return null;
            while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsTag("SmashFall"))
                yield return null;

            if (animator != null)
            {
                AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
                if (info.length > 0f) animator.speed = info.length / fallDuration;
            }

            elapsed = 0f;
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                transform.position = Vector3.Lerp(risePos, groundPos, t);
                yield return null;
            }
            transform.position = groundPos;
            if (animator != null) animator.speed = 1f;

            // Land - shockwave + land animation
            SpawnSmashShockwave();
            animator?.SetTrigger(SmashLandTrigger);

            // Open invincible window for player to hit
            health.IsInvincible = false;
            yield return new WaitForSeconds(smashLandPauseDuration);

            // Close invincible again if not last smash
            if (i < smashConfigs.Length - 1)
                health.IsInvincible = true;
        }

        // 4. End
        StartCoroutine(TransitionLights(false));
        if (CameraController.Instance != null)
        {
            StartCoroutine(CameraController.Instance.UnlockTargetSmooth(smashZoomDuration));
            StartCoroutine(CameraController.Instance.ZoomRestore(smashZoomDuration));
        }
        if (smashAgent != null && !smashAgent.enabled) smashAgent.enabled = true;
        movement.SetCanMove(true);
        isSmashRitual = false;
        lastLasherSmashTime = Time.time;
        currentLasherSmashCooldown = Random.Range(smashCooldownMin, smashCooldownMax);
        isAttacking = false;
        postAttackDelayMin = smashPostDelayMin;
        postAttackDelayMax = smashPostDelayMax;
        animator?.SetTrigger("LasherBackToIdle");
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    private void SpawnSmashShockwave()
    {
        if (smashShockwavePrefab == null) return;
        var go = Instantiate(smashShockwavePrefab, transform.position, Quaternion.identity);
        go.GetComponent<Shockwave>()?.Init(stats.Damage * smashShockwaveDamageScale, health);
    }

    private void CacheGlobalLight()
    {
        if (cachedGlobalLight != null) return;
        var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                cachedGlobalLight = l;
                originalGlobalLightIntensity = l.intensity;
                break;
            }
        }
    }

    private Light SpawnRitualSpotLight()
    {
        var go = new GameObject("SmashRitualSpotLight");
        go.transform.position = transform.position + Vector3.up * 10f;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = ritualSpotLightColor;
        light.range = ritualSpotLightRange;
        light.spotAngle = ritualSpotLightAngle;
        light.innerSpotAngle = ritualSpotLightInnerAngle;
        light.intensity = 0f;
        return light;
    }

    private IEnumerator TransitionLights(bool ritualOn)
    {
        CacheGlobalLight();

        if (ritualOn)
            ritualSpotLight = SpawnRitualSpotLight();

        float targetGlobal = ritualOn ? ritualGlobalLightIntensity : originalGlobalLightIntensity;
        float targetSpot = ritualOn ? ritualSpotLightIntensity : 0f;
        float startGlobal = cachedGlobalLight != null ? cachedGlobalLight.intensity : 0f;
        float startSpot = ritualSpotLight != null ? ritualSpotLight.intensity : 0f;

        float elapsed = 0f;
        while (elapsed < lightTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lightTransitionDuration);
            if (cachedGlobalLight != null) cachedGlobalLight.intensity = Mathf.Lerp(startGlobal, targetGlobal, t);
            if (ritualSpotLight != null) ritualSpotLight.intensity = Mathf.Lerp(startSpot, targetSpot, t);
            yield return null;
        }

        if (!ritualOn && ritualSpotLight != null)
        {
            UnityEngine.Object.Destroy(ritualSpotLight.gameObject);
            ritualSpotLight = null;
        }
    }

    private void ExecuteNextDashCombo()
    {
        if (dashComboQueue.Count == 0)
        {
            lastReaverDashTime = Time.time;
            currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
            currentReaverAttack = ReaverAttackType.None;
            isDashing = false;
            isReaverProjectilePhase = false;
            isJumpAttackPhase = false;

            // Clamp switch timer if had anchor in combo
            if (isComboAnchorPhase)
            {
                float halfInterval = Random.Range(switchIntervalMin, switchIntervalMax) * 0.5f;
                if (switchTimerRemain < halfInterval)
                    switchTimerRemain = halfInterval;
            }
            isComboAnchorPhase = false;

            isAttacking = false;
            lockedAttackDir = Vector3.zero;
            postAttackDelayMin = reaverDashPostDelayMin;
            postAttackDelayMax = reaverDashPostDelayMax;
            animator?.SetTrigger("ReaverBackToIdle");
            strafe.Reset();
            TriggerPostAttackDelay();
            return;
        }

        string next = dashComboQueue.Dequeue();
        if (next == "Proj")
        {
            isReaverProjectilePhase = true;
            animator?.SetTrigger("ReaverProjectileAttack");
        }
        else if (next == "Anchor")
        {
            isComboAnchorPhase = true;
            StartCoroutine(ComboAnchorRoutine());
        }
        else if (next == "Jump")
        {
            isJumpAttackPhase = true;
            animator?.SetTrigger("ReaverJumpRise");
        }
    }

    private IEnumerator ComboAnchorRoutine()
    {
        // Switch to Lasher
        currentForm = LasherReaverForm.Lasher;
        transform.localScale = lasherScale;
        animator?.ResetTrigger("ChargeWindUp");
        animator?.ResetTrigger("ChargeStart");
        animator?.SetTrigger("ReaverBackToIdle");
        if (switchVFXPrefab != null)
            Instantiate(switchVFXPrefab, transform.position + Vector3.forward * -0.1f, Quaternion.identity);

        // Wait for animator to leave JumpFall/JumpRise state
        yield return null;
        float waited = 0f;
        while (animator != null &&
               (animator.GetCurrentAnimatorStateInfo(0).IsName("Reaver_JumpAttack_Fall") ||
                animator.GetCurrentAnimatorStateInfo(0).IsName("Reaver_JumpAttack_Rise")) &&
               waited < 1f)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(switchVFXDelay);

        animator?.SetTrigger("LasherBackToIdle");
        yield return null;

        // Lock target and throw anchor
        AnchorLockTarget();
        animator?.SetTrigger("LasherAnchorThrow");
    }

    private void FinishComboAnchor()
    {
        lastLasherAnchorTime = Time.time;
        currentLasherAnchorCooldown = Random.Range(anchorCooldownMin, anchorCooldownMax);

        if (dashComboQueue.Count > 0)
        {
            // More actions remain — switch back to Reaver
            currentForm = LasherReaverForm.Reaver;
            transform.localScale = reaverScale;
            animator?.SetTrigger("ReaverBackToIdle");
            if (switchVFXPrefab != null)
                Instantiate(switchVFXPrefab, transform.position + Vector3.forward * -0.1f, Quaternion.identity);
            strafe.Init(reaverDashAttackRange);
            ExecuteNextDashCombo();
        }
        else
        {
            // Anchor was last — stay in Lasher form, half switch timer
            float halfInterval = Random.Range(switchIntervalMin, switchIntervalMax) * 0.5f;
            if (switchTimerRemain < halfInterval)
                switchTimerRemain = halfInterval;
            isComboAnchorPhase = false;

            lastReaverDashTime = Time.time;
            currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
            currentReaverAttack = ReaverAttackType.None;
            isDashing = false;
            isAttacking = false;
            lockedAttackDir = Vector3.zero;
            postAttackDelayMin = reaverDashPostDelayMin;
            postAttackDelayMax = reaverDashPostDelayMax;
            animator?.SetTrigger("LasherBackToIdle");
            strafe.Reset();
            TriggerPostAttackDelay();
        }
    }

    // Animation Events Reaver Jump
    private Coroutine jumpArcCoroutine = null;

    public void JumpStartArc()
    {
        jumpArcCoroutine = StartCoroutine(JumpArcRoutine());
    }

    private IEnumerator JumpArcRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = jumpTargetPosition;
        float totalDuration = jumpRiseDuration + jumpFallDuration;

        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        bool landTriggered = false;
        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / totalDuration);
            Vector3 flatPos = Vector3.Lerp(startPos, targetPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * jumpRiseHeight;
            transform.position = new Vector3(flatPos.x, startPos.y + height, flatPos.z);

            // Face movement direction
            Vector3 moveDir = targetPos - startPos;
            moveDir.y = 0f;
            if (moveDir.sqrMagnitude > 0.001f)
                movement.FaceTarget(transform.position + moveDir);

            // Trigger fall at midpoint
            if (elapsed >= jumpRiseDuration && elapsed - Time.deltaTime < jumpRiseDuration)
                animator?.SetTrigger("ReaverJumpFall");

            // Trigger JumpLand when close to ground on descent
            if (!landTriggered && elapsed > jumpRiseDuration && height <= jumpLandTriggerHeight)
            {
                landTriggered = true;
                JumpLand();
            }

            yield return null;
        }

        transform.position = new Vector3(targetPos.x, startPos.y, targetPos.z);
        if (agent != null) agent.enabled = true;

        // If combo queue active, continue immediately after land
        if (currentReaverAttack == ReaverAttackType.Dash && dashComboQueue.Count > 0)
        {
            FinishJumpAttack();
            yield break;
        }

        // Wait then finish
        yield return new WaitForSeconds(jumpLandFinishDelay);
        FinishJumpAttack();
    }

    public void JumpLockTarget()
    {
        Vector3 target = HasTarget ? TargetPosition : transform.position + transform.forward * 2f;
        // Add small random offset to avoid landing exactly on player
        Vector2 offset = Random.insideUnitCircle * jumpTargetRandomOffset;
        target += new Vector3(offset.x, 0f, offset.y);
        jumpTargetPosition = target;
        jumpTargetPosition.y = transform.position.y;
    }

    public void JumpLand()
    {
        StartCoroutine(JumpLandRoutine());
    }

    private IEnumerator JumpLandRoutine()
    {
        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        Vector3 landPos = new Vector3(jumpTargetPosition.x, transform.position.y, jumpTargetPosition.z);

        // Land AOE (center)
        if (jumpAoeFieldPrefab != null)
        {
            var go = Instantiate(jumpAoeFieldPrefab, landPos, Quaternion.identity);
            go.GetComponent<LashAoeField>()?.Initialize(jumpLandRadius, stats.Damage * jumpDamageScale, 0f, jumpFieldFadeInDuration, jumpFieldStayDuration, jumpFieldFadeOutDuration, hitMask, health, jumpAoeVFX, jumpVFXBaseRadius, jumpVFXOffsetY, jumpVFXOffsetZMult);
        }

        // Tip rings
        float currentDist = 0f;
        float currentStep = jumpTipBaseDistance;
        for (int i = 0; i < jumpTipCount; i++)
        {
            currentDist += currentStep;
            currentStep += jumpTipDistanceStep;
            float delay = jumpTipBaseDelay * (i + 1);
            float radius = jumpTipBaseRadius + jumpTipRadiusStep * i;
            float damage = stats.Damage * jumpTipDamageScale;
            SpawnJumpTipRing(landPos, currentDist, radius, damage, delay, hitMask);
        }

        float totalDuration = jumpTipBaseDelay * jumpTipCount + jumpFieldFadeInDuration;
        yield return new WaitForSeconds(totalDuration);
    }

    private void SpawnJumpTipRing(Vector3 center, float distance, float radius, float damage, float startDelay, LayerMask hitMask)
    {
        if (jumpAoeFieldPrefab == null) return;
        int count = Mathf.Max(6, Mathf.RoundToInt(2f * Mathf.PI * distance / (radius * 2f)));
        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
            var go = Instantiate(jumpAoeFieldPrefab, pos, Quaternion.identity);
            go.GetComponent<LashAoeField>()?.Initialize(radius, damage, startDelay, jumpFieldFadeInDuration, jumpFieldStayDuration, jumpFieldFadeOutDuration, hitMask, health, jumpAoeVFX, jumpVFXBaseRadius, jumpVFXOffsetY, jumpVFXOffsetZMult);
        }
    }

    public void FinishJumpAttack()
    {
        lastReaverJumpTime = Time.time;
        currentReaverJumpCooldown = Random.Range(jumpAttackCooldownMin, jumpAttackCooldownMax);
        isJumpAttackPhase = false;
        jumpTargetPosition = Vector3.zero;

        // Continue dash combo queue if active
        if (currentReaverAttack == ReaverAttackType.Dash && dashComboQueue.Count > 0)
        {
            animator?.SetTrigger("ReaverBackToIdle");
            ExecuteNextDashCombo();
            return;
        }

        currentReaverAttack = ReaverAttackType.None;
        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        postAttackDelayMin = jumpPostDelayMin;
        postAttackDelayMax = jumpPostDelayMax;
        animator?.SetTrigger("ReaverBackToIdle");
        strafe.Reset();
        TriggerPostAttackDelay();
    }

    // Animation Events Reaver
    public void FireReaverProjectile()
    {
        if (reaverProjectilePrefab == null) return;

        Vector3 spawnPos = transform.position + Vector3.up * 0.5f;
        Vector3 dir = lockedAttackDir.sqrMagnitude > 0.001f ? lockedAttackDir : transform.forward;
        Vector3 target = spawnPos + dir * 20f;

        var go = Instantiate(reaverProjectilePrefab, spawnPos, Quaternion.identity);
        go.GetComponent<ReaverProjectile>()?.Initialize(target, stats.Damage * reaverProjectileDamageScale, health);
    }

    public void FinishReaverProjectile()
    {
        if (isCrossFormCombo)
        {
            FinishCrossFormProjectile();
            return;
        }

        // Continue dash combo queue if active
        if (currentReaverAttack == ReaverAttackType.Dash && dashComboQueue.Count > 0)
        {
            isReaverProjectilePhase = false;
            lastReaverProjectileTime = Time.time - currentReaverProjectileCooldown * 0.5f;
            ExecuteNextDashCombo();
            return;
        }

        float jumpChance = jumpChanceAfterProjectile;
        if (!isJumpAttackPhase && Random.value < jumpChance)
        {
            if (isReaverProjectilePhase)
            {
                lastReaverDashTime = Time.time;
                currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
            }
            currentReaverProjectileCooldown = Random.Range(reaverProjectileCooldownMin, reaverProjectileCooldownMax);
            lastReaverProjectileTime = Time.time;
            currentReaverAttack = ReaverAttackType.None;
            isDashing = false;
            isReaverProjectilePhase = false;
            isJumpAttackPhase = true;
            animator?.SetTrigger("ReaverJumpRise");
            return;
        }

        if (isReaverProjectilePhase)
        {
            lastReaverDashTime = Time.time;
            currentReaverDashCooldown = Random.Range(reaverDashCooldownMin, reaverDashCooldownMax);
        }

        currentReaverProjectileCooldown = Random.Range(reaverProjectileCooldownMin, reaverProjectileCooldownMax);
        lastReaverProjectileTime = isReaverProjectilePhase
            ? Time.time - currentReaverProjectileCooldown * 0.5f
            : Time.time;
        currentReaverAttack = ReaverAttackType.None;
        isDashing = false;
        isReaverProjectilePhase = false;
        isJumpAttackPhase = false;
        isAttacking = false;
        lockedAttackDir = Vector3.zero;
        postAttackDelayMin = reaverProjectilePostDelayMin;
        postAttackDelayMax = reaverProjectilePostDelayMax;
        animator?.SetTrigger("ReaverBackToIdle");
        strafe.Reset();
        TriggerPostAttackDelay();
    }

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
        currentReaverProjectileCooldown = Random.Range(reaverProjectileCooldownMin, reaverProjectileCooldownMax);
        currentReaverJumpCooldown = Random.Range(jumpAttackCooldownMin, jumpAttackCooldownMax);

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

        GameObject warning = null;
        if (chargeWarningPrefab != null)
        {
            Vector3 wPos = transform.position + lockedAttackDir * chargeWarningOffset;
            wPos.z -= chargeWarningOffsetZ;
            warning = Instantiate(chargeWarningPrefab, wPos, Quaternion.LookRotation(lockedAttackDir, Vector3.up));
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

            if (warning != null)
            {
                Vector3 wPos = transform.position + lockedAttackDir * chargeWarningOffset;
                wPos.z -= chargeWarningOffsetZ;
                warning.transform.position = wPos;
                warning.transform.rotation = Quaternion.LookRotation(lockedAttackDir, Vector3.up);
            }

            yield return null;
        }

        if (warning != null) Destroy(warning);

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

    private float switchTimerRemain = 0f;

    private void OnGUI()
    {
        float dashRemain = Mathf.Max(0f, (lastReaverDashTime + currentReaverDashCooldown) - Time.time);
        float chargeRemain = Mathf.Max(0f, (lastReaverChargeTime + currentReaverChargeCooldown) - Time.time);
        float projectileRemain = Mathf.Max(0f, (lastReaverProjectileTime + currentReaverProjectileCooldown) - Time.time);
        float jumpRemain = Mathf.Max(0f, (lastReaverJumpTime + currentReaverJumpCooldown) - Time.time);

        GUILayout.BeginArea(new Rect(10, 10, 280, 160));
        GUILayout.Label($"Form: {currentForm}");
        GUILayout.Label($"Switch Cooldown: {switchTimerRemain:F1}s");
        if (currentForm == LasherReaverForm.Reaver)
        {
            GUILayout.Label($"[Reaver] Dash:       {dashRemain:F1}s");
            GUILayout.Label($"[Reaver] Charge:     {chargeRemain:F1}s");
            GUILayout.Label($"[Reaver] Projectile: {projectileRemain:F1}s");
            GUILayout.Label($"[Reaver] Jump:       {jumpRemain:F1}s");
        }
        GUILayout.EndArea();
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, anchorHitRadius);
        Gizmos.color = new Color(1f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, anchorAttackRange);
        Gizmos.color = new Color(0f, 1f, 1f);
        Gizmos.DrawWireSphere(transform.position, jumpAttackRange);
        Gizmos.color = new Color(0f, 0.5f, 1f);
        Gizmos.DrawWireSphere(transform.position, jumpAttackMinRange);
    }
}