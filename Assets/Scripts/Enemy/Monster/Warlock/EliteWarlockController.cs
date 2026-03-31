using System.Collections;
using UnityEngine;

public class EliteWarlockController : WarlockController
{
    // ?? AoE Smash ??????????????????????????????????????????????
    [Header("AoE Smash")]
    [SerializeField] private float smashRange = 5f;
    [SerializeField] private float smashDamageScale = 1.2f;
    [SerializeField] private float smashWarningDuration = 1f;
    [SerializeField] private float smashAoeRadius = 2.5f;
    [SerializeField] private float smashCooldown = 5f;
    [SerializeField] private float smashWindUpDuration = 0.8f;
    [SerializeField] private GameObject aoeWarningPrefab;
    [SerializeField] private LayerMask targetLayers;

    // ?? Jump ???????????????????????????????????????????????????
    [Header("Jump")]
    [SerializeField] private float jumpRepositionCooldown = 4f;
    [SerializeField] private float jumpEscapeCooldown = 8f;
    [SerializeField] private float jumpRadius = 6f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float jumpPeakHeight = 2f;
    [SerializeField] private float jumpCandidates = 8;

    // ?? Ultimate ???????????????????????????????????????????????
    [Header("Ultimate")]
    [SerializeField] private float ultimateDamageScale = 1.5f;
    [SerializeField] private float ultimateWarningDuration = 1.5f;
    [SerializeField] private float ultimateAoeRadius = 2f;
    [SerializeField] private float ultimateCooldown = 12f;
    [SerializeField] private int ultimateCircleCountMin = 4;
    [SerializeField] private int ultimateCircleCountMax = 7;
    [SerializeField] private float ultimateSpawnRadius = 8f;
    [SerializeField] private float ultimateWindUpDuration = 2f;

    // ?? State ??????????????????????????????????????????????????
    private EliteWarlockHealthBase eliteHealth;
    private bool isEnraged;

    private float lastSmashTime = -Mathf.Infinity;
    private float lastJumpRepositionTime = -Mathf.Infinity;
    private float lastJumpEscapeTime = -Mathf.Infinity;
    private float lastUltimateTime = -Mathf.Infinity;

    private bool isJumping;
    private bool isSmashing;
    private bool isCastingUltimate;

    protected override void Awake()
    {
        base.Awake();
        eliteHealth = GetComponent<EliteWarlockHealthBase>();
        if (eliteHealth != null)
            eliteHealth.OnEnrage += OnEnrage;
    }

    private void OnEnrage()
    {
        isEnraged = true;
    }

    // ?? Override UpdateState ???????????????????????????????????
    protected override void UpdateState()
    {
        if (isJumping || isSmashing || isCastingUltimate || isWindingUp) return;

        if (!HasTarget)
        {
            if (currentState != WarlockState.Wander)
                wander.Reset(movement);
            currentState = WarlockState.Wander;
            return;
        }

        if (currentState == WarlockState.Wander)
            wander.Reset(movement);

        // Escape jump — interrupt anything
        float dist = Vector3.Distance(transform.position, TargetPosition);
        if (dist < minRange && CanEscapeJump())
        {
            StartCoroutine(JumpRoutine());
            return;
        }

        float distCheck = Vector3.Distance(transform.position, TargetPosition);
        currentState = distCheck <= shootRange ? WarlockState.WindUp : WarlockState.Chase;
    }

    // ?? Override TickState ?????????????????????????????????????
    protected override void TickState()
    {
        if (isJumping || isSmashing || isCastingUltimate) return;

        // Try special skills before normal shoot
        if (currentState == WarlockState.WindUp)
        {
            if (isEnraged && TryUltimate()) return;
            if (TrySmash()) return;
        }

        // Reposition jump (cooldown-based)
        if (HasTarget && CanRepositionJump() && !isWindingUp)
        {
            StartCoroutine(JumpRoutine());
            return;
        }

        base.TickState();
    }

    // ?? Smash ??????????????????????????????????????????????????
    private bool TrySmash()
    {
        if (isSmashing) return true;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        float dist = Vector3.Distance(transform.position, TargetPosition);
        if (dist > smashRange) return false;

        StartCoroutine(SmashRoutine());
        return true;
    }

    private IEnumerator SmashRoutine()
    {
        isSmashing = true;
        movement.StopMoving();

        // Wind-up animation (no loop)
        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(smashWindUpDuration);

        // Spawn warning — counts down visually, deals damage when done
        if (aoeWarningPrefab != null)
        {
            Vector3 pos = new Vector3(TargetPosition.x, transform.position.y, TargetPosition.z);
            var go = Instantiate(aoeWarningPrefab, pos, Quaternion.identity);
            var warning = go.GetComponent<WarlockAOEWarning>();
            warning?.Initialize(stats.Damage * smashDamageScale, smashAoeRadius, smashWarningDuration, targetLayers);
        }

        // Wait for warning countdown, then smash
        yield return new WaitForSeconds(smashWarningDuration);
        animator?.SetTrigger("Smash");
        lastSmashTime = Time.time;
        isSmashing = false;
        TriggerPostAttackDelay();
    }

    // ?? Jump ???????????????????????????????????????????????????
    private bool CanEscapeJump()
    {
        return !isJumping && Time.time >= lastJumpEscapeTime + jumpEscapeCooldown;
    }

    private bool CanRepositionJump()
    {
        return !isJumping && Time.time >= lastJumpRepositionTime + jumpRepositionCooldown;
    }

    private Vector3 FindJumpDestination()
    {
        Vector3 best = transform.position;
        float bestDist = -1f;

        for (int i = 0; i < jumpCandidates; i++)
        {
            float angle = i * (360f / jumpCandidates);
            Vector3 candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * jumpRadius;

            // Prefer far from target
            float d = Vector3.Distance(candidate, TargetPosition);
            if (d > bestDist)
            {
                bestDist = d;
                best = candidate;
            }
        }

        return best;
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        bool isEscape = Vector3.Distance(transform.position, TargetPosition) < minRange;

        movement.SetCanMove(false);
        animator?.SetTrigger("Jump");

        // Wait 1 frame so animator transitions to Jump state
        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f)
                animator.speed = info.length / jumpDuration;
        }

        Vector3 destination = FindJumpDestination();
        Vector3 startPos = transform.position;
        float peakHeight = jumpPeakHeight;

        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float height = Mathf.Sin(t * Mathf.PI) * peakHeight;
            transform.position = Vector3.Lerp(startPos, destination, t) + Vector3.up * height;
            yield return null;
        }

        transform.position = new Vector3(destination.x, startPos.y, destination.z);
        if (animator != null) animator.speed = 1f;
        movement.SetCanMove(true);

        if (isEscape)
            lastJumpEscapeTime = Time.time;
        else
            lastJumpRepositionTime = Time.time;

        isJumping = false;
    }

    // ?? Ultimate ???????????????????????????????????????????????
    private bool TryUltimate()
    {
        if (isCastingUltimate) return true;
        if (Time.time < lastUltimateTime + ultimateCooldown) return false;

        StartCoroutine(UltimateRoutine());
        return true;
    }

    private IEnumerator UltimateRoutine()
    {
        isCastingUltimate = true;
        movement.StopMoving();

        // Wind-up animation (no loop) — longer than smash
        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(ultimateWindUpDuration);

        // Spawn all warning circles — each counts down and deals damage independently
        int circleCount = Random.Range(ultimateCircleCountMin, ultimateCircleCountMax + 1);
        for (int i = 0; i < circleCount; i++)
        {
            Vector2 rand = Random.insideUnitCircle * ultimateSpawnRadius;
            Vector3 pos = transform.position + new Vector3(rand.x, 0f, rand.y);

            if (aoeWarningPrefab != null)
            {
                var go = Instantiate(aoeWarningPrefab, pos, Quaternion.identity);
                var warning = go.GetComponent<WarlockAOEWarning>();
                warning?.Initialize(stats.Damage * ultimateDamageScale, ultimateAoeRadius, ultimateWarningDuration, targetLayers);
            }
        }

        // Wait for warning countdown, then smash
        yield return new WaitForSeconds(ultimateWarningDuration);
        animator?.SetTrigger("Smash");
        lastUltimateTime = Time.time;
        isCastingUltimate = false;
        TriggerPostAttackDelay();
    }

    // Elite uses same Animation Event pattern — 3 events in clip
    // FireProjectile x2, FireLastProjectile x1 (last shot handles cooldown)

    public override void OnDeath()
    {
        base.OnDeath();
        StopAllCoroutines();
        isJumping = false;
        isSmashing = false;
        isCastingUltimate = false;
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smashRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpRadius);
    }
}