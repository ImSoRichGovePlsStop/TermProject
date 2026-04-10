using UnityEngine;
public class ArcWarlockAnimationEvents : MonoBehaviour
{
    private ArcWarlockController controller;
    private void Awake()
    {
        controller = GetComponentInParent<ArcWarlockController>();
    }
    public void FireProjectile() => controller?.FireProjectile();
    public void FireLastProjectile() => controller?.FireLastProjectile();
    public void StartFlashBuildup(string args) => controller?.StartFlashBuildup(args);
    public void FlashWhite() => controller?.FlashWhite();
}