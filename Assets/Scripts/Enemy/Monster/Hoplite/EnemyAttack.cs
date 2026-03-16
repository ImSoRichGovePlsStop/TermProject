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
        if (enemyHealth != null && enemyHealth.IsDead) return false;
        if (enemyHealth != null && enemyHealth.IsHurt) return false;
        if (isAttacking) return false;
        return true;
    }

    public void StartAttack()
    {
        if (!CanAttack()) return;

        isAttacking = true;
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

    // Animation Event ท้ายคลิป
    public void FinishAttack()
    {
        isAttacking = false;
        Debug.Log("FinishAttack called");
    }

    public void ForceStopAttack()
    {
        isAttacking = false;

        if (animator != null && gameObject.name.Contains("Harpy"))
        {
            animator.ResetTrigger("Attack");
            // animator.SetBool("IsAttacking", false);
            animator.Play("Harpy_Walk");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}