using UnityEngine;

public class HuskAnimationEvents : MonoBehaviour
{
    private HuskController husk;

    private void Start()
    {
        husk = GetComponentInParent<HuskController>();
    }

    public void LockAttackDirection() => husk?.LockAttackDirection();
    public void FlashWhite() => husk?.FlashWhite();
    public void StartFlashBuildup(string args) => husk?.StartFlashBuildup(args);
    public void DashAttack() => husk?.DashAttack();
    public void FinishAttack() => husk?.FinishAttack();
}