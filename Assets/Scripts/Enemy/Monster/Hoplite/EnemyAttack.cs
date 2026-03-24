using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private float attackForwardOffset = 0.8f;

    [Header("References")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private bool isAttacking = false;
    private float nextAttackTime = 0f;

    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;
    public bool IsAttacking => isAttacking;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public bool CanAttack()
    {
        if (enemyHealth != null && enemyHealth.IsDead) return false;
        if (enemyHealth != null && enemyHealth.IsHurt) return false;
        if (isAttacking) return false;
        if (Time.time < nextAttackTime) return false;

        return true;
    }

    public void StartAttack()
    {
        if (!CanAttack()) return;

        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        Debug.Log("[EnemyAttack] StartAttack");

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

    public void DealDamage()
    {
        Debug.Log("[EnemyAttack] DealDamage");

        if (enemyHealth != null && enemyHealth.IsDead)
            return;

        float dirX = 1f;

        if (spriteRenderer != null && spriteRenderer.flipX)
            dirX = -1f;

        Vector3 center = transform.position + new Vector3(dirX * attackForwardOffset, 0f, 0f);

        Collider[] hits = Physics.OverlapSphere(center, attackRange, playerLayer);
        Debug.Log("[EnemyAttack] Player hits found = " + hits.Length);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].gameObject == gameObject || hits[i].transform.IsChildOf(transform))
                continue;

            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();

            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                Debug.Log("[EnemyAttack] Damage applied to player");
                playerStats.TakeDamage(attackDamage, enemyHealth);
                break;
            }
            else
            {
                Debug.LogWarning("[EnemyAttack] No PlayerStats found on hit object: " + hits[i].name);
            }
        }
    }

    public void FinishAttack()
    {
        isAttacking = false;
        Debug.Log("[EnemyAttack] FinishAttack");
    }

    public void ForceStopAttack()
    {
        isAttacking = false;

        if (animator != null)
            animator.ResetTrigger("Attack");
    }

    private void OnDrawGizmosSelected()
    {
        float dirX = 1f;

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            SpriteRenderer sr = spriteRenderer != null ? spriteRenderer : GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.flipX)
                dirX = -1f;
        }
        else
#endif
        {
            if (spriteRenderer != null && spriteRenderer.flipX)
                dirX = -1f;
        }

        Vector3 center = transform.position + new Vector3(dirX * attackForwardOffset, 0f, 0f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, attackRange);
    }
}