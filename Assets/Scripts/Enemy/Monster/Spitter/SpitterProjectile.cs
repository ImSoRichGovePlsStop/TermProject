using UnityEngine;

public class SpitterProjectile : EnemyProjectileBase
{
    protected override void OnHit(Collider hitTarget = null)
    {
        hasHit = true;
        if (hitTarget != null)
            DealDamageTo(hitTarget);
        Destroy(gameObject);
    }
}