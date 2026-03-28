using UnityEngine;

[RequireComponent(typeof(SummonerHealth))]
[RequireComponent(typeof(SummonerMovement))]
[RequireComponent(typeof(EntityStats))]
public class BrawlerSummoner : SummonerBase
{
    public enum BrawlerState
    {
        Wander,
        Chase,
        Attack
    }

    [Header("Attack")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackAngle = 120f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float damageScale = 0.25f;
    [SerializeField] private Animator animator;

    [Header("Search")]
    [SerializeField] private float searchRadius = 10f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderSpeedMultiplier = 0.5f;
    [SerializeField] private float wanderIdleTimeMin = 0.5f;
    [SerializeField] private float wanderIdleTimeMax = 1.5f;

    private EnemyHealth currentTarget;
    private BrawlerState currentState = BrawlerState.Wander;

    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    private Vector3 wanderTarget;
    private float wanderIdleTimer = 0f;
    private bool isWanderIdling = false;

    protected override void Awake()
    {
        base.Awake();

        movement.SetStopDistance(attackRange * 0.8f);

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected override void Update()
    {
        base.Update();

        if (health.IsDead) return;

        UpdateTarget();
        UpdateState();
        TickState();
    }

    private void UpdateTarget()
    {
        currentTarget = CombatUtility.FindNearest<EnemyHealth>(transform.position, searchRadius);
    }

    private void UpdateState()
    {
        if (isAttacking)
        {
            currentState = BrawlerState.Attack;
            return;
        }

        BrawlerState prevState = currentState;

        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
            currentState = dist <= attackRange ? BrawlerState.Attack : BrawlerState.Chase;
        }
        else
        {
            isAttacking = false;
            currentState = BrawlerState.Wander;
        }

        if (prevState == BrawlerState.Wander && currentState != BrawlerState.Wander)
        {
            movement.ResetSpeedMultiplier();
            isWanderIdling = false;
        }
    }

    private void TickState()
    {
        switch (currentState)
        {
            case BrawlerState.Chase:
                movement.MoveToTarget(currentTarget.transform.position);
                break;

            case BrawlerState.Attack:
                movement.FaceTarget(currentTarget.transform.position);
                if (!TryAttack())
                    movement.MoveToTarget(currentTarget.transform.position);
                else
                    movement.StopMoving();
                break;

            case BrawlerState.Wander:
                Wander();
                break;
        }
    }

    private bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + attackCooldown) return false;

        isAttacking = true;
        animator.SetTrigger("Attack");
        return true;
    }

    // Animation Event
    public void DealDamage()
    {
        if (currentTarget == null || currentTarget.IsDead) return;

        float damage = stats.Damage + (playerStats != null ? playerStats.Damage * damageScale : 0f);

        Vector3 attackDir = currentTarget.transform.position - transform.position;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.001f)
            attackDir.Normalize();

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(attackDir, dir);
                if (angle > attackAngle * 0.5f) continue;
            }

            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead)
                enemyHealth.TakeDamage(damage);
        }
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
    }

    private void Wander()
    {
        if (playerStats == null) return;

        Vector3 playerPos = playerStats.transform.position;
        float distToPlayer = Vector3.Distance(transform.position, playerPos);

        if (distToPlayer > wanderRadius)
        {
            isWanderIdling = false;
            wanderTarget = Vector3.zero;
            movement.ResetSpeedMultiplier();
            movement.MoveToTarget(playerPos);
            return;
        }

        if (isWanderIdling)
        {
            movement.StopMoving();
            wanderIdleTimer -= Time.deltaTime;
            if (wanderIdleTimer <= 0f)
            {
                isWanderIdling = false;
                wanderTarget = GetRandomWanderPoint(playerPos);
            }
            return;
        }
        if (wanderTarget == Vector3.zero)
        {
            wanderTarget = GetRandomWanderPoint(playerPos);
        }

        var agent = movement.GetAgent();
        bool reachedTarget = agent != null
            && agent.hasPath
            && agent.remainingDistance <= agent.stoppingDistance;

        if (reachedTarget)
        {
            isWanderIdling = true;
            wanderIdleTimer = Random.Range(wanderIdleTimeMin, wanderIdleTimeMax);
            movement.StopMoving();
            return;
        }

        movement.SetSpeedMultiplier(wanderSpeedMultiplier);
        movement.MoveToTarget(wanderTarget);
    }

    private Vector3 GetRandomWanderPoint(Vector3 center)
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        return center + new Vector3(random.x, 0f, random.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftDir = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + leftDir * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * attackRange);

        if (playerStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerStats.transform.position, wanderRadius);
        }
    }
}