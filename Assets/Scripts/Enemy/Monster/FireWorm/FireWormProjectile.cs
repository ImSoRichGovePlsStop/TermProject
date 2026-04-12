using UnityEngine;

public class FireWormProjectile : EnemyProjectileBase
{
    [Header("AoE Explosion")]
    [SerializeField] private float aoeRadius = 1.5f;

    [Header("References")]
    [SerializeField] private Animator projectileAnimator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public override void Initialize(Vector3 targetPosition, float dmg, HealthBase attackerHealth = null)
    {
        base.Initialize(targetPosition, dmg, attackerHealth);

        if (spriteRenderer != null && moveDirection != Vector3.zero)
        {
            float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angle + 270f, 0f);
        }
    }

    protected override void OnHit(Collider hitTarget = null)
    {
        hasHit = true;
        projectileAnimator?.SetTrigger("Explode");
    }

    // Animation Event
    public void ExplodeDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, targetLayers);
        foreach (var hit in hits)
            DealDamageTo(hit);
    }

    // Animation Event
    public void ExplodeFinish()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}