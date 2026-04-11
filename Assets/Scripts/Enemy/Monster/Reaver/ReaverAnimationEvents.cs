using UnityEngine;

public class ReaverAnimationEvents : MonoBehaviour
{
    private ReaverController reaver;

    private void Start()
    {
        reaver = GetComponentInParent<ReaverController>();
    }

    public void LockAttackDirection() => reaver?.LockAttackDirection();
    public void FlashWhite() => reaver?.FlashWhite();
    public void StartFlashBuildup(string args) => reaver?.StartFlashBuildup(args);
    public void DashAttack() => reaver?.DashAttack();
    public void StartCharge() => reaver?.StartCharge();
    public void FinishCharge() => reaver?.FinishCharge();
    public void FinishAttack() => reaver?.FinishAttack();
}