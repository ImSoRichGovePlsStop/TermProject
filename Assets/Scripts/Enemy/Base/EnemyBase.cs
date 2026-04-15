using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyHealthBase))]
[RequireComponent(typeof(EnemyMovementBase))]
[RequireComponent(typeof(EntityStats))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] protected float detectRange = 6f;
    [SerializeField] protected float loseTargetRange = 10f;

    [Header("Attack")]
    [SerializeField] protected float postAttackDelayMin = 0.4f;
    [SerializeField] protected float postAttackDelayMax = 0.8f;

    protected bool isPostAttackDelay = false;
    protected float postAttackTimer = 0f;

    [Header("Target Priority")]
    [SerializeField] protected TargetPriority targetPriority;

    [Header("Wander")]
    [SerializeField] protected WanderBehavior wander;

    [Header("References")]
    [SerializeField] protected EnemyHealthBase health;
    [SerializeField] protected EnemyMovementBase movement;
    [SerializeField] protected EntityStats stats;
    [SerializeField] protected Animator animator;

    [Header("Coin Drop")]
    [SerializeField] public int coinDropMin = 4;
    [SerializeField] public int coinDropMax = 9;

    [Header("Hurt")]
    [SerializeField] private float hurtDuration = 0.2f;
    [SerializeField] private float postHurtDelayMin = 0.1f;
    [SerializeField] private float postHurtDelayMax = 0.3f;

    [Header("Spawn Animation")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnRiseDistance = 1.5f;
    [SerializeField] private float spawnDuration = 1f;
    [SerializeField] private float spawnFadeOutDuration = 0.4f;
    [SerializeField] private float spawnEffectScale = 1f;
    [SerializeField] private float spawnStayDuration = 0.5f;

    protected bool skipSpawnEffect = false;

    protected bool isDead;
    protected bool isHurting = false;
    protected bool isSpawning = false;

    public virtual bool CanBeInterrupted() => true;

    private Coroutine hurtCoroutine;

    protected virtual void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealthBase>();
        if (movement == null) movement = GetComponent<EnemyMovementBase>();
        if (stats == null) stats = GetComponent<EntityStats>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Start()
    {
        if (spawnEffectPrefab != null || skipSpawnEffect)
            StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;
        health.IsInvincible = true;

        NavMeshAgent agent = movement.GetAgent();

        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return null;
        Vector3 finalPos = transform.position;

        agent.enabled = false;

        Vector3 startPos = finalPos + Vector3.down * spawnRiseDistance;
        transform.position = startPos;

        if (spriteRenderer != null) spriteRenderer.enabled = true;

        EnemySpawnEffect effect = null;
        if (!skipSpawnEffect && spawnEffectPrefab != null)
        {
            GameObject effectGO = Instantiate(spawnEffectPrefab, finalPos, Quaternion.identity);
            effectGO.transform.localScale *= spawnEffectScale;
            effect = effectGO.GetComponent<EnemySpawnEffect>();
            effect.Init();
            effect.PlayFadeIn(spawnDuration);
        }

        float t = 0f;
        while (t < spawnDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.SmoothStep(0f, 1f, t / spawnDuration);
            transform.position = Vector3.Lerp(startPos, finalPos, ratio);
            yield return null;
        }
        transform.position = finalPos;

        effect?.PlayFadeOut(spawnFadeOutDuration);

        agent.enabled = true;
        agent.Warp(finalPos);

        yield return new WaitForSeconds(spawnStayDuration);

        health.IsInvincible = false;
        movement.SetCanMove(true);
        isSpawning = false;

        OnSpawnComplete();
    }

    protected virtual void OnSpawnComplete() { }

    protected virtual void Update()
    {
        if (isDead) return;
        if (isSpawning) return;
        if (isHurting) { movement.StopMoving(); return; }

        if (isPostAttackDelay)
        {
            movement.StopMoving();
            postAttackTimer -= Time.deltaTime;
            if (postAttackTimer <= 0f)
                isPostAttackDelay = false;
            return;
        }

        UpdateTarget();
        UpdateState();
        TickState();
    }

    public void TriggerHurt()
    {
        if (isDead) return;
        if (hurtCoroutine != null) StopCoroutine(hurtCoroutine);
        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        isHurting = true;
        OnHurtTriggered();
        animator?.SetBool("IsHurting", true);
        yield return new WaitForSeconds(hurtDuration);
        isHurting = false;
        animator?.SetBool("IsHurting", false);
        hurtCoroutine = null;
        TriggerPostHurtDelay();
    }

    protected virtual void OnHurtTriggered() { }

    protected PlayerStats playerTarget;
    protected HealthBase entityTarget;

    protected bool HasTarget => (playerTarget != null && !playerTarget.IsDead)
                              || (entityTarget != null && !entityTarget.IsDead);

    protected Vector3 TargetPosition
    {
        get
        {
            if (playerTarget != null) return playerTarget.transform.position;
            if (entityTarget != null) return entityTarget.transform.position;
            return transform.position;
        }
    }

    protected void TriggerPostAttackDelay()
    {
        isPostAttackDelay = true;
        postAttackTimer = Random.Range(postAttackDelayMin, postAttackDelayMax);
    }

    protected void TriggerPostHurtDelay()
    {
        isPostAttackDelay = true;
        postAttackTimer = Random.Range(postHurtDelayMin, postHurtDelayMax);
    }

    private void UpdateTarget()
    {
        if (playerTarget != null && playerTarget.IsDead)
            playerTarget = null;
        if (entityTarget != null && entityTarget.IsDead)
            entityTarget = null;

        FindBestTarget();
    }

    private void FindBestTarget()
    {
        float scanRange = HasTarget ? loseTargetRange : detectRange;

        playerTarget = null;
        entityTarget = null;

        float bestScore = -1f;

        LayerMask targetMask = (1 << LayerMask.NameToLayer("Player"))
                             | (1 << LayerMask.NameToLayer("Summoner"))
                             | (1 << LayerMask.NameToLayer("Totem"));

        var colliders = Physics.OverlapSphere(transform.position, scanRange, targetMask);
        var checkedEntities = new HashSet<HealthBase>();

        foreach (var col in colliders)
        {
            float dist = Vector3.Distance(transform.position, col.transform.position);
            float score;

            var ps = col.GetComponent<PlayerStats>();
            if (ps == null) ps = col.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead)
            {
                score = targetPriority.playerPriority / Mathf.Max(dist, 0.1f);
                if (score > bestScore)
                {
                    bestScore = score;
                    playerTarget = ps;
                    entityTarget = null;
                }
                continue;
            }

            var hb = col.GetComponentInParent<HealthBase>();
            if (hb == null || hb.IsDead || hb == health) continue;
            if (checkedEntities.Contains(hb)) continue;
            checkedEntities.Add(hb);

            float priority = targetPriority.GetPriority(hb);
            if (priority <= 0f) continue;

            score = priority / Mathf.Max(dist, 0.1f);
            if (score > bestScore)
            {
                bestScore = score;
                entityTarget = hb;
                playerTarget = null;
            }
        }
    }

    protected abstract void UpdateState();
    protected abstract void TickState();

    public virtual void OnDeath()
    {
        isDead = true;
        playerTarget = null;
        entityTarget = null;
        movement.SetCanMove(false);

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }
        isHurting = false;
        animator?.SetBool("IsHurting", false);
        animator?.SetTrigger("Die");
    }

    protected IEnumerator DashRoutine(Vector3 dir, float speed, float duration, float hitRadius, float damageScale, System.Action onDashEnd)
    {
        var agent = movement.GetAgent();
        if (agent != null && agent.isOnNavMesh) agent.enabled = false;

        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        LayerMask barrierMask = 1 << LayerMask.NameToLayer("Barrier");
        LayerMask groundMask = 1 << LayerMask.NameToLayer("Ground");
        var alreadyHit = new HashSet<GameObject>();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float stepDist = speed * stats.MoveSpeedRatio * Time.deltaTime;
            if (Physics.Raycast(transform.position, dir, stepDist + 0.1f, wallMask))
                break;

            if (Physics.Raycast(transform.position, dir, stepDist + 0.1f, barrierMask))
            {
                float remaining = (duration - elapsed);
                Vector3 landingPos = transform.position + dir * (speed * stats.MoveSpeedRatio * remaining);
                if (!Physics.Raycast(landingPos + Vector3.up, Vector3.down, 2f, groundMask))
                    break;
            }

            transform.position += dir * stepDist;
            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);
                var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (ps != null && !ps.IsDead) { ps.TakeDamage(stats.Damage * damageScale, health); continue; }
                var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
                if (hb != null && !hb.IsDead && hb != health) hb.TakeDamage(stats.Damage * damageScale);
            }
            yield return null;
        }

        if (agent != null && !agent.enabled) agent.enabled = true;
        onDashEnd?.Invoke();
    }

    protected void DealDamageToTarget(float damage, float angle, float range)
    {
        Vector3 attackDir = TargetPosition - transform.position;
        attackDir.y = 0f;
        if (attackDir.sqrMagnitude > 0.001f)
            attackDir.Normalize();

        Collider[] hits = Physics.OverlapSphere(transform.position, range);
        foreach (var hit in hits)
        {
            Vector3 dir = hit.transform.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                float a = Vector3.Angle(attackDir, dir);
                if (a > angle * 0.5f) continue;
            }

            var ps = hit.GetComponent<PlayerStats>();
            if (ps == null) ps = hit.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead)
            {
                ps.TakeDamage(damage, health);
                continue;
            }

            var hb = hit.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead && hb != health)
            {
                hb.TakeDamage(damage);
            }
        }
    }

    protected void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}