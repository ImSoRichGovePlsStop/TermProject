using System.Collections;
using System.Collections.Generic;
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
        Detonating,
        Launched,
        Pulling
    }

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float baseExplosionRadius = 2f;
    [SerializeField] private float centerRadius = 0.8f;
    [SerializeField] private float centerDamageMultiplier = 1.5f;
    [SerializeField] private float edgeDamageMultiplier = 1f;
    [SerializeField] private float damageScale = 1f;
    [SerializeField] private float triggerRange = 0.8f;

    [Header("Wander")]
    [SerializeField] private WanderBehavior wander;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.05f;
    [SerializeField] private float speedScale = 0.1f;

    [System.NonSerialized] public bool volatileBody = false;
    [System.NonSerialized] public float explosionDamageMult = 1f;
    [System.NonSerialized] public float explosionRadiusMult = 1f;
    [System.NonSerialized] public float damageScaleBonus = 0f;
    [System.NonSerialized] public bool lightningRod = false;
    [System.NonSerialized] public bool unstableCharge = false;
    [System.NonSerialized] public bool scorchedEarth = false;
    [System.NonSerialized] public bool shrapnel = false;
    [System.NonSerialized] public bool singularity = false;

    [Header("Singularity")]
    [SerializeField] private float pullRadius = 5f;
    [SerializeField] private float pullDuration = 0.5f;
    [SerializeField] private float pullDistanceFactor = 0.33f;
    [SerializeField] private float pullSlowPercent = 0.4f;
    [SerializeField] private float pullSlowDuration = 3f;

    [Header("Unstable Charge")]
    [SerializeField] private float unstableChargePerSecond = 0.08f;
    [SerializeField] private float unstableChargeMax = 0.4f;

    [Header("Scorched Earth")]
    [SerializeField] private GameObject fireFieldPrefab;
    [SerializeField] private float fireTrailInterval = 0.5f;
    [SerializeField] private float fireTrailDuration = 2f;
    [SerializeField] private float fireFieldDuration = 4f;
    [SerializeField] private float fireFieldDamagePercent = 0.2f;

    [Header("Shrapnel")]
    [SerializeField] private float shrapnelLaunchSpeedMult = 3f;
    [SerializeField] private int shrapnelCount = 6;
    [SerializeField] private float shrapnelDistance = 3f;

    [Header("Mini Tier")]
    [SerializeField] private StatScale miniStatScale = new StatScale { hp = 0.4f, damage = 0.4f, moveSpeed = 0.7f };
    [SerializeField] private float miniRadiusMult = 0.6f;
    [SerializeField] private float miniTriggerRangeMult = 0.7f;
    [SerializeField] private float miniFireTrailDurationMult = 0.6f;
    [SerializeField] private float miniFireFieldDurationMult = 0.6f;
    [SerializeField] private float miniSizeMult = 0.5f;

    [Header("Elite Tier")]
    [SerializeField] private StatScale eliteStatScale = new StatScale { hp = 3f, damage = 2f, moveSpeed = 1.2f };
    [SerializeField] private float eliteRadiusMult = 1.5f;
    [SerializeField] private float eliteTriggerRangeMult = 1.3f;
    [SerializeField] private float eliteFireTrailDurationMult = 1.5f;
    [SerializeField] private float eliteFireFieldDurationMult = 1.5f;
    [SerializeField] private int eliteShrapnelCount = 8;
    [SerializeField] private float eliteShrapnelDistance = 4f;
    [SerializeField] private float eliteSizeMult = 1.5f;

    private float aliveTimer = 0f;
    private float fireTrailTimer = 0f;
    private Vector3 launchTarget;

    public int GetShrapnelCount(SummonerTier t) => t == SummonerTier.Elite ? eliteShrapnelCount : shrapnelCount;
    public float GetShrapnelDistance(SummonerTier t) => t == SummonerTier.Elite ? eliteShrapnelDistance : shrapnelDistance;

    public void Launch(Vector3 target)
    {
        launchTarget = target;
        currentState = BomberState.Launched;
        stats.AddMultiplierModifier(new EntityStatModifier { moveSpeed = shrapnelLaunchSpeedMult - 1f });
    }

    private HealthBase currentTarget;
    private BomberState currentState = BomberState.Wander;
    private LayerMask enemyMask;

    public override void Init(PlayerStats playerStats, SummonerTier tier = SummonerTier.Normal)
    {
        base.Init(playerStats, tier);
        health.OnDeath += OnBomberDeath;
    }

    private void OnBomberDeath()
    {
        if (volatileBody && currentState != BomberState.Detonating)
        {
            health.PreventDeath = true;
            Detonate();
        }
    }

    protected override void ApplyPlayerScaling()
    {
        if (playerStats == null) return;
        stats.AddFlatModifier(new EntityStatModifier
        {
            maxHP = playerStats.MaxHealth * (hpScale + hpScaleBonus),
            moveSpeed = playerStats.MoveSpeed * (speedScale + speedScaleBonus),
            damage = playerStats.Damage * (damageScale + damageScaleBonus)
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

        explosionRadius *= explosionRadiusMult;
        centerRadius *= explosionRadiusMult;

        switch (tier)
        {
            case SummonerTier.Mini:
                fireTrailDuration *= miniFireTrailDurationMult;
                fireFieldDuration *= miniFireFieldDurationMult;
                break;
            case SummonerTier.Elite:
                fireTrailDuration *= eliteFireTrailDurationMult;
                fireFieldDuration *= eliteFireFieldDurationMult;
                break;
        }
    }

    protected override void TickLifetime()
    {
        if (currentState == BomberState.Detonating) return;
        if (currentState == BomberState.Pulling) return;
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

        if (unstableCharge && currentState != BomberState.Detonating)
            aliveTimer += Time.deltaTime;

        if (scorchedEarth && currentState != BomberState.Detonating && fireFieldPrefab != null)
        {
            fireTrailTimer -= Time.deltaTime;
            if (fireTrailTimer <= 0f)
            {
                fireTrailTimer = fireTrailInterval;
                SpawnFireField(centerRadius, fireTrailDuration);
            }
        }

        switch (currentState)
        {
            case BomberState.Wander:
                TickWander();
                break;

            case BomberState.Chase:
                TickChase();
                break;

            case BomberState.Launched:
                TickLaunched();
                break;

            case BomberState.Pulling:
                // handled by coroutine
                break;
        }
    }

    private void TickLaunched()
    {
        Vector3 flatTarget = launchTarget;
        flatTarget.y = transform.position.y;
        float dist = Vector3.Distance(transform.position, flatTarget);

        if (dist <= 0.3f)
        {
            stats.RemoveMultiplierModifier(new EntityStatModifier { moveSpeed = shrapnelLaunchSpeedMult - 1f });
            currentState = BomberState.Wander;
            return;
        }

        movement.MoveToTarget(launchTarget);
    }

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
            Debug.Log($"[Singularity] tier={tier}, singularity={singularity}");
            if (singularity && tier == SummonerTier.Elite)
                StartCoroutine(PullAndDetonate());
            else
                Detonate();
            return;
        }

        movement.MoveToTarget(currentTarget.transform.position);
    }

    private IEnumerator PullAndDetonate()
    {
        currentState = BomberState.Pulling;
        movement.SetCanMove(false);
        health.IsInvincible = true;

        var hits = CombatUtility.FindAround<HealthBase>(transform.position, pullRadius, enemyMask);
        var pullTargets = new List<(HealthBase h, Vector3 start, Vector3 end, EnemyMovementBase mov, EntityStats entityStats)>();

        foreach (var h in hits)
        {
            if (h == null || h.IsDead) continue;
            var mov = h.GetComponent<EnemyMovementBase>();
            if (mov != null) mov.SetCanMove(false);

            float dist = Vector3.Distance(transform.position, h.transform.position);
            Vector3 dir = (h.transform.position - transform.position).normalized;
            Vector3 target = transform.position + dir * (dist * pullDistanceFactor);

            var es = h.GetComponent<EntityStats>();
            if (es != null) es.AddMultiplierModifier(new EntityStatModifier { moveSpeed = -pullSlowPercent });

            pullTargets.Add((h, h.transform.position, target, mov, es));
        }

        float elapsed = 0f;
        while (elapsed < pullDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pullDuration);
            foreach (var (h, start, end, mov, _) in pullTargets)
            {
                if (h == null || h.IsDead) continue;
                Vector3 newPos = Vector3.Lerp(start, end, t);
                var agent = mov?.GetAgent();
                if (agent != null && agent.isOnNavMesh)
                    agent.Warp(newPos);
                else
                    h.transform.position = newPos;
            }
            yield return null;
        }

        foreach (var (h, _, _, mov, es) in pullTargets)
        {
            if (mov != null) mov.SetCanMove(true);
            if (h != null && !h.IsDead && es != null)
                StartCoroutine(RemoveSlowAfter(es, pullSlowDuration));
        }

        Detonate();
    }

    private IEnumerator RemoveSlowAfter(EntityStats entityStats, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (entityStats != null)
            entityStats.RemoveMultiplierModifier(new EntityStatModifier { moveSpeed = -pullSlowPercent });
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

        float unstableBonus = unstableCharge
            ? Mathf.Min(aliveTimer * unstableChargePerSecond, unstableChargeMax)
            : 0f;
        float finalDamageMult = explosionDamageMult + unstableBonus;

        float centerDamage = 0f;

        foreach (var target in hits)
        {
            if (target == null || target.IsDead) continue;

            float dist = Vector3.Distance(
                new Vector3(transform.position.x, 0f, transform.position.z),
                new Vector3(target.transform.position.x, 0f, target.transform.position.z)
            );
            bool isCenter = dist <= centerRadius;

            float damage = isCenter
                ? stats.Damage * centerDamageMultiplier * finalDamageMult
                : stats.Damage * edgeDamageMultiplier * finalDamageMult;

            if (isCenter && centerDamage == 0f)
                centerDamage = damage;

            target.TakeDamage(damage);
        }

        if (scorchedEarth && fireFieldPrefab != null)
        {
            if (centerDamage == 0f)
                centerDamage = stats.Damage * centerDamageMultiplier * finalDamageMult;

            float fireDamagePerTick = centerDamage * fireFieldDamagePercent;
            SpawnFireField(explosionRadius, fireFieldDuration, fireDamagePerTick);
        }

        if (shrapnel && playerStats != null)
            OnExploded?.Invoke(transform.position);
    }

    public event System.Action<Vector3> OnExploded;

    private void SpawnFireField(float fieldRadius, float fieldDuration, float fieldDamage = -1f)
    {
        if (fireFieldPrefab == null) return;

        float dmg = fieldDamage >= 0f ? fieldDamage
            : stats.Damage * centerDamageMultiplier
              * (explosionDamageMult) * fireFieldDamagePercent;

        var go = Instantiate(fireFieldPrefab, new Vector3(transform.position.x, transform.position.y, transform.position.z), Quaternion.identity);
        go.GetComponent<FireField>()?.Init(fieldRadius, dmg, fieldDuration, enemyMask);
    }

    // Animation Event
    public void FinishExplosion()
    {
        health.ForceDestroy();
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