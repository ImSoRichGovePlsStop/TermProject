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

    private float attackTimer = 0f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();
    }

    private void Update()
    {
        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;
    }

    public bool CanAttack()
    {
        if (enemyHealth != null && enemyHealth.IsDead)
            return false;

        return attackTimer <= 0f;
    }

    public void Attack()
    {
        if (!CanAttack()) return;

        if (animator != null)
            animator.SetTrigger("Attack");

        DoDamage();
        attackTimer = attackCooldown;
    }

    private void DoDamage()
    {
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;

        Collider[] hits = Physics.OverlapSphere(center, attackRange, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
                break;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}