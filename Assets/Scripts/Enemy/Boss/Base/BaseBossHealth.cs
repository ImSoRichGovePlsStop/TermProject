using System.Collections;
using UnityEngine;

public abstract class BaseBossHealth : MonoBehaviour
{
    [Header("Base Health")]
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected float hurtDuration = 0.15f;
    [SerializeField] protected float destroyDelay = 3f;

    [Header("Base References")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Collider bossCollider;
    [SerializeField] protected Rigidbody rb;

    protected float currentHP;
    protected bool isDead = false;
    protected bool isHurt = false;
    protected Coroutine hurtRoutine;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;

    protected virtual void Awake()
    {
        currentHP = maxHP;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (bossCollider == null)
            bossCollider = GetComponent<Collider>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();
    }

    public virtual void TakeDamage(float damage)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        currentHP -= damage;
        currentHP = Mathf.Max(0f, currentHP);

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        if (hurtRoutine != null)
            StopCoroutine(hurtRoutine);

        hurtRoutine = StartCoroutine(HurtCoroutine());
    }

    protected virtual IEnumerator HurtCoroutine()
    {
        isHurt = true;

        if (animator != null)
            animator.SetTrigger("Hurt");

        yield return new WaitForSeconds(hurtDuration);

        isHurt = false;
        hurtRoutine = null;
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;

        if (bossCollider != null)
            bossCollider.enabled = false;

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