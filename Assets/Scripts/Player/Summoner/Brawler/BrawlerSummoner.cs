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
        Strafe,
        Attack
    }

    [Header("Attack")]
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float damageScale = 0.25f;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Wander")]
    [SerializeField] private WanderBehavior wander;

    [Header("Player Scaling")]
    [SerializeField] private float hpScale = 0.1f;
    [SerializeField] private float speedScale = 0.1f;

    [Header("Mini Tier")]
    [SerializeField] private StatScale miniStatScale = new StatScale { hp = 0.4f, damage = 0.4f, moveSpeed = 0.7f };
    [SerializeField] private float miniAttackRangeMult = 0.7f;
    [SerializeField] private float miniAttackCooldownMult = 1.3f;
    [SerializeField] private float miniDashSpeedMult = 0.8f;
    [SerializeField] private float miniDashDurationMult = 0.8f;
    [SerializeField] private float miniSizeMult = 0.5f;

    [Header("Elite Tier")]
    [SerializeField] private StatScale eliteStatScale = new StatScale { hp = 3f, damage = 2f, moveSpeed = 1.2f };
    [SerializeField] private float eliteAttackRangeMult = 1.5f;
    [SerializeField] private float eliteAttackCooldownMult = 0.7f;
    [SerializeField] private float eliteDashSpeedMult = 1.3f;
    [SerializeField] private float eliteDashDurationMult = 1.2f;
    [SerializeField] private float eliteSizeMult = 1.5f;

    [Header("Elite Tier VFX")]
    [SerializeField] private GameObject auraPrefab;

    private HealthBase currentTarget;
    private BrawlerState currentState = BrawlerState.Wander;

    [Header("Dash Attack")]
    [SerializeField] private float dashSpeed = 8f;
    [SerializeField] private float dashDuration = 0.25f;
    [SerializeField] private float dashHitRadius = 0.5f;

    private bool isAttacking = false;
    private bool isDashing = false;
    private float lastAttackTime = -Mathf.Infinity;


    private LayerMask _enemyMask;

    [System.NonSerialized] public float damageScaleBonus = 0f;

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
                attackRange *= miniAttackRangeMult;
                attackCooldown *= miniAttackCooldownMult;
                dashSpeed *= miniDashSpeedMult;
                dashDuration *= miniDashDurationMult;
                transform.localScale *= miniSizeMult;
                break;
            case SummonerTier.Elite:
                stats.SetStatScale(eliteStatScale);
                attackRange *= eliteAttackRangeMult;
                attackCooldown *= eliteAttackCooldownMult;
                dashSpeed *= eliteDashSpeedMult;
                dashDuration *= eliteDashDurationMult;
                transform.localScale *= eliteSizeMult;
                break;
        }

        health.SetMaxHP(stats.MaxHP);

        if (tier == SummonerTier.Elite && auraPrefab != null)
        {
            var auraObj = Instantiate(auraPrefab, transform.position, Quaternion.identity);
            var aura = auraObj.GetComponent<EliteBrawlerAura>();
            if (aura != null)
                aura.Init(stats, playerStats, GetComponent<HealthBase>());
        }
    }

    protected override void Awake()
    {
        base.Awake();

        movement.SetStopDistance(attackRange * 0.8f);
        strafe.Init(attackRange);
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

    [System.NonSerialized] public bool lightningRod = false;

    private void UpdateTarget()
    {
        if (lightningRod)
        {
            HealthBase nearest = null;
            float nearestDist = Mathf.Infinity;
            var candidates = CombatUtility.FindAround<HealthBase>(transform.position, searchRadius, _enemyMask);
            foreach (var h in candidates)
            {
                if (h == null || h.IsDead) continue;
                if (!ZapperSummoner.attachedEnemies.Contains(h)) continue;
                float dist = Vector3.Distance(transform.position, h.transform.position);
                if (dist < nearestDist) { nearestDist = dist; nearest = h; }
            }
            if (nearest != null) { currentTarget = nearest; return; }
        }
        currentTarget = CombatUtility.FindNearest<HealthBase>(transform.position, searchRadius, _enemyMask);
    }

    private void UpdateState()
    {
        if (isDashing)
        {
            if (currentTarget == null || currentTarget.IsDead)
            {
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

            bool canAttack = Time.time >= lastAttackTime + attackCooldown / stats.AttackSpeed;
            if (flatDist <= attackRange)
                currentState = canAttack ? BrawlerState.Attack : BrawlerState.Strafe;
            else
            {
                if (currentState == BrawlerState.Strafe) strafe.Reset();
                currentState = BrawlerState.Chase;
            }
        }
        else
        {
            isAttacking = false;
            currentState = BrawlerState.Wander;
        }

        if (prevState == BrawlerState.Wander && currentState != BrawlerState.Wander)
            wander.Reset(movement, stats);
    }

    private void TickState()
    {
        switch (currentState)
        {
            case BrawlerState.Chase:
                movement.MoveToTarget(currentTarget.transform.position);
                break;

            case BrawlerState.Strafe:
                if (movement.GetAgent() is var sa && sa != null) sa.stoppingDistance = 0f;
                strafe.Tick(transform, currentTarget.transform.position, movement);
                break;

            case BrawlerState.Attack:
                movement.FaceTarget(currentTarget.transform.position);
                if (!TryAttack())
                    movement.MoveToTarget(currentTarget.transform.position);
                else
                    movement.StopMoving();
                break;

            case BrawlerState.Wander:
                wander.Tick(transform, playerStats?.transform, movement, stats);
                break;
        }
    }

    private bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + attackCooldown / stats.AttackSpeed) return false;

        isAttacking = true;
        animator.speed = stats.AttackSpeed;
        animator.SetTrigger("Attack");
        return true;
    }

    // Animation Event
    public void DealDamage()
    {
        if (currentTarget == null || currentTarget.IsDead) return;
        Vector3 dir = currentTarget.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) dir.Normalize();
        StartCoroutine(DashAttackRoutine(dir));
    }

    private System.Collections.IEnumerator DashAttackRoutine(Vector3 dir)
    {
        isDashing = true;
        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        LayerMask enemyMask = _enemyMask;
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        var alreadyHit = new System.Collections.Generic.HashSet<GameObject>();

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float stepDist = dashSpeed * Time.deltaTime;
            if (Physics.Raycast(transform.position, dir, stepDist + 0.1f, wallMask)) break;

            transform.position += dir * stepDist;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, dashHitRadius, enemyMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);
                var enemyHealth = col.GetComponentInParent<EnemyHealthBase>();
                if (enemyHealth != null && !enemyHealth.IsDead)
                    enemyHealth.TakeDamage(stats.Damage, null, silent: true);
            }
            yield return null;
        }

        if (agent != null && !agent.enabled) agent.enabled = true;
        isDashing = false;
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        animator.speed = 1f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, dashHitRadius);
    }
}