using UnityEngine;
using System.Collections.Generic;

public class WandProjectile : MonoBehaviour
{
    // Set by WandAttack before spawn
    [HideInInspector] public float damage;
    [HideInInspector] public float maxSpeed;
    [HideInInspector] public float maxRange;
    [HideInInspector] public float lifetime;
    [HideInInspector] public PlayerStats shooterStats;
    [HideInInspector] public PlayerCombatContext context;
    [HideInInspector] public int comboIndex;
    [HideInInspector] public bool isCrit;
    [HideInInspector] public bool alwaysSlowClip;
    [HideInInspector] public float colliderRadius;
    [HideInInspector] public Vector3 moveDirection;

    // Animation clips
    [HideInInspector] public AnimationClip fastClip;
    [HideInInspector] public AnimationClip slowClip;
    [HideInInspector] public float slowThreshold = 0.4f;

    // Deceleration curve
    // speed = maxSpeed * (1 - t)^easePower  where t = distanceTravelled / maxRange
    [HideInInspector] public float easePower = 2f;

    // Internal state
    private bool isStopped = false;
    private float distanceTravelled = 0f;
    private float lifetimeTimer = 0f;
    private float currentSpeed = 0f;
    private bool isPlayingSlowClip = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private HashSet<int> hitEnemyIDs = new HashSet<int>();

    void Start()
    {
        lifetimeTimer = lifetime;
        currentSpeed = maxSpeed;
        animator = GetComponentInChildren<Animator>();
        var visual = transform.Find("Visual");
        if (visual != null) spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angle + 90f, 0f);
        }

        var col = GetComponent<CapsuleCollider>();
        if (col != null && colliderRadius > 0f)
            col.radius = colliderRadius;

        if (alwaysSlowClip)
            PlayClip(slowClip);
        else
            PlayClip(fastClip);

        isPlayingSlowClip = alwaysSlowClip;
    }

    void Update()
    {
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        if (isStopped) return;

        float t = maxRange > 0f ? distanceTravelled / maxRange : 1f;
        t = Mathf.Clamp01(t);

        currentSpeed = maxSpeed * Mathf.Pow(1f - t, easePower);

        float step = currentSpeed * Time.deltaTime;
        transform.position += moveDirection * step;
        distanceTravelled += step;

        UpdateClip();

        if (distanceTravelled >= maxRange)
        {
            isStopped = true;
            currentSpeed = 0f;
            UpdateClip();
        }
    }

    private void UpdateClip()
    {
        if (alwaysSlowClip) return;

        bool shouldBeSlow = currentSpeed < maxSpeed * slowThreshold;
        if (shouldBeSlow == isPlayingSlowClip) return;

        isPlayingSlowClip = shouldBeSlow;
        PlayClip(shouldBeSlow ? slowClip : fastClip);
    }

    private void PlayClip(AnimationClip clip)
    {
        if (animator == null || clip == null) return;
        animator.Play(clip.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        int id = other.gameObject.GetInstanceID();
        if (hitEnemyIDs.Contains(id)) return;

        hitEnemyIDs.Add(id);
        DealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;
        if (!isStopped) return;

        int id = other.gameObject.GetInstanceID();
        if (hitEnemyIDs.Contains(id)) return;

        hitEnemyIDs.Add(id);
        DealDamage(other);
    }

    private void DealDamage(Collider other)
    {
        var healthBase = other.GetComponentInParent<HealthBase>();
        if (healthBase != null && !healthBase.IsDead)
        {
            healthBase.TakeDamage(damage, isCrit);
            context?.NotifyAttack(new HashSet<EnemyHealth>(), comboIndex);
            return;
        }

        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        enemyHealth.TakeDamage(damage, isCrit);
        var result = new HashSet<EnemyHealth> { enemyHealth };
        context?.NotifyAttack(result, comboIndex);
    }
}