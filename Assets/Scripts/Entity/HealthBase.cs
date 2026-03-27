using System;
using System.Collections;
using UnityEngine;

public abstract class HealthBase : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float maxHP = 30f;
    [SerializeField] private float destroyDelay = 2f;

    [Header("Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    protected float currentHP;
    protected bool isDead;
    protected bool isHurt;

    private Coroutine flashCoroutine;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;

    public event Action<float, bool> OnDamageReceived;
    public event Action OnDeath;

    protected virtual void Awake()
    {
        currentHP = maxHP;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        currentHP -= damage;
        currentHP = Mathf.Max(currentHP, 0f);

        OnDamageReceived?.Invoke(damage, isCrit);
        OnDamageTaken(damage, isCrit);

        if (currentHP <= 0f)
        {
            Die();
            return;
        }

        isHurt = true;
        OnHurtStart();
    }

    protected virtual void OnDamageTaken(float damage, bool isCrit)
    {
        TryFlash();
    }

    protected virtual void OnHurtStart() { }
    protected virtual void OnHurtEnd() { }
    protected virtual void OnDeathStart() { }

    protected void EndHurt()
    {
        isHurt = false;
        OnHurtEnd();
    }

    protected virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        isHurt = false;

        OnDeathStart();
        OnDeath?.Invoke();
        Destroy(gameObject, destroyDelay);
    }

    protected void TryFlash()
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        Color original = spriteRenderer.color;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = original;
        flashCoroutine = null;
    }
}