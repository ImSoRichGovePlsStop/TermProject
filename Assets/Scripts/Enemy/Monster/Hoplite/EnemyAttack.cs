using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth enemyHealth;

    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;

    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    public bool CanAttack()
    {
        // Debug.Log($"CanAttack? {Time.time} vs {lastAttackTime + attackCooldown}, isAttacking={isAttacking}");        if (enemyHealth != null && enemyHealth.IsDead) return false;
        // Debug.Log($"CanAttack on {gameObject.name} | id={GetInstanceID()} | isAttacking={isAttacking}");
        if (enemyHealth != null && enemyHealth.IsHurt) return false;
        if (isAttacking) return false;
        if (Time.time < lastAttackTime + attackCooldown) return false;
        return true;
    }

    public void StartAttack()
    {
        // if (!CanAttack()) return;
        // Debug.Log(">>> CALL StartAttack");

        isAttacking = true;
        animator.SetBool("IsAttacking", true);
        Debug.Log("Attack started");

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

    // Animation Event กลางคลิป
    public void DealDamage()
    {
        Debug.Log("DealDamage called");

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(center, attackRange, playerLayer);

        Debug.Log("Player hits found = " + hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                Debug.Log("Damage applied to player");
                playerStats.TakeDamage(attackDamage, enemyHealth);
                break;
            }
        }
    }

    public void DealDamage(GameObject target)
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        PlayerStats playerStats = target.GetComponent<PlayerStats>();

        if (playerStats == null) 
            playerStats = target.GetComponentInParent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.TakeDamage(attackDamage, enemyHealth);
        }
    }

    // Animation Event ท้ายคลิป
    public void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        animator.SetBool("IsAttacking", false);
        Debug.Log("FinishAttack called");
        Debug.Log($"FinishAttack called on {gameObject.name} | id={GetInstanceID()}");
    }

    public void ForceStopAttack()
    {
        if (!isAttacking) return;

        isAttacking = false;
        lastAttackTime = Time.time;
        animator.SetBool("IsAttacking", false);

        if (animator != null && gameObject.name.Contains("Harpy"))
        {
            animator.ResetTrigger("Attack");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }

    public void SetDamageMultiplier(float multiplier)
    {
        attackDamage *= multiplier;
    }

    public void SetAttackRangeMultiplier(float multiplier)
    {
        attackRange *= multiplier;
    }
}