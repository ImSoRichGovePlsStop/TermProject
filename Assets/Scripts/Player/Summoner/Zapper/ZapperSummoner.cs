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
        Attached,
        AttachedToPlayer
    }

    [Header("References")]
    [SerializeField] private Collider zapperCollider;
    [SerializeField] private GameObject shockedVFXPrefab;
    [SerializeField] private WanderBehavior wander;
    [SerializeField] private Animator animator;

    [Header("Attach")]
    [SerializeField] private float attachDuration = 4f;
    [SerializeField] private float attachRange = 1f;

    [Header("Debuff (On Attach)")]
    [SerializeField] private float damageAmpPercent = 0.1f;
    [SerializeField] private float chainPercent = 0.3f;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.05f;
    [SerializeField] private float speedScale = 0.1f;

    [Header("Cardiac Arrest")]
    [SerializeField] private float cardiacArrestHpThreshold = 0.25f;

    [Header("Arc Pulse")]
    [SerializeField] private float arcPulseInterval = 0.5f;
    [SerializeField] private float arcPulseDamageScale = 0.05f;
    [SerializeField] private int arcPulseTargets = 2;
    [SerializeField] private float arcPulseRadius = 2f;

    [Header("Lightning Rod")]
    [SerializeField] private float lightningRodDamagePenalty = 0.3f;

    [Header("Mini Tier")]
    [SerializeField] private StatScale miniStatScale = new StatScale { hp = 0.4f, damage = 0.4f, moveSpeed = 0.7f };
    [SerializeField] private float miniAttachRangeMult = 0.7f;
    [SerializeField] private float miniAttachDurationMult = 0.7f;
    [SerializeField] private float miniDamageAmpMult = 0.6f;
    [SerializeField] private float miniChainPercentMult = 0.6f;
    [SerializeField] private float miniSizeMult = 0.5f;
    [SerializeField] private float miniLightningRodPenaltyMult = 0.5f;
    [SerializeField] private float miniArcPulseDamageMult = 0.4f;
    [SerializeField] private float miniArcPulseRadiusMult = 0.6f;
    [SerializeField] private float miniInstantKillThresholdMult = 0.5f;

    [Header("Elite Tier")]
    [SerializeField] private StatScale eliteStatScale = new StatScale { hp = 3f, damage = 2f, moveSpeed = 1.2f };
    [SerializeField] private float eliteAttachRangeMult = 1.5f;
    [SerializeField] private float eliteAttachDurationMult = 1.5f;
    [SerializeField] private float eliteDamageAmpMult = 2f;
    [SerializeField] private float eliteChainPercentMult = 1.5f;
    [SerializeField] private float eliteSizeMult = 1.5f;
    [SerializeField] private float eliteLightningRodPenaltyMult = 2f;
    [SerializeField] private float eliteArcPulseDamageMult = 2f;
    [SerializeField] private float eliteArcPulseRadiusMult = 1.5f;
    [SerializeField] private int eliteArcPulseTargets = 4;
    [SerializeField] private float eliteInstantKillThresholdMult = 1.5f;

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

    private PlayerStats.ShieldInstance playerShield;
    private float playerArcPulseTimer = 0f;

    // key = enemy, value = attached ZapperSummoner
    public static readonly Dictionary<HealthBase, ZapperSummoner> attachedZappers = new Dictionary<HealthBase, ZapperSummoner>();
    public static readonly HashSet<HealthBase> attachedEnemies = new HashSet<HealthBase>(); // kept for chain logic
    public static bool playerHasEliteZapper = false;
    private static bool isChaining = false;

    private float attachTimer;
    private float arcPulseTimer = 0f;
    private LayerMask enemyMask;

    public float attachDurationBonus = 0f;
    public float chainPercentBonus = 0f;
    public bool cardiacArrest = false;
    public bool arcPulse = false;
    public bool lightningRod = false;
    public float arcPulseTierMult = 1f;
    public float instantKillThresholdTierMult = 1f;
    public float lightningRodPenaltyTierMult = 1f;

    private EntityStatModifier lightningRodModifier = new EntityStatModifier();

    protected override void ApplyPlayerScaling()
    {
        if (playerStats == null) return;
        stats.AddFlatModifier(new EntityStatModifier
        {
            maxHP = playerStats.MaxHealth * (hpScale + hpScaleBonus),
            moveSpeed = playerStats.MoveSpeed * (speedScale + speedScaleBonus)
        });

        attachDuration += attachDurationBonus;
        chainPercent += chainPercentBonus;

        switch (tier)
        {
            case SummonerTier.Mini:
                stats.SetStatScale(miniStatScale);
                attachRange *= miniAttachRangeMult;
                attachDuration *= miniAttachDurationMult;
                damageAmpPercent *= miniDamageAmpMult;
                chainPercent *= miniChainPercentMult;
                transform.localScale *= miniSizeMult;
                arcPulseTierMult = miniArcPulseDamageMult;
                lightningRodPenaltyTierMult = miniLightningRodPenaltyMult;
                instantKillThresholdTierMult = miniInstantKillThresholdMult;
                arcPulseRadius *= miniArcPulseRadiusMult;
                break;
            case SummonerTier.Elite:
                stats.SetStatScale(eliteStatScale);
                attachRange *= eliteAttachRangeMult;
                attachDuration *= eliteAttachDurationMult;
                damageAmpPercent *= eliteDamageAmpMult;
                chainPercent *= eliteChainPercentMult;
                transform.localScale *= eliteSizeMult;
                arcPulseTierMult = eliteArcPulseDamageMult;
                lightningRodPenaltyTierMult = eliteLightningRodPenaltyMult;
                instantKillThresholdTierMult = eliteInstantKillThresholdMult;
                arcPulseRadius *= eliteArcPulseRadiusMult;
                arcPulseTargets = eliteArcPulseTargets;
                break;
        }

        health.SetMaxHP(stats.MaxHP);
    }

    public override void Init(PlayerStats playerStats, SummonerTier tier = SummonerTier.Normal)
    {
        base.Init(playerStats, tier);
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

        if (arcPulse && currentState != ZapperState.AttachedToPlayer)
        {
            arcPulseTimer -= Time.deltaTime;
            if (arcPulseTimer <= 0f)
            {
                arcPulseTimer = arcPulseInterval;
                DoArcPulse();
            }
        }

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

            case ZapperState.AttachedToPlayer:
                TickAttachedToPlayer();
                break;
        }
    }

    protected override void TickLifetime()
    {
        if (currentState == ZapperState.Attached) return;
        if (currentState == ZapperState.AttachedToPlayer) return;
        base.TickLifetime();
    }

    private void TickWander()
    {
        // Elite: player first
        if (tier == SummonerTier.Elite && !playerHasEliteZapper && playerStats != null && !playerStats.IsDead)
        {
            wander.Reset(movement, stats);
            currentTarget = null;
            currentState = ZapperState.Chase;
            return;
        }

        HealthBase target = FindBestTarget();
        if (target != null)
        {
            wander.Reset(movement, stats);
            currentTarget = target;
            currentState = ZapperState.Chase;
            return;
        }

        if (tier == SummonerTier.Elite && playerStats != null)
            movement.MoveToTarget(playerStats.transform.position);
        else
            wander.Tick(transform, playerStats?.transform, movement, stats);
    }

    private HealthBase FindBestTarget()
    {
        var enemies = CombatUtility.FindAround<HealthBase>(transform.position, searchRadius, enemyMask);

        // Priority: Enemy with no Zapper attached
        HealthBase unattachedNearest = null;
        float unattachedDist = Mathf.Infinity;
        foreach (var e in enemies)
        {
            if (e == null || e.IsDead || attachedZappers.ContainsKey(e)) continue;
            float d = Vector3.Distance(transform.position, e.transform.position);
            if (d < unattachedDist) { unattachedDist = d; unattachedNearest = e; }
        }
        if (unattachedNearest != null) return unattachedNearest;

        if (tier == SummonerTier.Mini) return null;

        // Normal/Elite: Enemy with Mini Zapper - least duration first
        HealthBase miniTarget = FindAttachedByTierLeastDuration(enemies, SummonerTier.Mini);
        if (miniTarget != null) return miniTarget;

        // Elite only: Enemy with Normal Zapper - least duration first
        if (tier == SummonerTier.Elite)
        {
            HealthBase normalTarget = FindAttachedByTierLeastDuration(enemies, SummonerTier.Normal);
            if (normalTarget != null) return normalTarget;
        }

        return null;
    }

    private HealthBase FindAttachedByTierLeastDuration(List<HealthBase> enemies, SummonerTier targetTier)
    {
        HealthBase best = null;
        float lowestTimer = Mathf.Infinity;
        foreach (var e in enemies)
        {
            if (e == null || e.IsDead) continue;
            if (!attachedZappers.TryGetValue(e, out var zapper)) continue;
            if (zapper.tier != targetTier) continue;
            if (zapper.attachTimer < lowestTimer) { lowestTimer = zapper.attachTimer; best = e; }
        }
        return best;
    }

    private void TickChase()
    {
        // Elite targets player first
        if (tier == SummonerTier.Elite && !playerHasEliteZapper)
        {
            if (playerStats == null || playerStats.IsDead) { currentState = ZapperState.Wander; return; }
            Vector3 flatTarget = playerStats.transform.position;
            flatTarget.y = transform.position.y;
            float dist = Vector3.Distance(transform.position, flatTarget);
            if (dist <= attachRange) { AttachToPlayer(); return; }
            movement.MoveToTarget(playerStats.transform.position);
            return;
        }

        if (currentTarget == null || currentTarget.IsDead)
        {
            currentTarget = null;
            currentState = ZapperState.Wander;
            return;
        }

        // Re-evaluate: check if target is still worth chasing
        if (attachedZappers.TryGetValue(currentTarget, out var existing))
        {
            bool canReplace = false;
            if (tier == SummonerTier.Normal && existing.tier == SummonerTier.Mini) canReplace = true;
            if (tier == SummonerTier.Elite && (existing.tier == SummonerTier.Mini || existing.tier == SummonerTier.Normal)) canReplace = true;

            if (!canReplace)
            {
                currentTarget = null;
                currentState = ZapperState.Wander;
                return;
            }
        }

        Vector3 flatPos = currentTarget.transform.position;
        flatPos.y = transform.position.y;
        float flatDist = Vector3.Distance(transform.position, flatPos);
        float heightDiff = Mathf.Abs(currentTarget.transform.position.y - transform.position.y);
        if (heightDiff > maxHeightDiff) { currentState = ZapperState.Wander; return; }

        if (flatDist <= attachRange)
        {
            // Force detach existing zapper if needed
            if (attachedZappers.TryGetValue(currentTarget, out var existingZapper))
                existingZapper.ForceDetach();
            Attach();
            return;
        }

        movement.MoveToTarget(currentTarget.transform.position);
    }

    private void AttachToPlayer()
    {
        currentState = ZapperState.AttachedToPlayer;
        movement.SetCanMove(false);

        if (zapperCollider != null) zapperCollider.enabled = false;
        gameObject.layer = LayerMask.NameToLayer("Hidden");
        health.IsInvincible = true;

        // Convert HP to shield
        playerShield = playerStats.GainShield(health.CurrentHP, attachDuration);

        // Subscribe to player damage for chain
        playerStats.OnPlayerDamaged += OnPlayerDamagedChain;
        playerStats.OnPlayerDeath += DetachFromPlayer;
        if (cardiacArrest)
            context.OnGetHit += OnPlayerHitCardiacArrest;
        playerHasEliteZapper = true;

        attachTimer = attachDuration;
        animator.SetTrigger("Attach");
    }

    private void TickAttachedToPlayer()
    {
        if (playerStats == null || playerStats.IsDead) { DetachFromPlayer(); return; }

        transform.position = playerStats.transform.position;

        // Arc Pulse on player
        if (arcPulse)
        {
            playerArcPulseTimer -= Time.deltaTime;
            if (playerArcPulseTimer <= 0f)
            {
                playerArcPulseTimer = arcPulseInterval;
                DoArcPulse();
            }
        }

        attachTimer -= Time.deltaTime;
        if (attachTimer <= 0f)
            DetachFromPlayer();
    }

    private void OnPlayerDamagedChain(float damage)
    {
        // Chain to attached enemies
        if (isChaining) return;
        isChaining = true;
        float chainDamage = damage * chainPercent;
        var targets = new List<HealthBase>(attachedEnemies);
        foreach (var enemy in targets)
        {
            if (enemy == null || enemy.IsDead) continue;
            enemy.TakeDamage(chainDamage);
        }
        isChaining = false;
    }

    private void OnPlayerHitCardiacArrest()
    {
        var attacker = context.LastAttacker;
        Debug.Log($"[CardiacArrest] LastAttacker = {(attacker == null ? "NULL" : attacker.name)}, IsDead = {attacker?.IsDead}");
        if (attacker == null || attacker.IsDead) return;

        var enemyHealth = attacker.GetComponent<EnemyHealthBase>();
        Debug.Log($"[CardiacArrest] EnemyHealthBase = {(enemyHealth == null ? "NULL" : enemyHealth.name)}, Tier = {enemyHealth?.Tier}");
        if (enemyHealth == null) return;
        if (enemyHealth.Tier == EnemyTier.Boss || enemyHealth.Tier == EnemyTier.Miniboss) return;

        float hpRatio = attacker.CurrentHP / attacker.MaxHP;
        float threshold = cardiacArrestHpThreshold * instantKillThresholdTierMult;
        Debug.Log($"[CardiacArrest] HP = {attacker.CurrentHP}/{attacker.MaxHP} ({hpRatio:P0}), threshold = {threshold:P0}, will kill = {hpRatio < threshold}");
        if (hpRatio < threshold)
            attacker.TakeDamage(attacker.CurrentHP);
    }

    private void DetachFromPlayer()
    {
        playerStats.OnPlayerDamaged -= OnPlayerDamagedChain;
        playerStats.OnPlayerDeath -= DetachFromPlayer;
        if (cardiacArrest)
            context.OnGetHit -= OnPlayerHitCardiacArrest;
        playerHasEliteZapper = false;

        if (playerShield != null)
        {
            // Shield naturally expires or was consumed
            playerShield = null;
        }

        if (zapperCollider != null) zapperCollider.enabled = true;
        health.IsInvincible = false;
        gameObject.layer = LayerMask.NameToLayer("Summoner");

        if (context != null)
            context.OnEntityKilled -= OnEntityKilled;
        DieWithoutAnimation();
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
        {
            entityStats.AddMultiplierModifier(new EntityStatModifier { damageTaken = damageAmpPercent });
            if (lightningRod)
            {
                lightningRodModifier.damage = -lightningRodDamagePenalty * lightningRodPenaltyTierMult;
                entityStats.AddMultiplierModifier(lightningRodModifier);
            }
        }

        currentTarget.OnDamageReceived += OnHostDamaged;
        attachedEnemies.Add(currentTarget);
        attachedZappers[currentTarget] = this;
        attachTimer = attachDuration;
        animator.SetTrigger("Attach");
    }

    public static event System.Action<HealthBase> OnAttachedEnemyKilled;

    private void OnEntityKilled(HealthBase entity)
    {
        if (this == null || gameObject == null) return;
        if (entity == currentTarget)
        {
            OnAttachedEnemyKilled?.Invoke(entity);
            Detach();
        }
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

    public void ForceDetach()
    {
        Detach();
    }

    private void Detach()
    {
        if (this == null || gameObject == null) return;
        if (currentTarget != null)
        {
            var entityStats = currentTarget.GetComponent<EntityStats>();
            if (entityStats != null)
            {
                entityStats.RemoveMultiplierModifier(new EntityStatModifier { damageTaken = damageAmpPercent });
                if (lightningRod)
                    entityStats.RemoveMultiplierModifier(lightningRodModifier);
            }

            if (cardiacArrest && !currentTarget.IsDead)
            {
                var enemyHealth = currentTarget.GetComponent<EnemyHealthBase>();
                if (enemyHealth != null &&
                    enemyHealth.Tier != EnemyTier.Miniboss &&
                    enemyHealth.Tier != EnemyTier.Boss)
                {
                    float hpRatio = currentTarget.CurrentHP / currentTarget.MaxHP;
                    if (hpRatio < cardiacArrestHpThreshold * instantKillThresholdTierMult)
                    {
                        var target = currentTarget;
                        float dmg = target.CurrentHP;
                        currentTarget.OnDamageReceived -= OnHostDamaged;
                        attachedEnemies.Remove(currentTarget);
                        attachedZappers.Remove(currentTarget);
                        currentTarget = null;
                        target.TakeDamage(dmg);
                        goto cleanup;
                    }
                }
            }

            currentTarget.OnDamageReceived -= OnHostDamaged;
            attachedEnemies.Remove(currentTarget);
            attachedZappers.Remove(currentTarget);
            currentTarget = null;

        cleanup:;
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

        if (context != null)
            context.OnEntityKilled -= OnEntityKilled;
        DieWithoutAnimation();
    }

    private void DoArcPulse()
    {
        if (playerStats == null) return;
        if (isChaining) return;

        isChaining = true;
        float damage = playerStats.Damage * arcPulseDamageScale * arcPulseTierMult;

        var hits = CombatUtility.FindAround<HealthBase>(transform.position, arcPulseRadius, enemyMask);
        int count = 0;
        foreach (var h in hits)
        {
            if (h == null || h.IsDead) continue;
            if (h == currentTarget) continue;
            if (count >= arcPulseTargets) break;
            h.TakeDamage(damage);
            count++;
        }
        isChaining = false;
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