using System.Collections;
using UnityEngine;

public class WarlockController : EnemyBase
{
    public enum WarlockState { Wander, Chase, Strafe, WindUp }

    [Header("Shoot")]
    [SerializeField] protected float shootRange = 6f;
    [SerializeField] protected float minRange = 4f;
    [SerializeField] protected float shootCooldown = 2.5f;
    [SerializeField] protected float projectileDamageScale = 1f;
    [SerializeField] protected float spreadAngle = 15f;
    [SerializeField] protected float maxTrackRotateSpeed = 90f;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;

    [Header("AoE Smash")]
    [SerializeField] protected float smashRange = 5f;
    [SerializeField] protected float smashDamageScale = 1.2f;
    [SerializeField] protected float smashWindUpDuration = 0.8f;
    [SerializeField] protected float smashWarningDuration = 1f;
    [SerializeField] protected float smashAoeRadius = 2.5f;
    [SerializeField] protected float smashCooldown = 5f;
    [SerializeField] protected GameObject aoeWarningPrefab;
    [SerializeField] protected LayerMask targetLayers;

    [Header("Strafe")]
    [SerializeField] protected float strafeIdleTimeMin = 0.5f;
    [SerializeField] protected float strafeIdleTimeMax = 1.5f;

    [Header("Retreat")]
    [SerializeField] private float retreatDelay = 0.5f;

    protected WarlockState currentState = WarlockState.Wander;
    protected bool isWindingUp = false;
    protected bool isSmashing = false;
    protected float lastShootTime = -Mathf.Infinity;
    protected float lastSmashTime = 0f;
    protected Vector3 lockedTargetPosition;

    // Strafe
    private bool strafeIsIdling = false;
    private float strafeIdleTimer = 0f;
    private Vector3 strafeTarget = Vector3.zero;

    // Retreat
    private bool isWaitingToRetreat = false;
    private bool isRetreating = false;
    private float retreatDelayTimer = 0f;
    private Vector3 retreatDestination;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
    }

    // ?? State Machine ??????????????????????????????????????????

    protected override void UpdateState()
    {
        if (isWindingUp || isSmashing) return;

        WarlockState prevState = currentState;

        if (!HasTarget || Vector3.Distance(transform.position, TargetPosition) >= minRange + 0.5f)
        {
            isRetreating = false;
            isWaitingToRetreat = false;
        }

        if (isRetreating || isWaitingToRetreat) return;

        if (!HasTarget)
        {
            currentState = WarlockState.Wander;
            return;
        }

        if (prevState == WarlockState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canShoot = Time.time >= lastShootTime + shootCooldown;

        if (canShoot && dist <= shootRange) currentState = WarlockState.WindUp;
        else if (dist > shootRange) currentState = WarlockState.Chase;
        else if (dist < minRange) currentState = WarlockState.Chase;
        else currentState = WarlockState.Strafe;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case WarlockState.Wander: TickWander(); break;
            case WarlockState.Chase: TickChase(); break;
            case WarlockState.Strafe: TickStrafe(); break;
            case WarlockState.WindUp: TickWindUp(); break;
        }
    }

    // ?? State Ticks ????????????????????????????????????????????

    private void TickWander()
    {
        wander.Tick(transform, transform, movement, stats);
    }

    private void TickChase()
    {
        float dist = Vector3.Distance(transform.position, TargetPosition);

        if (isRetreating)
        {
            var ag = movement.GetAgent();
            bool reached = ag != null && ag.hasPath && ag.remainingDistance <= 0.2f;
            if (reached || dist >= minRange + 0.5f)
                isRetreating = false;
            else
                movement.MoveToTarget(retreatDestination);
            return;
        }

        if (dist < minRange)
        {
            if (!isWaitingToRetreat)
            {
                isWaitingToRetreat = true;
                retreatDelayTimer = retreatDelay;
            }
            else
            {
                retreatDelayTimer -= Time.deltaTime;
                if (retreatDelayTimer <= 0f)
                {
                    isWaitingToRetreat = false;
                    isRetreating = true;
                    Vector3 awayDir = (transform.position - TargetPosition);
                    awayDir.y = 0f;
                    awayDir = awayDir.sqrMagnitude > 0.001f ? awayDir.normalized : Vector3.forward;
                    retreatDestination = transform.position + awayDir * (minRange - dist + 1.5f);
                }
            }
            movement.StopMoving();
            return;
        }

        isWaitingToRetreat = false;
        movement.MoveToTarget(TargetPosition);
    }

    private void TickStrafe()
    {
        movement.FaceTarget(TargetPosition);

        if (strafeIsIdling)
        {
            movement.StopMoving();
            strafeIdleTimer -= Time.deltaTime;
            if (strafeIdleTimer <= 0f)
            {
                strafeIsIdling = false;
                strafeTarget = Vector3.zero;
            }
            return;
        }

        if (strafeTarget == Vector3.zero)
            strafeTarget = GetStrafePoint();

        var agent = movement.GetAgent();
        bool reached = agent != null && agent.hasPath && agent.remainingDistance <= agent.stoppingDistance + 0.1f;
        if (reached)
        {
            strafeIsIdling = true;
            strafeIdleTimer = Random.Range(strafeIdleTimeMin, strafeIdleTimeMax);
            movement.StopMoving();
            return;
        }

        movement.MoveToTarget(strafeTarget);
    }

    private void TickWindUp()
    {
        movement.StopMoving();

        if (HasTarget)
        {
            Vector3 currentDir = (lockedTargetPosition - transform.position);
            Vector3 targetDir = (TargetPosition - transform.position);
            currentDir.y = 0f;
            targetDir.y = 0f;

            if (currentDir.sqrMagnitude > 0.001f && targetDir.sqrMagnitude > 0.001f)
            {
                float maxDeg = maxTrackRotateSpeed * Time.deltaTime;
                Vector3 newDir = Vector3.RotateTowards(currentDir.normalized, targetDir.normalized, maxDeg * Mathf.Deg2Rad, 0f);
                lockedTargetPosition = transform.position + newDir * targetDir.magnitude;
            }
        }

        movement.FaceTarget(lockedTargetPosition);
        if (!TrySmash()) TryWindUp();
    }

    // ?? Smash ??????????????????????????????????????????????????

    protected virtual bool TrySmash()
    {
        if (isSmashing) return true;
        if (isWindingUp) return false;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        if (!HasTarget || Vector3.Distance(transform.position, TargetPosition) > smashRange) return false;

        StartCoroutine(SmashRoutine());
        return true;
    }

    private System.Collections.IEnumerator SmashRoutine()
    {
        isSmashing = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");
        yield return new UnityEngine.WaitForSeconds(smashWindUpDuration);

        SpawnAOEWarning(TargetPosition, smashDamageScale, smashAoeRadius, smashWarningDuration);

        animator?.SetTrigger("Smash");
        yield return new UnityEngine.WaitForSeconds(smashWarningDuration);

        lastSmashTime = Time.time;
        isSmashing = false;
        TriggerPostAttackDelay();
    }

    protected void SpawnAOEWarning(Vector3 pos, float damageScale, float radius, float duration)
    {
        if (aoeWarningPrefab == null) return;
        var go = Instantiate(aoeWarningPrefab, pos, Quaternion.identity);
        go.GetComponent<WarlockAOEWarning>()?.Initialize(stats.Damage * damageScale, radius, duration, targetLayers, health);
    }

    // ?? Helpers ????????????????????????????????????????????????

    private Vector3 GetStrafePoint()
    {
        Vector3 toTarget = TargetPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;

        float preferredDist = (minRange + shootRange) * 0.5f;
        int dir = Random.value > 0.5f ? 1 : -1;
        Vector3 strafeVec = Vector3.Cross(Vector3.up, toTarget.normalized) * dir;
        Vector3 candidate = transform.position + strafeVec * Random.Range(1.5f, 3f);

        Vector3 toCand = candidate - TargetPosition;
        toCand.y = 0f;
        if (toCand.sqrMagnitude > 0.001f)
            candidate = TargetPosition + toCand.normalized * preferredDist;

        candidate.y = transform.position.y;
        return candidate;
    }

    protected bool TryWindUp()
    {
        if (isWindingUp) return true;
        if (Time.time < lastShootTime + shootCooldown) return false;

        isWindingUp = true;
        lockedTargetPosition = TargetPosition;
        animator?.SetTrigger("WindUp");
        return true;
    }

    // ?? Animation Events ???????????????????????????????????????

    public virtual void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();
    }

    public virtual void FireLastProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();
        isWindingUp = false;
        lastShootTime = Time.time;
        TriggerPostAttackDelay();
    }

    protected void SpawnSingleProjectile()
    {
        Vector3 baseDir = lockedTargetPosition - firePoint.position;
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = transform.forward;
        baseDir.Normalize();

        float angle = Random.Range(-spreadAngle, spreadAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        var proj = go.GetComponent<WarlockProjectile>();
        proj?.Initialize(firePoint.position + dir * 20f, stats.Damage * projectileDamageScale, health);
    }

    // ?? Overrides ??????????????????????????????????????????????

    public override void OnDeath()
    {
        base.OnDeath();
        isWindingUp = false;
        isSmashing = false;
        StopAllCoroutines();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minRange);
    }
}