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
    [SerializeField] private float attachRange = 1f;

    [Header("Charged Debuff")]
    [SerializeField] private float damageAmpPercent = 0.1f;
    [SerializeField] private float chainPercent = 0.3f;

    [Header("Wander")]
    [SerializeField] private WanderBehavior wander;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("References")]
    [SerializeField] private Collider zapperCollider;
    [SerializeField] private GameObject shockedVFXPrefab;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.05f;
    [SerializeField] private float speedScale = 0.1f;

    private Vector3 GetAttachPosition(Vector3 targetPos)
    {
        float tiltRad = Camera.main.transform.eulerAngles.x * Mathf.Deg2Rad;
        float offsetY = -targetPos.y * 0.2f;
        float zOffset = offsetY * Mathf.Tan(tiltRad);
        return targetPos + new Vector3(0f, offsetY, zOffset - 0.05f);
    }

    private PlayerCombatContext context;
    private HealthBase currentTarget;
    private ZapperState currentState = ZapperState.Wander;

    private static readonly HashSet<HealthBase> attachedEnemies = new HashSet<HealthBase>();
    private static bool isChaining = false;

    private float attachTimer;
    private LayerMask enemyMask;

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

    public override void Init(PlayerStats playerStats)
    {
        base.Init(playerStats);
        context = playerStats.GetComponent<PlayerCombatContext>();
        context.OnEntityKilled += OnEntityKilled;
    }

    protected override void Awake()
    {
        base.Awake();

        enemyMask = 1 << LayerMask.NameToLayer("Enemy");

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (zapperCollider == null)
            zapperCollider = GetComponent<Collider>();
    }

    protected override void Update()
    {
        if (health.IsDead) return;

        base.Update();

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
        var enemies = CombatUtility.FindAround<HealthBase>(transform.position, searchRadius, enemyMask);
        HealthBase target = FindUnattachedEnemy(enemies);

        if (target != null)
        {
            wander.Reset(movement);
            currentTarget = target;
            currentState = ZapperState.Chase;
            return;
        }

        wander.Tick(transform, playerStats?.transform, movement);
    }

    private void TickChase()
    {
        if (currentTarget == null || currentTarget.IsDead || attachedEnemies.Contains(currentTarget))
        {
            currentTarget = null;
            currentState = ZapperState.Wander;
            return;
        }

        float dist = Vector3.Distance(transform.position, currentTarget.transform.position);
        if (dist <= attachRange)
        {
            Attach();
            return;
        }

        movement.MoveToTarget(currentTarget.transform.position);
    }

    private void TickAttached()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            Detach();
            return;
        }

        transform.position = GetAttachPosition(currentTarget.transform.position);

        attachTimer -= Time.deltaTime;
        if (attachTimer <= 0f)
            Detach();
    }

    private void Attach()
    {
        currentState = ZapperState.Attached;
        movement.SetCanMove(false);

        transform.position = GetAttachPosition(currentTarget.transform.position);

        if (zapperCollider != null)
            zapperCollider.enabled = false;

        gameObject.layer = LayerMask.NameToLayer("Hidden");
        health.IsInvincible = true;

        var sr = animator.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 0.6f;
            sr.color = c;
        }

        var entityStats = currentTarget.GetComponent<EntityStats>();
        if (entityStats != null)
            entityStats.AddMultiplierModifier(new EntityStatModifier { damageTaken = damageAmpPercent });

        currentTarget.OnDamageReceived += OnHostDamaged;
        attachedEnemies.Add(currentTarget);
        attachTimer = attachDuration;
        animator.SetTrigger("Attach");
    }

    private void OnEntityKilled(HealthBase entity)
    {
        if (entity == currentTarget)
            Detach();
    }

    private void OnHostDamaged(float damage, bool isCrit)
    {
        if (currentTarget == null) return;

        if (isChaining)
        {
            if (shockedVFXPrefab != null)
            {
                Vector3 vfxPos = GetAttachPosition(currentTarget.transform.position);
                vfxPos.z -= 0.05f;
                Instantiate(shockedVFXPrefab, vfxPos, Quaternion.identity);
            }
            return;
        }

        isChaining = true;
        float chainDamage = damage * chainPercent;

        var targets = new List<HealthBase>(attachedEnemies);
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.IsDead || enemy == currentTarget) continue;
            enemy.TakeDamage(chainDamage);
        }
        isChaining = false;
    }

    private void Detach()
    {
        if (currentTarget != null)
        {
            var entityStats = currentTarget.GetComponent<EntityStats>();
            if (entityStats != null)
                entityStats.RemoveMultiplierModifier(new EntityStatModifier { damageTaken = damageAmpPercent });

            currentTarget.OnDamageReceived -= OnHostDamaged;
            attachedEnemies.Remove(currentTarget);
            currentTarget = null;
        }

        if (zapperCollider != null)
            zapperCollider.enabled = true;

        health.IsInvincible = false;
        gameObject.layer = LayerMask.NameToLayer("Summoner");

        var sr = animator.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = 1f;
            sr.color = c;
        }

        context.OnEntityKilled -= OnEntityKilled;
        DieWithoutAnimation();
    }

    private HealthBase FindUnattachedEnemy(List<HealthBase> enemies)
    {
        HealthBase closest = null;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, searchRadius);

        if (playerStats != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(playerStats.transform.position, 3f);
        }
    }
}