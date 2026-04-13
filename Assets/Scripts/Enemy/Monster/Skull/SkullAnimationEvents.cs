using UnityEngine;

public class SkullAnimationEvents : MonoBehaviour
{
    private SkullController skull;

    private void Awake()
    {
        skull = GetComponentInParent<SkullController>();
    }

    public void LockAttackDirection() => skull?.LockAttackDirection();
    public void DashAttack() => skull?.DashAttack();
    public void FinishAttack() => skull?.FinishAttack();
    public void StartFlashBuildup(string args) => skull?.StartFlashBuildup(args);
    public void FlashWhite() => skull?.FlashWhite();
}