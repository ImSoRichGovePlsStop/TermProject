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
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;

    [Header("Strafe")]
    [SerializeField] protected float strafeIdleTimeMin = 0.5f;
    [SerializeField] protected float strafeIdleTimeMax = 1.5f;

    [Header("Retreat")]
    [SerializeField] private float retreatDelay = 0.5f;

    protected WarlockState currentState = WarlockState.Wander;
    protected bool isWindingUp = false;
    protected float lastShootTime = -Mathf.Infinity;

    // Strafe
    private bool strafeIsIdling = false;
    private float strafeIdleTimer = 0f;
    private Vector3 strafeTarget = Vector3.zero;

    // Shoot
    private Vector3 lockedTargetPosition;
    private bool isTargetLocked = false;

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
        if (isWindingUp) return;

        WarlockState prevState = currentState;

        // Clear retreat if far enough from target
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
            wander.Reset(movement);

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
        wander.Tick(transform, transform, movement);
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
        if (HasTarget && !isTargetLocked)
        {
            lockedTargetPosition = TargetPosition;
            movement.FaceTarget(lockedTargetPosition);
        }
        TryWindUp();
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
        isTargetLocked = false;
        lockedTargetPosition = TargetPosition;
        animator?.SetTrigger("WindUp");
        return true;
    }

    // ?? Animation Events ???????????????????????????????????????

    public virtual void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        isTargetLocked = true;
        SpawnSingleProjectile();
    }

    public virtual void FireLastProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();

        isTargetLocked = false;
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
        Vector3 spawnTarget = firePoint.position + dir * 20f;

        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        var proj = go.GetComponent<WarlockProjectile>();
        proj?.Initialize(spawnTarget, stats.Damage * projectileDamageScale, health);
    }

    // ?? Overrides ??????????????????????????????????????????????

    public override void OnDeath()
    {
        base.OnDeath();
        isWindingUp = false;
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