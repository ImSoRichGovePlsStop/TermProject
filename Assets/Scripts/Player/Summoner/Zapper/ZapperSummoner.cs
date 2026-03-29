using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SummonerHealth))]
[RequireComponent(typeof(SummonerMovement))]
[RequireComponent(typeof(EntityStats))]
public class ZapperSummoner : SummonerBase
{
    public enum ZapperState
    {
        Wander,
        Chase,
        Attached
    }

    [Header("Search")]
    [SerializeField] private float searchRadius = 10f;

    [Header("Attach")]
    [SerializeField] private float attachDuration = 4f;
    [SerializeField] private float attackRange = 1f;

    [Header("Charged Debuff")]
    [SerializeField] private float damageAmpPercent = 0.2f;
    [SerializeField] private float chainPercent = 0.35f;
    [SerializeField] private float chainRange = 2f;

    [Header("Wander")]
    [SerializeField] private float wanderRadius = 3f;
    [SerializeField] private float wanderPointReachedDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("References")]
    [SerializeField] private Collider zapperCollider;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.05f;
    [SerializeField] private float speedScale = 0.1f;

    private PlayerCombatContext context;
    private EnemyHealth currentTarget;
    private ZapperState currentState = ZapperState.Wander;

    private static readonly HashSet<EnemyHealth> attachedEnemies = new HashSet<EnemyHealth>();

    private float attachTimer;
    private Vector3 wanderTarget;
    private bool jumpFinished = false;

    protected override void ApplyPlayerScaling()
    {
        if (playerStats == null) return;
        stats.AddFlatModifier(new EntityStatModifier
        {
            maxHP = playerStats.MaxHealth * hpScale,
            moveSpeed = playerStats.MoveSpeed * speedScale
        });
    }

    public override void Init(PlayerStats playerStats)
    {
        base.Init(playerStats);
        context = playerStats.GetComponent<PlayerCombatContext>();
        context.OnEnemyKilled += OnEnemyKilled;
    }

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (zapperCollider == null)
            zapperCollider = GetComponent<Collider>();
    }

    protected override void Update()
    {
        if (health.IsDead) return;

        switch (currentState)
        {
            case ZapperState.Wander:
                TickWander();
                break;

            case ZapperState.Chase:
                TickChase();
                break;

            case ZapperState.Attached:
                TickAttached();
                break;
        }
    }

    protected override void TickLifetime()
    {
        if (currentState == ZapperState.Attached) return;
        base.TickLifetime();
    }

    private void TickWander()
    {
        var enemies = CombatUtility.FindAround<EnemyHealth>(transform.position, searchRadius);
        EnemyHealth target = FindUnattachedEnemy(enemies);
        if (target != null)
        {
            currentTarget = target;
            currentState = ZapperState.Chase;
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
            currentState = ZapperState.Wander;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist <= attackRange)
        {
            StartCoroutine(JumpAndAttach());
            return;
        }

        movement.MoveToTarget(currentTarget.transform.position);
    }

    private void TickAttached()
    {
        transform.position = currentTarget.transform.position;

        attachTimer -= Time.deltaTime;
        if (attachTimer <= 0f)
            Detach();
    }

    private IEnumerator JumpAndAttach()
    {
        currentState = ZapperState.Attached;
        movement.SetCanMove(false);

        jumpFinished = false;
        animator.SetTrigger("Jump");
        yield return new WaitUntil(() => jumpFinished);

        if (currentTarget == null || currentTarget.IsDead)
        {
            movement.SetCanMove(true);
            currentState = ZapperState.Wander;
            yield break;
        }

        transform.position = currentTarget.transform.position;

        if (zapperCollider != null)
            zapperCollider.enabled = false;

        var statusHandler = currentTarget.GetComponent<EnemyStatusHandler>();
        if (statusHandler != null)
            statusHandler.AddMultiplierModifier(new EnemyStatModifier { damageTaken = damageAmpPercent });

        currentTarget.OnDamageReceived += OnHostDamaged;
        attachedEnemies.Add(currentTarget);
        attachTimer = attachDuration;
    }

    // Animation Event
    public void FinishJump()
    {
        jumpFinished = true;
    }

    private void OnEnemyKilled(EnemyHealth enemy)
    {
        if (enemy == currentTarget)
            Detach();
    }

    private void OnHostDamaged(float damage, bool isCrit)
    {
        if (currentTarget == null) return;

        float chainDamage = damage * chainPercent;

        var enemies = CombatUtility.FindAround<EnemyHealth>(currentTarget.transform.position, chainRange);
        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || enemy == currentTarget) continue;
            enemy.TakeDamage(chainDamage);
        }
    }

    private void Detach()
    {
        if (currentTarget != null)
        {
            var statusHandler = currentTarget.GetComponent<EnemyStatusHandler>();
            if (statusHandler != null)
                statusHandler.RemoveMultiplierModifier(new EnemyStatModifier { damageTaken = damageAmpPercent });

            currentTarget.OnDamageReceived -= OnHostDamaged;
            attachedEnemies.Remove(currentTarget);
            currentTarget = null;
        }

        if (zapperCollider != null)
            zapperCollider.enabled = true;

        context.OnEnemyKilled -= OnEnemyKilled;
        DieWithoutAnimation();
    }

    private EnemyHealth FindUnattachedEnemy(List<EnemyHealth> enemies)
    {
        EnemyHealth closest = null;
        float closestDist = Mathf.Infinity;

        foreach (var enemy in enemies)
        {
            if (enemy == null || enemy.IsDead) continue;
            if (attachedEnemies.Contains(enemy)) continue;

            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    private Vector3 GetRandomWanderPoint(Vector3 center)
    {
        Vector2 random = Random.insideUnitCircle * wanderRadius;
        return center + new Vector3(random.x, 0f, random.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, chainRange);

        if (playerStats != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(playerStats.transform.position, wanderRadius);
        }
    }
}