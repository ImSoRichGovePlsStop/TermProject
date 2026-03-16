// using UnityEngine;

// public class HarpyAttack : MonoBehaviour
// {
//     [Header("Attack")]
//     [SerializeField] private float attackDamage = 10f;
//     [SerializeField] private float attackRange = 1.2f;
//     [SerializeField] private float attackCooldown = 1.2f;

//     [Header("Reference")]
//     [SerializeField] private Transform attackPoint;
//     [SerializeField] private LayerMask playerLayer;
//     [SerializeField] private Animator animator;
//     [SerializeField] private EnemyHealth enemyHealth;

//     private float attackTimer = 0f;

//     private void Awake()
//     {
//         if (animator == null)
//             animator = GetComponentInChildren<Animator>();

//         if (enemyHealth == null)
//             enemyHealth = GetComponent<EnemyHealth>();
//     }

//     private void Update()
//     {
//         if (attackTimer > 0f)
//             attackTimer -= Time.deltaTime;
//     }

//     public bool CanAttack()
//     {
//         if (enemyHealth != null && enemyHealth.IsDead)
//             return false;

//         return attackTimer <= 0f;
//     }

//     public void Attack()
//     {
//         if (!CanAttack()) return;

//         if (animator != null)
//             animator.SetTrigger("Attack");
        
//         DoDamage();
//         attackTimer = attackCooldown;
//     }

//     private void DoDamage()
//     {
//         Vector3 center = attackPoint != null ? attackPoint.position : transform.position;

//         Collider[] hits = Physics.OverlapSphere(center, attackRange, playerLayer);

//         for (int i = 0; i < hits.Length; i++)
//         {
//             PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

//             if (playerStats == null)
//                 playerStats = hits[i].GetComponentInParent<PlayerStats>();

//             if (playerStats != null)
//             {
//                 playerStats.TakeDamage(attackDamage);
//                 break;
//             }
//         }
//     }

//     private void OnDrawGizmosSelected()
//     {
//         Gizmos.color = Color.magenta;
//         Vector3 center = attackPoint != null ? attackPoint.position : transform.position;
//         Gizmos.DrawWireSphere(center, attackRange);
//     }
// }

// using UnityEngine;

// public class HarpyAttack : EnemyAttack
// {
//     [Header("Harpy Attack")]
//     [SerializeField] private float diveSpeed = 6f;
//     [SerializeField] private Transform player;

//     private bool isDiving = false;

//     public override void StartAttack()
//     {
//         if (!CanAttack()) return;

//         IsAttacking = true;
//         isDiving = true;

//         if (animator != null)
//             animator.SetTrigger("Attack");
//     }

//     private void Update()
//     {
//         if (!isDiving) return;

//         if (player == null) return;

//         Vector3 dir = (player.position - transform.position).normalized;
//         transform.position += dir * diveSpeed * Time.deltaTime;
//         if (Vector3.Distance(transform.position, player.position) < 0.5f)
//         {
//             isDiving = false;
//         }
//     }

//     public override void FinishAttack()
//     {
//         base.FinishAttack();
//         isDiving = false;
//     }
// }