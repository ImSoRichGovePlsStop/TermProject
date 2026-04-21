using UnityEngine;

public class MinotaurAnimationEvents : MonoBehaviour
{
    private MinotaurController minotaur;

    private void Start()
    {
        minotaur = GetComponentInParent<MinotaurController>();
    }

    public void LockAttackDirection() => minotaur?.LockAttackDirection();
    public void StartFlashBuildup(string args) => minotaur?.StartFlashBuildup(args);
    public void FlashWhite() => minotaur?.FlashWhite();
    public void DashAttack() => minotaur?.DashAttack();
    public void FinishAttack() => minotaur?.FinishAttack();
}