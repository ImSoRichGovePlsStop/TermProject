using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    protected bool isDead;
    protected bool isHurting = false;

    public virtual bool CanBeInterrupted() => true;

    private Coroutine hurtCoroutine;

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

    protected virtual void Awake()
    {
        if (health == null) health = GetComponent<EnemyHealthBase>();
        if (movement == null) movement = GetComponent<EnemyMovementBase>();
        if (stats == null) stats = GetComponent<EntityStats>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    protected virtual void Update()
    {
        if (isDead) return;
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