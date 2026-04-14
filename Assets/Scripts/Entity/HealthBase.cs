using System;
using System.Collections;
using UnityEngine;

public abstract class HealthBase : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] protected float destroyDelay = 2f;

    [Header("Flash")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private SpriteRenderer flashSpriteRenderer;
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
    private Coroutine hitFlashCoroutine;
    private Color originalColor;
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => isDead;
    public bool IsHurt => isHurt;
    public bool IsInvincible { get; set; }

    public event Action<float, bool> OnDamageReceived;
    protected void RaiseOnDamageReceived(float damage, bool isCrit) => OnDamageReceived?.Invoke(damage, isCrit);
    public event Action OnDeath;

    protected virtual void Awake()
    {
        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null)
            maxHP = entityStats.MaxHP;

        currentHP = maxHP;

        if (spriteRenderer == null)
        {
            var visual = transform.Find("Visual");
            if (visual != null)
                spriteRenderer = visual.GetComponent<SpriteRenderer>();
        }

        if (flashSpriteRenderer == null)
        {
            var overlay = transform.Find("FlashOverlay");
            if (overlay != null)
                flashSpriteRenderer = overlay.GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.material.GetColor(ColorID);
            if (flashSpriteRenderer != null)
                flashSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    protected virtual void Start()
    {
        if (showHealthBar)
            EntityHealthBarSpawner.Instance?.SpawnBar(this, healthBarHeight, healthBarScale);
    }

    protected virtual void Update() { }

    protected virtual void LateUpdate()
    {
        if (flashSpriteRenderer != null && spriteRenderer != null)
        {
            flashSpriteRenderer.sprite = spriteRenderer.sprite;
            flashSpriteRenderer.flipX = spriteRenderer.flipX;
            flashSpriteRenderer.flipY = spriteRenderer.flipY;
            flashSpriteRenderer.transform.localScale = spriteRenderer.transform.localScale;
        }
    }

    public void SetMaxHP(float newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = maxHP;
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        float actual = Mathf.Min(amount, maxHP - currentHP);
        currentHP += actual;
        if (actual > 0f)
            DamageNumberSpawner.Instance?.SpawnHealNumber(transform.position, actual);
    }

    public virtual void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (IsInvincible) return;
        if (damage <= 0f) return;

        var entityStats = GetComponent<EntityStats>();
        if (entityStats != null)
            damage *= entityStats.DamageTaken;

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

    public void TryFlash()
    {
        if (spriteRenderer == null) return;
        if (hitFlashCoroutine != null) StopCoroutine(hitFlashCoroutine);
        hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
    }

    public void TryFlash(Color color)
    {
        if (spriteRenderer == null) return;

        if (buildupCoroutine != null)
        {
            StopCoroutine(buildupCoroutine);
            buildupCoroutine = null;
            if (flashSpriteRenderer != null)
                flashSpriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
        }

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine(color));
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.material.SetColor(ColorID, flashColor);
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material.SetColor(ColorID, originalColor);
        hitFlashCoroutine = null;
    }

    private IEnumerator FlashRoutine(Color color)
    {
        if (flashSpriteRenderer != null)
        {
            flashSpriteRenderer.color = color;
            yield return new WaitForSeconds(flashDuration);
            flashSpriteRenderer.color = new Color(color.r, color.g, color.b, 0f);
        }
        else
        {
            spriteRenderer.color = color;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
        }
        flashCoroutine = null;
    }

    private Coroutine buildupCoroutine;

    public void StartFlashBuildup(Color color, float buildupDuration, float maxAlpha = 0.4f)
    {
        if (flashSpriteRenderer == null) return;
        if (buildupCoroutine != null) StopCoroutine(buildupCoroutine);
        buildupCoroutine = StartCoroutine(FlashBuildupRoutine(color, buildupDuration, maxAlpha));
    }

    private IEnumerator FlashBuildupRoutine(Color color, float buildupDuration, float maxAlpha)
    {
        float elapsed = 0f;
        while (elapsed < buildupDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, maxAlpha, elapsed / buildupDuration);
            if (flashSpriteRenderer != null)
                flashSpriteRenderer.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }
        buildupCoroutine = null;
    }

    public void StopFlashBuildup()
    {
        if (buildupCoroutine != null)
        {
            StopCoroutine(buildupCoroutine);
            buildupCoroutine = null;
        }
        if (flashSpriteRenderer != null)
            flashSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
    }
}