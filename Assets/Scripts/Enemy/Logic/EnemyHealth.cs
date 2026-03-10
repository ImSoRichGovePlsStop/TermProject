using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHP = 30;
    [SerializeField] private float destroyDelay = 2f;

    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider enemyCollider;
    [SerializeField] private Rigidbody rb;

    private int currentHP;
    private bool isDead = false;

    public int CurrentHP => currentHP;
    public int MaxHP => maxHP;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHP = maxHP;

        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        if (damage <= 0) return;

        currentHP -= damage;

        if (currentHP <= 0)
        {
            currentHP = 0;
            Die();
            return;
        }

        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        if (enemyController != null)
            enemyController.Die();

        if (enemyCollider != null)
            enemyCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, destroyDelay);
    }
}