using UnityEngine;

public class WarlockAnimationEvents : MonoBehaviour
{
    private WarlockController controller;
    private EliteWarlockController eliteController;

    private void Awake()
    {
        controller = GetComponentInParent<WarlockController>();
        eliteController = GetComponentInParent<EliteWarlockController>();
    }

    public void FireProjectile() => controller?.FireProjectile();
    public void FireLastProjectile() => controller?.FireLastProjectile();

    public void FinishSmash() => eliteController?.FinishSmash();
}