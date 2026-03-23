using System;
using System.Collections;
using UnityEngine;

public abstract class EnemyHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float maxHP = 30f;
    [SerializeField] protected float hurtStunDuration = 0.2f;
    [SerializeField] protected float destroyDelay = 2f;

    [Header("References")]
    [SerializeField] protected Animator animator;
    [SerializeField] protected Collider enemyCollider;
    [SerializeField] protected Rigidbody rb;

    [Header("Health Bar")]
    [SerializeField] private Vector3 healthBarOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector3 healthBarScale = new Vector3(1f, 1f, 1f);

    protected float currentHP;
    protected bool isDead;
    protected bool isHurt;
    protected Coroutine hurtCoroutine;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;
    public Vector3 HealthBarOffset => healthBarOffset;
    public Vector3 HealthBarScale => healthBarScale;

    public event Action<float, bool> OnDamageReceived;

    protected virtual void Awake()
    {
        currentHP = maxHP;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemyCollider == null)
            enemyCollider = GetComponent<Collider>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        CacheComponents();
    }

    protected virtual void Start()
    {
        var spawner = FindFirstObjectByType<EnemyHealthBarSpawner>();
        spawner?.SpawnBar(this, healthBarOffset, healthBarScale);

        var damageSpawner = FindFirstObjectByType<DamageNumberSpawner>();
        damageSpawner?.RegisterEnemy(this);
    }

    protected virtual void CacheComponents() { }

    public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        float finalDamage = ModifyIncomingDamage(damage);
        if (finalDamage <= 0f) return;

        currentHP -= finalDamage;
        OnDamageTaken(finalDamage);
        OnDamageReceived?.Invoke(finalDamage, isCrit);

        if (currentHP <= 0f)
        {
            currentHP = 0f;
            Die();
            return;
        }

        if (hurtCoroutine != null)
            StopCoroutine(hurtCoroutine);

        hurtCoroutine = StartCoroutine(HurtRoutine());
    }

    protected virtual float ModifyIncomingDamage(float damage)
    {
        return damage;
    }

    protected virtual void OnDamageTaken(float finalDamage)
    {
        //Debug.Log($"[{gameObject.name}] TakeDamage: {finalDamage:F1} HP: {currentHP:F1}/{maxHP:F1}");
    }

    protected virtual IEnumerator HurtRoutine()
    {
        isHurt = true;
        OnHurtStart();
        TriggerHurtAnimation();

        yield return new WaitForSeconds(hurtStunDuration);

        OnHurtEnd();
        isHurt = false;
        hurtCoroutine = null;
    }

    protected virtual void OnHurtStart() { }
    protected virtual void OnHurtEnd() { }

    protected virtual void TriggerHurtAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Hurt");
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;

        if (hurtCoroutine != null)
        {
            StopCoroutine(hurtCoroutine);
            hurtCoroutine = null;
        }

        var context = FindFirstObjectByType<PlayerCombatContext>();
        context?.NotifyEnemyKilled(this);

        OnDeathStart();
        DisableMainCollider();
        HandleRigidbodyOnDeath();
        TriggerDeathAnimation();
        Destroy(gameObject, destroyDelay);
    }

    protected virtual void OnDeathStart() { }

    protected virtual void DisableMainCollider()
    {
        if (enemyCollider != null)
            enemyCollider.enabled = false;
    }

    protected virtual void HandleRigidbodyOnDeath()
    {
        if (rb == null) return;

        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true;
    }

    protected virtual void TriggerDeathAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Die");
    }
}