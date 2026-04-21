using System.Collections;
using UnityEngine;

public class MinibossHarpyController : HarpyController
{
    public enum AirPhaseState { None, FlyingUp, Hovering, Diving, FlyingUpAfterDive, Landing }

    [Header("Air Phase")]
    [SerializeField] private float flyHeight = 3f;
    [SerializeField] private float flySpeed = 5f;
    [SerializeField] private float hoverDuration = 0.5f;

    [Header("Dive")]
    [SerializeField] private float diveSpeed = 14f;
    [SerializeField] private float diveDamageScale = 2f;
    [SerializeField] private float diveHitRadius = 1f;

    [Header("Dive Warning")]
    [SerializeField] private GameObject diveWarningPrefab;
    [SerializeField] private float diveWarningRadius = 0.8f;
    [SerializeField] private float diveWanderDuration = 1.5f;
    [SerializeField] private float diveLockDuration = 0.5f;
    [SerializeField] private float diveMoveToPlayerDuration = 0.5f;
    [SerializeField] private float diveWanderRadius = 3f;
    [SerializeField] private float diveWanderSpeed = 2f;

    [Header("Shockwave")]
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private float shockwaveDamageScale = 1.5f;

    [Header("Land")]
    [SerializeField] private float landRadius = 2f;

    [Header("Fire Trail")]
    [SerializeField] private GameObject fireDropWarningPrefab;
    [SerializeField] private GameObject fireFieldPrefab;
    [SerializeField] private float fireTrailDropInterval = 2f;
    [SerializeField] private float fireDropWarningDuration = 1.5f;
    [SerializeField] private float fireDropRadius = 1.2f;
    [SerializeField] private float fireDropDamageScale = 0.8f;
    [SerializeField] private float fireTrailSpeed = 6f;
    [SerializeField] private float fireFieldDuration = 4f;
    [SerializeField] private float fireFieldTickInterval = 0.5f;
    [SerializeField] private float fireFieldDamageScale = 0.3f;

    [Header("Before Enrage - Air Phase")]
    [SerializeField] private float airCooldownNormalMin = 6f;
    [SerializeField] private float airCooldownNormalMax = 10f;
    [SerializeField][Range(0f, 1f)] private float fireTrailChanceNormal = 0.5f;
    [SerializeField][Range(0f, 1f)] private float fireTrailChance2Normal = 0.5f;
    [SerializeField][Range(0f, 1f)] private float diveAfterTrailChanceNormal = 0.5f;

    [Header("After Enrage - Air Phase")]
    [SerializeField] private float airCooldownEnragedMin = 3f;
    [SerializeField] private float airCooldownEnragedMax = 6f;
    [SerializeField][Range(0f, 1f)] private float fireTrailChanceEnraged = 0.7f;
    [SerializeField][Range(0f, 1f)] private float fireTrailChance2Enraged = 0.7f;
    [SerializeField][Range(0f, 1f)] private float diveAfterTrailChanceEnraged = 0.7f;

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private MinibossHarpyHealthBase eliteHealth;
    private bool isEnraged = false;
    private float lastAirTime = 0f;
    private float currentAirCooldown;
    private GameObject activeWarning;
    private AirPhaseState airState = AirPhaseState.None;
    private bool diveLandFinished = false;
    private float baseY;

    // Air Phase detection override
    private float savedDetectRange;
    private float savedLoseTargetRange;
    private float savedPlayerPriority;

    public bool IsInAirPhase => airState != AirPhaseState.None;

    public override bool CanBeInterrupted() => false;

    protected override void Awake()
    {
        base.Awake();
        if (rb == null) rb = GetComponent<Rigidbody>();
        eliteHealth = GetComponent<MinibossHarpyHealthBase>();
        if (eliteHealth != null)
            eliteHealth.OnEnrage += () => isEnraged = true;
    }

    protected override void Start()
    {
        baseY = transform.position.y;
        lastAirTime = Time.time;
        currentAirCooldown = Random.Range(airCooldownNormalMin, airCooldownNormalMax);
    }

    protected override void UpdateState()
    {
        if (IsInAirPhase) return;
        base.UpdateState();
    }

    protected override void TickState()
    {
        if (IsInAirPhase) return;

        if (HasTarget && !IsAttacking
            && Time.time >= lastAirTime + currentAirCooldown)
        {
            StartCoroutine(AirPhaseRoutine());
            return;
        }

        base.TickState();
    }

    private IEnumerator AirPhaseRoutine()
    {
        airState = AirPhaseState.FlyingUp;
        movement.SetCanMove(false);
        rb.useGravity = false;

        var agent = movement.GetAgent();
        if (agent != null) agent.enabled = false;

        animator?.SetBool("IsFlying", true);

        ApplyAirPhaseDetection();

        yield return StartCoroutine(FlyToHeight(baseY + flyHeight));

        airState = AirPhaseState.Hovering;
        yield return new WaitForSeconds(hoverDuration);

        float trailChance = isEnraged ? fireTrailChanceEnraged : fireTrailChanceNormal;
        float trail2Chance = isEnraged ? fireTrailChance2Enraged : fireTrailChance2Normal;
        float diveChance = isEnraged ? diveAfterTrailChanceEnraged : diveAfterTrailChanceNormal;

        bool doFireTrail = Random.value <= trailChance;
        bool doSecondFireTrail = doFireTrail && Random.value <= trail2Chance;
        bool doDive = !doFireTrail || Random.value <= diveChance;

        if (doFireTrail)
        {
            yield return StartCoroutine(FireTrailRoutine());
            if (doSecondFireTrail)
            {
                airState = AirPhaseState.FlyingUp;
                yield return StartCoroutine(FlyToHeight(baseY + flyHeight));
                yield return StartCoroutine(FireTrailRoutine());
            }
        }

        if (doDive)
        {
            airState = AirPhaseState.FlyingUp;
            yield return StartCoroutine(FlyToHeight(baseY + flyHeight));
            yield return StartCoroutine(DiveRoutine());
        }

        airState = AirPhaseState.FlyingUp;
        yield return StartCoroutine(FlyToHeight(baseY + flyHeight));

        airState = AirPhaseState.Landing;
        Vector3 landPos = GetRandomLandPosition();
        yield return StartCoroutine(FlyToPosition(landPos));

        RestoreDetection();

        rb.useGravity = true;
        if (agent != null) agent.enabled = true;
        movement.SetCanMove(true);
        animator?.SetBool("IsFlying", false);

        lastAirTime = Time.time;
        currentAirCooldown = Random.Range(
            isEnraged ? airCooldownEnragedMin : airCooldownNormalMin,
            isEnraged ? airCooldownEnragedMax : airCooldownNormalMax);
        airState = AirPhaseState.None;
        TriggerPostAttackDelay();
    }

    private IEnumerator FireTrailRoutine()
    {
        if (playerTarget == null) yield break;

        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        LayerMask targetLayers = (1 << LayerMask.NameToLayer("Player"))
                                | (1 << LayerMask.NameToLayer("Summoner"))
                                | (1 << LayerMask.NameToLayer("Totem"));

        Vector3 playerPos = playerTarget.transform.position;

        // Find entry and exit points
        float entryAngle = Random.Range(0f, 360f);
        float exitAngle = entryAngle + 180f + Random.Range(-10f, 10f);

        Vector3 entryDir = new Vector3(Mathf.Sin(entryAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(entryAngle * Mathf.Deg2Rad));
        Vector3 exitDir = new Vector3(Mathf.Sin(exitAngle * Mathf.Deg2Rad), 0f, Mathf.Cos(exitAngle * Mathf.Deg2Rad));

        Vector3 entryPoint = playerPos;
        Vector3 exitPoint = playerPos;

        if (Physics.Raycast(new Vector3(playerPos.x, baseY + 0.1f, playerPos.z), entryDir, out RaycastHit entryHit, 50f, wallMask))
            entryPoint = new Vector3(entryHit.point.x, baseY + flyHeight, entryHit.point.z);
        else
            entryPoint = new Vector3(playerPos.x + entryDir.x * 20f, baseY + flyHeight, playerPos.z + entryDir.z * 20f);

        if (Physics.Raycast(new Vector3(playerPos.x, baseY + 0.1f, playerPos.z), exitDir, out RaycastHit exitHit, 50f, wallMask))
            exitPoint = new Vector3(exitHit.point.x, baseY + flyHeight, exitHit.point.z);
        else
            exitPoint = new Vector3(playerPos.x + exitDir.x * 20f, baseY + flyHeight, playerPos.z + exitDir.z * 20f);

        // Fly to entry point
        yield return StartCoroutine(FlyToPosition(entryPoint));

        // Fly from entry to exit dropping fire balls
        Vector3 flyDir = (exitPoint - entryPoint);
        flyDir.y = 0f;
        float totalDist = flyDir.magnitude;
        if (totalDist < 0.01f) yield break;
        flyDir.Normalize();

        float distTraveled = 0f;
        float distSinceLastDrop = 0f;

        while (distTraveled < totalDist)
        {
            float step = fireTrailSpeed * Time.deltaTime;
            step = Mathf.Min(step, totalDist - distTraveled);

            transform.position += new Vector3(flyDir.x, 0f, flyDir.z) * step;
            movement.FaceTarget(transform.position + flyDir);
            distTraveled += step;
            distSinceLastDrop += step;

            if (distSinceLastDrop >= fireTrailDropInterval)
            {
                distSinceLastDrop = 0f;
                if (fireDropWarningPrefab != null)
                {
                    Vector3 dropPos = new Vector3(transform.position.x, baseY, transform.position.z);
                    GameObject warning = Instantiate(fireDropWarningPrefab, dropPos, Quaternion.identity);
                    var w = warning.GetComponent<HarpyFireDropWarning>();
                    w?.Initialize(stats.Damage * fireDropDamageScale, fireDropRadius,
                        fireDropWarningDuration, targetLayers, health, fireFieldPrefab,
                        fireFieldDuration, fireFieldTickInterval, stats.Damage * fireFieldDamageScale);
                }
            }

            yield return null;
        }
    }

    private IEnumerator DiveRoutine()
    {
        Vector3 wanderCenter = TargetPosition;
        wanderCenter.y = baseY + 0.05f;

        GameObject warning = null;
        DiveWarningVFX warningVFX = null;
        if (diveWarningPrefab != null)
        {
            warning = Instantiate(diveWarningPrefab, wanderCenter, Quaternion.identity);
            activeWarning = warning;
            warningVFX = warning.GetComponent<DiveWarningVFX>();
            warningVFX?.SetRadius(diveWarningRadius);
        }

        Vector3 wanderPos = wanderCenter;
        Vector3 wanderTarget = GetRandomWanderPoint();
        float elapsed = 0f;
        while (elapsed < diveWanderDuration)
        {
            if (Vector3.Distance(wanderPos, wanderTarget) < 0.2f)
                wanderTarget = GetRandomWanderPoint();

            wanderPos = Vector3.MoveTowards(wanderPos, wanderTarget, diveWanderSpeed * Time.deltaTime);
            wanderPos.y = baseY + 0.05f;
            if (warning != null) warning.transform.position = wanderPos;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Vector3 diveTarget = TargetPosition;
        diveTarget.y = baseY;
        Vector3 moveTarget = diveTarget;
        moveTarget.y = baseY + 0.05f;

        float moveElapsed = 0f;
        float moveDuration = diveMoveToPlayerDuration;
        Vector3 moveStart = wanderPos;
        while (moveElapsed < moveDuration)
        {
            moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(moveElapsed / moveDuration);
            Vector3 pos = Vector3.Lerp(moveStart, moveTarget, t);
            if (warning != null) warning.transform.position = pos;
            yield return null;
        }

        if (warning != null) warning.transform.position = moveTarget;
        warningVFX?.SetLocked(true);

        yield return new WaitForSeconds(diveLockDuration);

        if (warning != null) Destroy(warning);
        activeWarning = null;

        airState = AirPhaseState.Diving;
        animator?.SetTrigger("Dive");

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        bool hitSomething = false;

        while (Vector3.Distance(transform.position, diveTarget) > 0.3f)
        {
            Vector3 dir = (diveTarget - transform.position).normalized;
            transform.position += dir * diveSpeed * stats.MoveSpeedRatio * Time.deltaTime;

            if (!hitSomething)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, diveHitRadius, hitMask);
                foreach (var col in hits)
                {
                    var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                    if (ps != null && !ps.IsDead) { ps.TakeDamage(stats.Damage * diveDamageScale, eliteHealth); hitSomething = true; continue; }

                    var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
                    if (hb != null && !hb.IsDead) { hb.TakeDamage(stats.Damage * diveDamageScale); hitSomething = true; }
                }
            }

            yield return null;
        }

        animator?.SetTrigger("DiveEnd");

        if (shockwavePrefab != null)
        {
            var sw = Instantiate(shockwavePrefab, diveTarget, Quaternion.identity);
            sw.GetComponent<Shockwave>()?.Init(stats.Damage * shockwaveDamageScale, eliteHealth);
        }

        while (!diveLandFinished)
            yield return null;

        diveLandFinished = false;
    }

    private IEnumerator FlyToHeight(float targetY)
    {
        while (Mathf.Abs(transform.position.y - targetY) > 0.1f)
        {
            Vector3 pos = transform.position;
            pos.y = Mathf.MoveTowards(pos.y, targetY, flySpeed * stats.MoveSpeedRatio * Time.deltaTime);
            transform.position = pos;
            yield return null;
        }
    }

    private IEnumerator FlyToPosition(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, target, flySpeed * stats.MoveSpeedRatio * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }

    // Animation Event
    public void FinishDiveLand()
    {
        diveLandFinished = true;
    }

    private Vector3 GetRandomWanderPoint()
    {
        Vector2 rand = Random.insideUnitCircle * diveWanderRadius;
        Vector3 point = TargetPosition + new Vector3(rand.x, 0f, rand.y);
        point.y = baseY + 0.05f;
        return point;
    }

    private Vector3 GetRandomLandPosition()
    {
        Vector3 center = playerTarget != null ? playerTarget.transform.position : transform.position;
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");

        for (int i = 0; i < 20; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized * Random.Range(landRadius * 0.5f, landRadius);
            Vector3 candidate = center + new Vector3(rand.x, 0f, rand.y);
            candidate.y = baseY;

            if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
                continue;

            Vector3 rayOrigin = new Vector3(center.x, baseY + 0.1f, center.z);
            Vector3 rayTarget = new Vector3(hit.position.x, baseY + 0.1f, hit.position.z);
            Vector3 rayDir = rayTarget - rayOrigin;
            float rayDist = rayDir.magnitude;

            if (rayDist < 0.01f) return hit.position;
            if (!Physics.Raycast(rayOrigin, rayDir.normalized, rayDist, wallMask))
                return hit.position;
        }

        return playerTarget != null ? playerTarget.transform.position : transform.position;
    }
    private void ApplyAirPhaseDetection()
    {
        savedDetectRange = detectRange;
        savedLoseTargetRange = loseTargetRange;
        savedPlayerPriority = targetPriority.playerPriority;

        detectRange = float.MaxValue;
        loseTargetRange = float.MaxValue;
        targetPriority.playerPriority = savedPlayerPriority * 3f;
    }

    private void RestoreDetection()
    {
        detectRange = savedDetectRange;
        loseTargetRange = savedLoseTargetRange;
        targetPriority.playerPriority = savedPlayerPriority;
    }

    public override void OnDeath()
    {
        if (IsInAirPhase) RestoreDetection();
        if (activeWarning != null)
        {
            Destroy(activeWarning);
            activeWarning = null;
        }
        base.OnDeath();
    }
}