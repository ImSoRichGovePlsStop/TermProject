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
    [SerializeField] private WanderBehavior wander;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.1f;
    [SerializeField] private float speedScale = 0.1f;

    private HealthBase currentTarget;
    private BrawlerState currentState = BrawlerState.Wander;

    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;


    private LayerMask _enemyMask;

    protected override void ApplyPlayerScaling()
    {
        if (playerStats == null) return;
        stats.AddFlatModifier(new EntityStatModifier
        {
            maxHP = playerStats.MaxHealth * hpScale,
            moveSpeed = playerStats.MoveSpeed * speedScale
        });
        health.SetMaxHP(stats.MaxHP);
    }

    protected override void Awake()
    {
        base.Awake();

        movement.SetStopDistance(attackRange * 0.8f);
        _enemyMask = 1 << LayerMask.NameToLayer("Enemy");

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
        currentTarget = CombatUtility.FindNearest<HealthBase>(transform.position, searchRadius, _enemyMask);
    }

    private void UpdateState()
    {
        if (isAttacking)
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
                isAttacking = false;
                currentState = BrawlerState.Wander;
                return;
            }
            currentState = BrawlerState.Attack;
            return;
        }

        BrawlerState prevState = currentState;

        if (currentTarget != null)
        {
            Vector3 flatTarget = currentTarget.transform.position;
            flatTarget.y = transform.position.y;
            float flatDist = Vector3.Distance(transform.position, flatTarget);
            float heightDiff = Mathf.Abs(currentTarget.transform.position.y - transform.position.y);
            if (heightDiff > maxHeightDiff) { currentTarget = null; currentState = BrawlerState.Wander; return; }
            currentState = flatDist <= attackRange ? BrawlerState.Attack : BrawlerState.Chase;
        }
        else
        {
            isAttacking = false;
            currentState = BrawlerState.Wander;
        }

        if (prevState == BrawlerState.Wander && currentState != BrawlerState.Wander)
            wander.Reset(movement);
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
                wander.Tick(transform, playerStats?.transform, movement);
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

        Collider[] hits = Physics.OverlapSphere(transform.position, attackRange, _enemyMask);
        foreach (var hit in hits)
        {
            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                float angle = Vector3.Angle(attackDir, dir);
                if (angle > attackAngle * 0.5f) continue;
            }

            var enemyHealth = hit.GetComponentInParent<HealthBase>();
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 leftDir = Quaternion.Euler(0, -attackAngle * 0.5f, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, attackAngle * 0.5f, 0) * transform.forward;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawLine(transform.position, transform.position + leftDir * attackRange);
        Gizmos.DrawLine(transform.position, transform.position + rightDir * attackRange);
    }
}