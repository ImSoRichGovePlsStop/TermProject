using UnityEngine;

public class HarpyAnimationEvents : MonoBehaviour
{
    private HarpyController harpy;
    private EliteHarpyController eliteHarpy;

    private void Start()
    {
        harpy = GetComponentInParent<HarpyController>();
        eliteHarpy = GetComponentInParent<EliteHarpyController>();
    }

    public void DashAttack() => harpy?.DashAttack();
    public void FinishAttack() => harpy?.FinishAttack();
    public void LockAttackDirection() => harpy?.LockAttackDirection();
    public void StartFlashBuildup(string args) => harpy?.StartFlashBuildup(args);
    public void FlashWhite() => harpy?.FlashWhite();
    public void FinishDiveLand() => eliteHarpy?.FinishDiveLand();
}