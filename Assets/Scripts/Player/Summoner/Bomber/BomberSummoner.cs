using UnityEngine;

[RequireComponent(typeof(SummonerHealth))]
[RequireComponent(typeof(SummonerMovement))]
[RequireComponent(typeof(EntityStats))]
public class BomberSummoner : SummonerBase
{
    public enum BomberState
    {
        Wander,
        Chase,
        Detonating
    }

    [Header("Search")]
    [SerializeField] private float searchRadius = 10f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float centerRadius = 0.8f;
    [SerializeField] private float centerDamageScale = 1.5f;
    [SerializeField] private float centerBaseDamage = 30f;
    [SerializeField] private float edgeDamageScale = 1f;
    [SerializeField] private float edgeBaseDamage = 20f;
    [SerializeField] private float triggerRange = 0.8f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderPointReachedDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private EnemyHealth currentTarget;
    private BomberState currentState = BomberState.Wander;
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

        switch (currentState)
        {
            case BomberState.Wander:
                TickWander();
                break;

            case BomberState.Chase:
                TickChase();
                break;
        }
    }

    private void TickWander()
    {
        currentTarget = CombatUtility.FindNearest<EnemyHealth>(transform.position, searchRadius);

        if (currentTarget != null)
        {
            currentState = BomberState.Chase;
            return;
        }

        if (playerStats == null) return;

        Vector3 playerPos = playerStats.transform.position;
        float distToWander = Vector3.Distance(transform.position, wanderTarget);

        if (wanderTarget == Vector3.zero || distToWander <= wanderPointReachedDistance)
            wanderTarget = GetRandomWanderPoint(playerPos);

        movement.MoveToTarget(wanderTarget);
    }

    private void TickChase()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            currentState = BomberState.Wander;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist <= triggerRange)
        {
            Detonate();
            return;
        }

        movement.MoveToTarget(currentTarget.transform.position);
    }

    private void Detonate()
    {
        currentState = BomberState.Detonating;
        movement.SetCanMove(false);
        animator.SetTrigger("Explode");
    }

    // Animation Event
    public void DealExplosionDamage()
    {
        var enemies = CombatUtility.FindAround<EnemyHealth>(transform.position, explosionRadius);

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            bool isCenter = dist <= centerRadius;

            float damage = isCenter
                ? centerBaseDamage + (playerStats != null ? playerStats.Damage * centerDamageScale : 0f)
                : edgeBaseDamage + (playerStats != null ? playerStats.Damage * edgeDamageScale : 0f);

            enemy.TakeDamage(damage);
        }
    }

    // Animation Event
    public void FinishExplosion()
    {
        DieWithoutAnimation();
    }

    private Vector3 GetRandomWanderPoint(Vector3 center)
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        return center + new Vector3(random.x, 0f, random.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, centerRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);

        if (playerStats != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(playerStats.transform.position, wanderRadius);
        }
    }
}