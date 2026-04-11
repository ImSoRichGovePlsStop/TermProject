using UnityEngine;

public class LasherAnimationEvents : MonoBehaviour
{
    private LasherController lasher;

    private void Start()
    {
        lasher = GetComponentInParent<LasherController>();
    }

    public void LockAttackDirection() => lasher?.LockAttackDirection();
    public void FlashWhite() => lasher?.FlashWhite();
    public void StartFlashBuildup(string args) => lasher?.StartFlashBuildup(args);

    // Attack1
    public void DashAttackFirst() => lasher?.DashAttackFirst();
    public void OnComboGap() => lasher?.OnComboGap();
    public void DashAttackSecond() => lasher?.DashAttackSecond();

    // Attack2
    public void StartLashCharge() => lasher?.StartLashCharge();
    public void LashHit() => lasher?.LashHit();

    // Shared
    public void FinishAttack() => lasher?.FinishAttack();
}