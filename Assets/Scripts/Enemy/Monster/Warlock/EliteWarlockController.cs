using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EliteWarlockController : WarlockController
{
    // AoE Smash
    [Header("AoE Smash")]
    [SerializeField] private float smashRange = 5f;
    [SerializeField] private float smashDamageScale = 1.2f;
    [SerializeField] private float smashWindUpDuration = 0.8f;
    [SerializeField] private float smashWarningDuration = 1f;
    [SerializeField] private float smashAoeRadius = 2.5f;
    [SerializeField] private float smashCooldown = 5f;
    [SerializeField] private GameObject aoeWarningPrefab;
    [SerializeField] private LayerMask targetLayers;

    // Jump
    [Header("Jump")]
    [SerializeField] private float jumpTriggerRange = 2f;
    [SerializeField] private float jumpEscapeCooldown = 8f;
    [SerializeField] private float jumpRadius = 6f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float jumpPeakHeight = 2f;
    [SerializeField] private int jumpCandidates = 8;

    // Ultimate
    [Header("Ultimate")]
    [SerializeField] private float ultimateDamageScale = 1.5f;
    [SerializeField] private float ultimateWindUpDuration = 2f;
    [SerializeField] private float ultimateWarningDuration = 1.5f;
    [SerializeField] private float ultimateAoeRadius = 2f;
    [SerializeField] private float ultimateCooldown = 12f;
    [SerializeField] private int ultimateCircleCountMin = 4;
    [SerializeField] private int ultimateCircleCountMax = 7;
    [SerializeField] private float ultimateSpawnRadius = 8f;
    [SerializeField] private float ultimateMinSpawnDistance = 1f;

    private EliteWarlockHealthBase eliteHealth;
    private bool isEnraged;
    private bool isJumping;
    private bool isSmashing;
    private bool isCastingUltimate;
    private bool isSmashAnimFinished;

    private float lastSmashTime = -Mathf.Infinity;
    private float lastJumpEscapeTime = -Mathf.Infinity;
    private float lastUltimateTime = -Mathf.Infinity;

    private bool IsOccupied => isJumping || isSmashing || isCastingUltimate;


    protected override void Awake()
    {
        base.Awake();
        eliteHealth = GetComponent<EliteWarlockHealthBase>();
        if (eliteHealth != null)
            eliteHealth.OnEnrage += () => isEnraged = true;
        lastSmashTime = Time.time;
    }


    protected override void UpdateState()
    {
        if (IsOccupied || isWindingUp) return;

        if (HasTarget && Vector3.Distance(transform.position, TargetPosition) < jumpTriggerRange && CanEscapeJump())
        {
            StartCoroutine(JumpRoutine());
            return;
        }

        base.UpdateState();
    }

    protected override void TickState()
    {
        if (IsOccupied) return;

        if (currentState == WarlockState.WindUp)
        {
            if (isEnraged && TryUltimate()) return;
            if (TrySmash()) return;
        }

        base.TickState();
    }

    // Shoot

    public override void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        isTargetLocked = true;
        SpawnSingleProjectile();
    }

    public override void FireLastProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();
        isTargetLocked = false;
        isWindingUp = false;
        lastShootTime = Time.time;
        TriggerPostAttackDelay();
    }

    // Smash

    private bool TrySmash()
    {
        if (isSmashing) return true;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        if (Vector3.Distance(transform.position, TargetPosition) > smashRange) return false;

        StartCoroutine(SmashRoutine());
        return true;
    }

    private IEnumerator SmashRoutine()
    {
        isSmashing = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(smashWindUpDuration);

        SpawnAOEWarning(TargetPosition, smashDamageScale, smashAoeRadius, smashWarningDuration);

        isSmashAnimFinished = false;
        animator?.SetTrigger("Smash");
        yield return new WaitUntil(() => isSmashAnimFinished);

        lastSmashTime = Time.time;
        isSmashing = false;
        TriggerPostAttackDelay();
    }

    public void FinishSmash() => isSmashAnimFinished = true;

    // Jump

    private bool CanEscapeJump() => !isJumping && Time.time >= lastJumpEscapeTime + jumpEscapeCooldown;

    private Vector3 FindJumpDestination()
    {
        Vector3 best = transform.position;
        float bestDist = -1f;

        for (int i = 0; i < jumpCandidates; i++)
        {
            float angle = i * (360f / jumpCandidates);
            Vector3 candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * jumpRadius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;

            float d = Vector3.Distance(hit.position, TargetPosition);
            if (d > bestDist) { bestDist = d; best = hit.position; }
        }

        return best;
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        movement.SetCanMove(false);
        animator?.SetTrigger("Jump");

        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / jumpDuration;
        }

        Vector3 destination = FindJumpDestination();
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            transform.position = Vector3.Lerp(startPos, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpPeakHeight;
            yield return null;
        }

        var agent = movement.GetAgent();
        if (NavMesh.SamplePosition(destination, out NavMeshHit landHit, 2f, NavMesh.AllAreas))
            destination = landHit.position;

        if (agent != null) agent.Warp(destination);
        else transform.position = destination;

        if (animator != null) animator.speed = 1f;
        movement.SetCanMove(true);
        lastJumpEscapeTime = Time.time;
        isJumping = false;
    }

    // Ultimate

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

        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(ultimateWindUpDuration);

        SpawnUltimateCircles();

        isSmashAnimFinished = false;
        animator?.SetTrigger("Smash");
        yield return new WaitUntil(() => isSmashAnimFinished);

        lastUltimateTime = Time.time;
        isCastingUltimate = false;
        TriggerPostAttackDelay();
    }

    private void SpawnUltimateCircles()
    {
        int circleCount = Random.Range(ultimateCircleCountMin, ultimateCircleCountMax + 1);
        var spawned = new List<Vector3>();
        int maxAttempts = circleCount * 5;

        for (int attempt = 0; attempt < maxAttempts && spawned.Count < circleCount; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle * ultimateSpawnRadius;
            Vector3 candidate = TargetPosition + new Vector3(rand.x, 0f, rand.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, NavMesh.AllAreas)) continue;

            bool tooClose = false;
            foreach (var pos in spawned)
                if (Vector3.Distance(navHit.position, pos) < ultimateMinSpawnDistance) { tooClose = true; break; }
            if (tooClose) continue;

            SpawnAOEWarning(navHit.position, ultimateDamageScale, ultimateAoeRadius, ultimateWarningDuration);
            spawned.Add(navHit.position);
        }
    }

    private void SpawnAOEWarning(Vector3 pos, float damageScale, float radius, float duration)
    {
        if (aoeWarningPrefab == null) return;
        var go = Instantiate(aoeWarningPrefab, pos, Quaternion.identity);
        go.GetComponent<WarlockAOEWarning>()?.Initialize(stats.Damage * damageScale, radius, duration, targetLayers);
    }

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
        Gizmos.DrawWireSphere(transform.position, jumpTriggerRange);
    }
}