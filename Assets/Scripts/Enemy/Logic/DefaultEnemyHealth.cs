using UnityEngine;

public class DefaultEnemyHealth : EnemyHealth
{
    [Header("Default Enemy References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;
    [SerializeField] private EnemyAttack enemyAttack;

    private EnemyStatusHandler statusHandler;

    protected override void CacheComponents()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponentInChildren<EnemyAttack>();

        if (statusHandler == null)
            statusHandler = GetComponent<EnemyStatusHandler>();
    }

    protected override float ModifyIncomingDamage(float damage)
    {
        float finalDamage = damage;

        if (statusHandler != null)
            finalDamage *= statusHandler.DamageTakenMultiplier;

        return finalDamage;
    }

    protected override void OnHurtStart()
    {
        if (enemyAttack != null)
            enemyAttack.ForceStopAttack();

        if (enemyMovement != null)
        {
            enemyMovement.StopMoving();
            enemyMovement.SetCanMove(false);
        }
    }

    protected override void OnHurtEnd()
    {
        if (!isDead && enemyMovement != null)
            enemyMovement.SetCanMove(true);
    }

    protected override void OnDeathStart()
    {
        if (enemyAttack != null)
            enemyAttack.ForceStopAttack();

        if (enemyController != null)
            enemyController.Die();
    }
}
