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

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float baseExplosionRadius = 2f;
    [SerializeField] private float centerRadius = 0.8f;
    [SerializeField] private float centerDamageMultiplier = 1.5f;
    [SerializeField] private float centerDamageScale = 1.5f;
    [SerializeField] private float edgeDamageMultiplier = 1f;
    [SerializeField] private float edgeDamageScale = 1f;
    [SerializeField] private float triggerRange = 0.8f;

    [Header("Wander")]
    [SerializeField] private WanderBehavior wander;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.05f;
    [SerializeField] private float speedScale = 0.1f;

    [Header("Mini Tier")]
    [SerializeField] private StatScale miniStatScale = new StatScale { hp = 0.4f, damage = 0.4f, moveSpeed = 0.7f };
    [SerializeField] private float miniRadiusMult = 0.6f;
    [SerializeField] private float miniTriggerRangeMult = 0.7f;
    [SerializeField] private float miniSizeMult = 0.5f;

    [Header("Elite Tier")]
    [SerializeField] private StatScale eliteStatScale = new StatScale { hp = 3f, damage = 2f, moveSpeed = 1.2f };
    [SerializeField] private float eliteRadiusMult = 1.5f;
    [SerializeField] private float eliteTriggerRangeMult = 1.3f;
    [SerializeField] private float eliteSizeMult = 1.5f;

    private HealthBase currentTarget;
    private BomberState currentState = BomberState.Wander;
    private LayerMask enemyMask;

    public float centerDamageScaleBonus = 0f;
    public float edgeDamageScaleBonus = 0f;

    protected override void ApplyPlayerScaling()
    {
        if (playerStats == null) return;
        stats.AddFlatModifier(new EntityStatModifier
        {
            maxHP = playerStats.MaxHealth * (hpScale + hpScaleBonus),
            moveSpeed = playerStats.MoveSpeed * (speedScale + speedScaleBonus)
        });

        switch (tier)
        {
            case SummonerTier.Mini:
                stats.SetStatScale(miniStatScale);
                explosionRadius *= miniRadiusMult;
                centerRadius *= miniRadiusMult;
                triggerRange *= miniTriggerRangeMult;
                transform.localScale *= miniSizeMult;
                break;
            case SummonerTier.Elite:
                stats.SetStatScale(eliteStatScale);
                explosionRadius *= eliteRadiusMult;
                centerRadius *= eliteRadiusMult;
                triggerRange *= eliteTriggerRangeMult;
                transform.localScale *= eliteSizeMult;
                break;
        }

        health.SetMaxHP(stats.MaxHP);
    }

    protected override void TickLifetime()
    {
        if (currentState == BomberState.Detonating) return;
        base.TickLifetime();
    }

    protected override void Awake()
    {
        base.Awake();

        movement.SetStopDistance(0f);
        enemyMask = 1 << LayerMask.NameToLayer("Enemy");

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

    public bool lightningRod = false;

    private void TickWander()
    {
        if (lightningRod)
        {
            HealthBase nearest = null;
            float nearestDist = Mathf.Infinity;
            var candidates = CombatUtility.FindAround<HealthBase>(transform.position, searchRadius, enemyMask);
            foreach (var h in candidates)
            {
                if (h == null || h.IsDead) continue;
                if (!ZapperSummoner.attachedEnemies.Contains(h)) continue;
                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = h; }
            }
            if (nearest != null)
            {
                currentTarget = nearest;
                wander.Reset(movement, stats);
                currentState = BomberState.Chase;
                return;
            }
        }

        currentTarget = CombatUtility.FindNearest<HealthBase>(transform.position, searchRadius, enemyMask);

        if (currentTarget != null)
        {
            wander.Reset(movement, stats);
            currentState = BomberState.Chase;
            return;
        }

        wander.Tick(transform, playerStats?.transform, movement, stats);
    }

    private void TickChase()
    {
        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            currentState = BomberState.Wander;
            return;
        }

        Vector3 flatTarget = currentTarget.transform.position;
        flatTarget.y = transform.position.y;
        float flatDist = Vector3.Distance(transform.position, flatTarget);
        float heightDiff = Mathf.Abs(currentTarget.transform.position.y - transform.position.y);
        float dist = flatDist;
        if (heightDiff > maxHeightDiff) { currentTarget = null; currentState = BomberState.Wander; return; }
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
        health.IsInvincible = true;

        var billboard = animator.GetComponent<BillboardSprite>();
        if (billboard != null) billboard.isBillboard = false;
        animator.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        animator.SetTrigger("Explode");
    }

    // Animation Event
    public void SetExplosionScale()
    {
        float scale = explosionRadius / baseExplosionRadius;
        animator.transform.localScale = Vector3.one * scale;
    }

    // Animation Event
    public void DealExplosionDamage()
    {
        var hits = CombatUtility.FindAround<HealthBase>(transform.position, explosionRadius, enemyMask);

        foreach (var target in hits)
        {
            if (target == null || target.IsDead) continue;

            float dist = Vector3.Distance(transform.position, target.transform.position);
            bool isCenter = dist <= centerRadius;

            float damage = isCenter
                ? stats.Damage * centerDamageMultiplier + (playerStats != null ? playerStats.Damage * (centerDamageScale + centerDamageScaleBonus) : 0f)
                : stats.Damage * edgeDamageMultiplier + (playerStats != null ? playerStats.Damage * (edgeDamageScale + edgeDamageScaleBonus) : 0f);

            target.TakeDamage(damage);
        }
    }

    // Animation Event
    public void FinishExplosion()
    {
        DieWithoutAnimation();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, centerRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRange);
    }
}