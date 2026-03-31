using UnityEngine;

public class WarlockAnimationEvents : MonoBehaviour
{
    private WarlockController controller;

    private void Awake()
    {
        controller = GetComponentInParent<WarlockController>();
    }

    // Animation Event — call per projectile
    public void FireProjectile()
    {
        controller?.FireProjectile();
    }

    // Animation Event — call on last projectile
    public void FireLastProjectile()
    {
        controller?.FireLastProjectile();
    }
}