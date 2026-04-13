using UnityEngine;

public class MimicAnimationEvents : MonoBehaviour
{
    private MimicController mimic;

    private void Awake()
    {
        mimic = GetComponentInParent<MimicController>();
    }

    public void LockAttackDirection() => mimic?.LockAttackDirection();
    public void DashAttack() => mimic?.DashAttack();
    public void FinishAttack() => mimic?.FinishAttack();
    public void StartFlashBuildup(string args) => mimic?.StartFlashBuildup(args);
    public void FlashWhite() => mimic?.FlashWhite();
}