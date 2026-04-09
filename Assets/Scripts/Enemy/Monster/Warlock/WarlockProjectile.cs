using UnityEngine;

public class WarlockProjectile : EnemyProjectileBase
{
    [Header("AoE Explosion")]
    [SerializeField] private float aoeRadius = 1.5f;

    protected override void OnHit(Collider hitTarget = null)
    {
        hasHit = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, targetLayers);
        foreach (var hit in hits)
            DealDamageTo(hit);

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}