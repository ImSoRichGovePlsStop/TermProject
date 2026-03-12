using System.Collections;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHP = 30;
    [SerializeField] private float hurtStunDuration = 0.2f;
    [SerializeField] private float destroyDelay = 2f;

    [Header("References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private Animator animator;
    [SerializeField] private Collider enemyCollider;
    [SerializeField] private Rigidbody rb;

    private float currentHP;
    private bool isDead = false;
    private bool isHurt = false;
    private Coroutine hurtCoroutine;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;

    private void Awake()
    {
        currentHP = maxHP;

        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

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

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    private IEnumerator HurtRoutine()
    {
        isHurt = true;

        if (enemyAttack != null)
            enemyAttack.ForceStopAttack();

        if (enemyMovement != null)
        {
            enemyMovement.StopMoving();
            enemyMovement.SetCanMove(false);
        }

        if (animator != null)
            animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(hurtStunDuration);

        if (!isDead && enemyMovement != null)
            enemyMovement.SetCanMove(true);

        isHurt = false;
        hurtCoroutine = null;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        isHurt = false;

        if (enemyAttack != null)
            enemyAttack.ForceStopAttack();

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