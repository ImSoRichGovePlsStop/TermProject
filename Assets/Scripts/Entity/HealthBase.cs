using System;
using System.Collections;
using UnityEngine;

public abstract class HealthBase : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float destroyDelay = 2f;

    [Header("Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    [Header("Health Bar")]
    [SerializeField] private bool showHealthBar = true;
    [SerializeField] protected float healthBarHeight = 1f;
    [SerializeField] protected Vector3 healthBarScale = new Vector3(1f, 1f, 1f);

    public float HealthBarHeight => healthBarHeight;
    public Vector3 HealthBarScale => healthBarScale;

    protected float maxHP = 30f;
    protected float currentHP;
    protected bool isDead;
    protected bool isHurt;

    private Coroutine flashCoroutine;
    private Color originalColor;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;
    public bool IsInvincible { get; set; }

    public event Action<float, bool> OnDamageReceived;
    public event Action OnDeath;

    protected virtual void Awake()
    {
        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null)
            maxHP = entityStats.MaxHP;

        currentHP = maxHP;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    protected virtual void Start()
    {
        if (showHealthBar)
            EntityHealthBarSpawner.Instance?.SpawnBar(this, healthBarHeight, healthBarScale);
    }

    protected virtual void Update() { }

    public void SetMaxHP(float newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = maxHP;
    }

    public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (IsInvincible) return;
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
        OnDie();
    }

    protected virtual void OnDie()
    {
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
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
}