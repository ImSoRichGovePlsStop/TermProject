using UnityEngine;

public class MinotaurAnimationEvents : MonoBehaviour
{
    private MinotaurController minotaur;

    private void Start()
    {
        minotaur = GetComponentInParent<MinotaurController>();
    }

    public void DashToTarget() => minotaur?.DashToTarget();
    public void DealDamage() => minotaur?.DealDamage();
    public void FinishAttack() => minotaur?.FinishAttack();
}