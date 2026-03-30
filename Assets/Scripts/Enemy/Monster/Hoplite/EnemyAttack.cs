using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] protected float attackDamage = 10f;
    [SerializeField] protected float attackRange = 1.2f;
    [SerializeField] protected float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] protected Transform attackPoint;
    [SerializeField] protected LayerMask playerLayer;
    [SerializeField] protected Animator animator;
    [SerializeField] protected EnemyHealth enemyHealth;

    protected bool isAttacking = false;
    protected float lastAttackTime = -Mathf.Infinity;

    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public bool IsAttacking => isAttacking;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyHealth == null)
            enemyHealth = GetComponentInParent<EnemyHealth>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    public virtual bool CanAttack()
    {
        if (enemyHealth != null && enemyHealth.IsDead) return false;
        if (enemyHealth != null && enemyHealth.IsHurt) return false;
        if (isAttacking) return false;
        if (Time.time < lastAttackTime + attackCooldown) return false;
        return true;
    }

    public virtual void StartAttack()
    {
        if (!CanAttack()) return;

        isAttacking = true;

        if (animator != null)
            animator.SetBool("IsAttacking", true);

        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        else
        {
            DealDamage();
            FinishAttack();
        }
    }

    // animation event
    public virtual void DealDamage()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        Vector3 center = GetAttackCenter();
        Collider[] hits = Physics.OverlapSphere(center, attackRange, playerLayer);

        TryDamageFromHits(hits);
    }

    public virtual void DealDamage(GameObject target)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        if (target == null) return;

        PlayerStats playerStats = target.GetComponent<PlayerStats>();

        if (playerStats == null)
            playerStats = target.GetComponentInParent<PlayerStats>();

        if (playerStats != null)
            playerStats.TakeDamage(GetFinalDamage(), enemyHealth);
    }

    protected virtual Vector3 GetAttackCenter()
    {
        return attackPoint != null ? attackPoint.position : transform.position;
    }

    protected virtual void TryDamageFromHits(Collider[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(GetFinalDamage(), enemyHealth);
                break;
            }
        }
    }

    protected virtual float GetFinalDamage()
    {
        return attackDamage;
    }

    // animation event
    public virtual void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.ResetTrigger("Attack");
        }
    }

    public virtual void ForceStopAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;

        if (animator != null)
        {
            animator.SetBool("IsAttacking", false);
            animator.ResetTrigger("Attack");
        }
    }

    public virtual void SetDamageMultiplier(float multiplier)
    {
        attackDamage *= multiplier;
    }

    public virtual void SetAttackRangeMultiplier(float multiplier)
    {
        attackRange *= multiplier;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(GetAttackCenter(), attackRange);
    }
}