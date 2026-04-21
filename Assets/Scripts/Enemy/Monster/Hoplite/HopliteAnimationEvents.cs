using UnityEngine;

public class HopliteAnimationEvents : MonoBehaviour
{
    private HopliteController hoplite;

    private void Start()
    {
        hoplite = GetComponentInParent<HopliteController>();
    }

    public void DashAttack() => hoplite?.DashAttack();
    public void FinishAttack() => hoplite?.FinishAttack();
    public void LockAttackDirection() => hoplite?.LockAttackDirection();
    public void FlashWhite() => hoplite?.FlashWhite();
    public void StartFlashBuildup(string args) => hoplite?.StartFlashBuildup(args);
}