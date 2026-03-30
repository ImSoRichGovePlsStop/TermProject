using UnityEngine;

public class DefaultEnemyHealth : EnemyHealth
{
    [Header("Default Enemy References")]
    [SerializeField] private EnemyController enemyController;
    [SerializeField] private EnemyMovement enemyMovement;

    private EnemyStatusHandler statusHandler;

    protected override void CacheComponents()
    {
        if (enemyController == null)
            enemyController = GetComponent<EnemyController>();

        if (enemyMovement == null)
            enemyMovement = GetComponent<EnemyMovement>();


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

}
