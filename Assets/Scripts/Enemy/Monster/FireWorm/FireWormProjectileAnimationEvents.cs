using UnityEngine;

public class FireWormProjectileAnimationEvents : MonoBehaviour
{
    private FireWormProjectile projectile;

    private void Start()
    {
        projectile = GetComponentInParent<FireWormProjectile>();
    }

    public void ExplodeDamage() => projectile?.ExplodeDamage();
    public void ExplodeFinish() => projectile?.ExplodeFinish();
}