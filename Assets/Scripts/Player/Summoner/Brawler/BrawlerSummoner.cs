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
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float damageScale = 0.25f;
    [SerializeField] private Animator animator;

    [Header("Search")]
    [SerializeField] private float searchRadius = 10f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderPointReachedDistance = 0.5f;

    private EnemyHealth currentTarget;
    private BrawlerState currentState = BrawlerState.Wander;

    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    private Vector3 wanderTarget;

    protected override void Awake()
    {
        base.Awake();

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
    }

    private void TickState()
    {
        switch (currentState)
        {
            case BrawlerState.Chase:
                movement.MoveToTarget(currentTarget.transform.position);
                break;

            case BrawlerState.Attack:
                movement.StopMoving();
                movement.FaceTarget(currentTarget.transform.position);
                TryAttack();
                break;

            case BrawlerState.Wander:
                Wander();
                break;
        }
    }

    private void TryAttack()
    {
        if (isAttacking) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
    }

    // Animation Event
    public void DealDamage()
    {
        if (currentTarget == null || currentTarget.IsDead) return;

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist > attackRange) return;

        float damage = stats.Damage + (playerStats != null ? playerStats.Damage * damageScale : 0f);

        currentTarget.TakeDamage(damage);
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
    }

    private void Wander()
    {
        if (playerStats == null) return;

        Vector3 playerPos = playerStats.transform.position;
        float distToWander = Vector3.Distance(transform.position, wanderTarget);

        if (wanderTarget == Vector3.zero || distToWander <= wanderPointReachedDistance)
            wanderTarget = GetRandomWanderPoint(playerPos);

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

        if (playerStats != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerStats.transform.position, wanderRadius);
        }
    }
}