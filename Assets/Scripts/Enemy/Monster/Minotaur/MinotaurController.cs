using System.Collections;
using UnityEngine;

public class MinotaurController : EnemyBase
{
    public enum MinotaurState
    {
        Wander,
        Chase,
        Attack
    }

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackAngle = 120f;
    [SerializeField] private float attackDamageScale = 1f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;

    private MinotaurState currentState = MinotaurState.Wander;
    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    private bool isDashing = false;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(attackRange * 0.8f);
    }

    protected override void UpdateState()
    {
        if (isAttacking)
        {
            currentState = MinotaurState.Attack;
            return;
        }

        MinotaurState prevState = currentState;

        if (!HasTarget)
        {
            currentState = MinotaurState.Wander;
        }
        else
        {
            float dist = Vector3.Distance(transform.position, TargetPosition);

            switch (currentState)
            {
                case MinotaurState.Wander:
                case MinotaurState.Chase:
                    currentState = dist <= attackRange ? MinotaurState.Attack : MinotaurState.Chase;
                    break;

                case MinotaurState.Attack:
                    if (dist > attackRange)
                        currentState = MinotaurState.Chase;
                    break;
            }
        }

        if (prevState == MinotaurState.Wander && currentState != MinotaurState.Wander)
            wander.Reset(movement);
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case MinotaurState.Wander:
                wander.Tick(transform, transform, movement);
                break;

            case MinotaurState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;

            case MinotaurState.Attack:
                movement.FaceTarget(TargetPosition);
                if (!TryAttack())
                    movement.MoveToTarget(TargetPosition);
                else
                    movement.StopMoving();
                break;
        }
    }

    private bool TryAttack()
    {
        if (isAttacking) return true;
        if (Time.time < lastAttackTime + attackCooldown) return false;

        isAttacking = true;
        animator.SetTrigger("Attack");
        return true;
    }

    // Animation Event
    public void DashToTarget()
    {
        if (isDashing || !HasTarget) return;

        float dist = Vector3.Distance(transform.position, TargetPosition);
        if (dist <= attackRange) return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        isDashing = true;
        movement.SetCanMove(false);

        var agent = movement.GetAgent();
        if (agent != null) agent.enabled = false;

        Vector3 dashDir = (TargetPosition - transform.position);
        dashDir.y = 0f;
        dashDir.Normalize();

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            transform.position += dashDir * dashSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (agent != null) agent.enabled = true;
        movement.SetCanMove(true);
        isDashing = false;
    }

    // Animation Event
    public void DealDamage()
    {
        DealDamageToTarget(stats.Damage * attackDamageScale, attackAngle, attackRange);
    }

    // Animation Event
    public void FinishAttack()
    {
        isAttacking = false;
        lastAttackTime = Time.time;
        TriggerPostAttackDelay();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}