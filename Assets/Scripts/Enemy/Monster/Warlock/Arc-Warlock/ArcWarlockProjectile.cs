using UnityEngine;
public class ArcWarlockProjectile : EnemyProjectileBase
{
    [Header("Homing")]
    [SerializeField] private float homingStrength = 120f;
    [SerializeField] private float targetScanRange = 30f;
    [Header("Lifetime")]
    [SerializeField] private float lifetime = 8f;
    [Header("Damage Tick")]
    [SerializeField] private float damageInterval = 0.25f;
    [Header("Collider")]
    [SerializeField] private float colliderRadius = 0.3f;
    [Header("Animation")]
    [SerializeField] private AnimationClip projectileClip;

    private Animator animator;
    private Transform homingTarget;
    private float lifetimeTimer;
    private float damageTimer;
    private Collider playerCollider;

    public override void Initialize(Vector3 targetPosition, float dmg, HealthBase attackerHealth = null)
    {
        base.Initialize(targetPosition, dmg, attackerHealth);
        homingTarget = FindPlayer();
        lifetimeTimer = lifetime;
        damageTimer = damageInterval;
        var cap = GetComponent<CapsuleCollider>();
        if (cap != null) cap.radius = colliderRadius;
        animator = GetComponentInChildren<Animator>();
        if (animator != null && projectileClip != null)
            animator.Play(projectileClip.name);
    }

    protected override void Update()
    {
        if (hasHit) return;
        if (attacker != null && attacker.IsDead)
        {
            DestroySelf();
            return;
        }
        lifetimeTimer -= Time.deltaTime;
        if (lifetimeTimer <= 0f)
        {
            DestroySelf();
            return;
        }
        if (homingTarget == null)
            homingTarget = FindPlayer();
        if (homingTarget != null)
        {
            Vector3 toTarget = homingTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                float maxRad = homingStrength * Mathf.Deg2Rad * Time.deltaTime;
                moveDirection = Vector3.RotateTowards(moveDirection, toTarget.normalized, maxRad, 0f);
            }
        }
        Move();
        if (playerCollider != null)
        {
            damageTimer -= Time.deltaTime;
            if (damageTimer <= 0f)
            {
                damageTimer = damageInterval;
                DealDamageTo(playerCollider);
            }
        }
    }

    protected override void OnHit(Collider hitTarget = null) { }

    private new void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        var ps = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        if (ps == null || ps.IsDead) return;
        playerCollider = other;
        damageTimer = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == playerCollider)
            playerCollider = null;
    }

    private void DestroySelf()
    {
        hasHit = true;
        Destroy(gameObject);
    }

    private Transform FindPlayer()
    {
        var hits = Physics.OverlapSphere(transform.position, targetScanRange,
                       LayerMask.GetMask("Player"));
        foreach (var col in hits)
        {
            var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) return col.transform;
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}