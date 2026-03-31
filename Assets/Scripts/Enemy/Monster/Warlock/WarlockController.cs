using UnityEngine;

public class WarlockController : EnemyBase
{
    public enum WarlockState { Wander, Chase, Strafe, WindUp }

    [Header("Shoot")]
    [SerializeField] protected float shootRange = 6f;
    [SerializeField] protected float minRange = 4f;
    [SerializeField] protected float shootCooldown = 2.5f;
    [SerializeField] protected float projectileDamageScale = 1f;
    [SerializeField] protected float projectileTargetDistance = 10f;
    [SerializeField] protected float spreadAngle = 15f;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;

    [Header("Strafe")]
    [SerializeField] protected float strafeIdleTimeMin = 0.5f;
    [SerializeField] protected float strafeIdleTimeMax = 1.5f;

    protected WarlockState currentState = WarlockState.Wander;
    protected bool isWindingUp = false;
    protected float lastShootTime = -Mathf.Infinity;

    private bool strafeIsIdling = false;
    private float strafeIdleTimer = 0f;
    private Vector3 strafeTarget = Vector3.zero;

    [Header("Retreat")]
    [SerializeField] private float retreatDelay = 0.5f;
    private float retreatDelayTimer = 0f;
    private bool isWaitingToRetreat = false;
    private bool isRetreating = false;
    private Vector3 retreatDestination;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0f);
    }

    protected override void UpdateState()
    {
        if (isWindingUp) return;

        WarlockState prevState = currentState;

        // Clear retreat flags if player moved away
        float distCheck = HasTarget ? Vector3.Distance(transform.position, TargetPosition) : float.MaxValue;
        if (distCheck >= minRange + 0.5f)
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

        // Just got a target — reset wander
        if (prevState == WarlockState.Wander)
            wander.Reset(movement);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canShoot = Time.time >= lastShootTime + shootCooldown;

        if (canShoot && dist <= shootRange)
            currentState = WarlockState.WindUp;      // shoot — highest priority
        else if (dist > shootRange)
            currentState = WarlockState.Chase;       // chase in
        else if (dist < minRange)
            currentState = WarlockState.Chase;       // retreat
        else
            currentState = WarlockState.Strafe;      // in range but cooldown
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case WarlockState.Wander:
                wander.Tick(transform, transform, movement);
                Debug.Log($"[Warlock] Wander isIdling={wander.IsIdling} hasTarget={HasTarget}");
                break;

            case WarlockState.Chase:
                float dist = Vector3.Distance(transform.position, TargetPosition);
                if (isRetreating)
                {
                    // Already retreating — keep going to locked destination
                    var ag = movement.GetAgent();
                    bool reached = ag != null && ag.hasPath && ag.remainingDistance <= 0.2f;
                    if (reached || dist >= minRange + 0.5f)
                        isRetreating = false;
                    else
                        movement.MoveToTarget(retreatDestination);
                }
                else if (dist < minRange)
                {
                    if (!isWaitingToRetreat)
                    {
                        isWaitingToRetreat = true;
                        retreatDelayTimer = retreatDelay;
                        movement.StopMoving();
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
                            movement.MoveToTarget(retreatDestination);
                        }
                        else
                            movement.StopMoving();
                    }
                }
                else
                {
                    isWaitingToRetreat = false;
                    movement.MoveToTarget(TargetPosition);
                }
                break;

            case WarlockState.Strafe:
                TickStrafe();
                break;

            case WarlockState.WindUp:
                movement.StopMoving();
                movement.FaceTarget(TargetPosition);
                TryWindUp();
                break;
        }
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

    private Vector3 GetStrafePoint()
    {
        Vector3 toTarget = TargetPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.001f) toTarget = Vector3.forward;

        float preferredDist = (minRange + shootRange) * 0.5f;
        int dir = Random.value > 0.5f ? 1 : -1;
        Vector3 strafeVec = Vector3.Cross(Vector3.up, toTarget.normalized) * dir;
        float strafeAmount = Random.Range(1.5f, 3f);
        Vector3 candidate = transform.position + strafeVec * strafeAmount;

        // Keep candidate at preferred distance from target
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
        animator?.SetTrigger("WindUp");
        return true;
    }

    // Called by Animation Event — once per projectile
    public virtual void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();
    }

    // Called by Animation Event on last projectile frame
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
        Vector3 baseDir = TargetPosition - firePoint.position;
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = transform.forward;
        baseDir.Normalize();

        float angle = Random.Range(-spreadAngle, spreadAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
        Vector3 spawnTarget = firePoint.position + dir * projectileTargetDistance;

        var go = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(dir, Vector3.up));
        var proj = go.GetComponent<WarlockProjectile>();
        proj?.Initialize(spawnTarget, stats.Damage * projectileDamageScale, health);
    }

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