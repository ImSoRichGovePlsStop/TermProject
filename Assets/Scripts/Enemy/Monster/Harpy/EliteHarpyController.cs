using System.Collections;
using UnityEngine;

public class EliteHarpyController : HarpyController
{
    public enum AirPhaseState { None, FlyingUp, Hovering, Diving, FlyingUpAfterDive, Landing }

    [Header("Air Phase")]
    [SerializeField] private float airCooldown = 8f;
    [SerializeField] private float airCooldownEnraged = 4f;
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

    [Header("References")]
    [SerializeField] private Rigidbody rb;

    private EliteHarpyHealthBase eliteHealth;
    private bool isEnraged = false;
    private float lastAirTime = 0f;
    private GameObject activeWarning;
    private AirPhaseState airState = AirPhaseState.None;
    private bool diveLandFinished = false;
    private float baseY;

    // Air Phase detection override
    private float savedDetectRange;
    private float savedLoseTargetRange;
    private float savedPlayerPriority;

    public bool IsInAirPhase => airState != AirPhaseState.None;

    protected override void Awake()
    {
        base.Awake();
        if (rb == null) rb = GetComponent<Rigidbody>();
        eliteHealth = GetComponent<EliteHarpyHealthBase>();
        if (eliteHealth != null)
            eliteHealth.OnEnrage += () => isEnraged = true;
    }

    protected override void Start()
    {
        baseY = transform.position.y;
        lastAirTime = Time.time;
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
            && Time.time >= lastAirTime + (isEnraged ? airCooldownEnraged : airCooldown))
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

        yield return StartCoroutine(DiveRoutine());

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
        airState = AirPhaseState.None;
        TriggerPostAttackDelay();
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
            sw.GetComponent<HarpyShockwave>()?.Init(stats.Damage * shockwaveDamageScale, eliteHealth);
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
        for (int i = 0; i < 10; i++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized * Random.Range(landRadius * 0.5f, landRadius);
            Vector3 candidate = transform.position + new Vector3(rand.x, 0f, rand.y);
            candidate.y = baseY;

            if (UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit hit, 1f, UnityEngine.AI.NavMesh.AllAreas))
                return hit.position;
        }

        return transform.position;
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