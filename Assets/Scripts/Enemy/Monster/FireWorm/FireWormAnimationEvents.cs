using UnityEngine;

public class FireWormAnimationEvents : MonoBehaviour
{
    private FireWormController fireWorm;

    private void Start()
    {
        fireWorm = GetComponentInParent<FireWormController>();
    }

    public void FlashWhite() => fireWorm?.FlashWhite();
    public void StartFlashBuildup(string args) => fireWorm?.StartFlashBuildup(args);
    public void FireProjectile() => fireWorm?.FireProjectile();
    public void FireLastProjectile() => fireWorm?.FireLastProjectile();
}