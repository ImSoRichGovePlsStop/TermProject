using UnityEngine;

public class EnemyAnimationRelay : MonoBehaviour
{
    [SerializeField] private EnemyAttack enemyAttack;

    private void Awake()
    {
        if (enemyAttack == null)
            enemyAttack = GetComponentInParent<EnemyAttack>();
    }

    public void DealDamage()
    {
        if (enemyAttack != null)
            enemyAttack.DealDamage();
    }

    public void FinishAttack()
    {
        if (enemyAttack != null)
            enemyAttack.FinishAttack();
    }
}