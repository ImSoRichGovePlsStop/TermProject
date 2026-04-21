using UnityEngine;
public class WarlockAnimationEvents : MonoBehaviour
{
    private WarlockController controller;
    private MinibossWarlockController minibossController;
    private void Awake()
    {
        controller = GetComponentInParent<WarlockController>();
        minibossController = GetComponentInParent<MinibossWarlockController>();
    }
    public void FireProjectile() => controller?.FireProjectile();
    public void FireLastProjectile() => controller?.FireLastProjectile();
    public void FinishSmash() => minibossController?.FinishSmash();
    public void StartFlashBuildup(string args) => controller?.StartFlashBuildup(args);
    public void FlashWhite() => controller?.FlashWhite();
}