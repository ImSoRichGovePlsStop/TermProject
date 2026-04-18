using UnityEngine;

public class LasherReaverAnimationEvents : MonoBehaviour
{
    private LasherReaverController controller;

    private void Start()
    {
        controller = GetComponentInParent<LasherReaverController>();
    }

    // Shared
    public void LockAttackDirection() => controller?.LockAttackDirection();
    public void FlashWhite() => controller?.FlashWhite();
    public void StartFlashBuildup(string args) => controller?.StartFlashBuildup(args);
    public void FinishAttack() => controller?.FinishAttack();

    // Lasher Form
    public void DashAttackFirst() => controller?.DashAttackFirst();
    public void DashAttackSecond() => controller?.DashAttackSecond();
    public void TryExtendCombo() => controller?.TryExtendCombo();
    public void DashAttackThird() => controller?.DashAttackThird();
    public void AfterHit3() => controller?.AfterHit3();
    public void Hit4Begin() => controller?.Hit4Begin();
    public void Hit4StopMoving() => controller?.Hit4StopMoving();
    public void Hit4LashHit() => controller?.Hit4LashHit();

    // Reaver Form
    public void ReaverDashAttack() => controller?.ReaverDashAttack();
    public void StartCharge() => controller?.StartCharge();
    public void FinishCharge() => controller?.FinishCharge();
}