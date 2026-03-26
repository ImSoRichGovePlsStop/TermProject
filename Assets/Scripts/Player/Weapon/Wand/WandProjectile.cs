using UnityEngine;
using System.Collections.Generic;

// Attached to each wand projectile prefab.
// - Flies forward with deceleration (fast at start, slows near end, stops at maxRange)
// - Lifetime expires at the same time the projectile stops (designed to sync)
// - Each enemy can only be hit once (enemies that walk into a stopped projectile are also hit)
// - Swaps animation clip based on current speed, unless alwaysSlowClip is true
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
    [HideInInspector] public bool alwaysSlowClip;   // true = big projectile always uses slow clip
    [HideInInspector] public float colliderRadius;
    [HideInInspector] public Vector3 moveDirection;  // direction to fly, set by WandAttack

    // Animation clips
    [HideInInspector] public AnimationClip fastClip;
    [HideInInspector] public AnimationClip slowClip;
    // Switch to slow clip when currentSpeed < maxSpeed * slowThreshold
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
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Rotate Visual to face move direction
        if (spriteRenderer != null && moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angle + 90f, 0f);
        }

        var col = GetComponent<CapsuleCollider>();
        if (col != null && colliderRadius > 0f)
            col.radius = colliderRadius;

        // Start with appropriate clip
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

        // t = travel progress (0 = just spawned, 1 = reached max range)
        float t = maxRange > 0f ? distanceTravelled / maxRange : 1f;
        t = Mathf.Clamp01(t);

        // (1 - t)^easePower: full speed at start, decelerates to near zero at end
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
        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth == null) return;

        enemyHealth.TakeDamage(damage, isCrit);

        var result = new HashSet<EnemyHealth> { enemyHealth };
        context?.NotifyAttack(result, comboIndex);
    }
}