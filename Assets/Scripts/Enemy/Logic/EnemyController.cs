using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Hurt,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float loseTargetRange = 8f;
    [SerializeField] private float attackBuffer = 0.5f;

    [Header("References")]
    private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;

    [Header("Optional Elite")]
    [SerializeField] private EliteHopliteGuard eliteGuard;

    private EnemyState currentState = EnemyState.Idle;
    private bool isDead = false;
    private Coroutine attackLoopCoroutine;

    private void Awake()
    {
        GameObject playerInScene = GameObject.FindWithTag("Player");
        if (playerInScene != null)
            player = playerInScene.transform;

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponentInChildren<EnemyAttack>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (eliteGuard == null)
            eliteGuard = GetComponent<EliteHopliteGuard>();
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            ChangeState(EnemyState.Dead);
            movement.StopMoving();
            UpdateAnimation();
            return;
        }

        if (enemyHealth != null && enemyHealth.IsHurt)
        {
            ChangeState(EnemyState.Hurt);
            StopAttackLoop();
            movement.StopMoving();
            UpdateAnimation();
            return;
        }

        // Optional elite guard state:
        // while guarding -> no move, no chase, no face target, no attack
        if (eliteGuard != null && eliteGuard.IsGuarding)
        {
            ChangeState(EnemyState.Idle);
            StopAttackLoop();

            if (movement != null)
            {
                movement.StopMoving();
                movement.SetCanMove(false);
            }

            UpdateAnimation();
            return;
        }
        else
        {
            if (movement != null)
                movement.SetCanMove(true);
        }

        float distance = Vector3.Distance(
            GetFlatPosition(transform.position),
            GetFlatPosition(player.position)
        );

        float attackRange = enemyAttack != null ? enemyAttack.AttackRange : 1.2f;
        float attackExitRange = attackRange + attackBuffer;

        if (distance > loseTargetRange)
        {
            ChangeState(EnemyState.Idle);
        }
        else if (currentState == EnemyState.Attack)
        {
            ChangeState(distance <= attackExitRange ? EnemyState.Attack : EnemyState.Chase);
        }
        else
        {
            if (distance <= attackRange)
                ChangeState(EnemyState.Attack);
            else if (distance <= detectRange)
                ChangeState(EnemyState.Chase);
            else
                ChangeState(EnemyState.Idle);
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                movement.SetCanMove(true);
                movement.StopMoving();
                break;

            case EnemyState.Chase:
                movement.SetCanMove(true);
                movement.MoveToTarget(player.position);
                break;

            case EnemyState.Attack:
                movement.SetCanMove(true);
                movement.StopMoving();
                movement.FaceTarget(player.position);

                if (attackLoopCoroutine == null)
                    attackLoopCoroutine = StartCoroutine(AttackLoop());
                break;

            case EnemyState.Hurt:
                movement.StopMoving();
                break;

            case EnemyState.Dead:
                movement.StopMoving();
                movement.SetCanMove(false);
                break;
        }

        UpdateAnimation();
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            if (newState == EnemyState.Attack && attackLoopCoroutine == null)
                attackLoopCoroutine = StartCoroutine(AttackLoop());

            return;
        }

        ExitState(currentState);
        currentState = newState;
        EnterState(newState);
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
                break;

            case EnemyState.Hurt:
            case EnemyState.Dead:
                StopAttackLoop();
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        if (state == EnemyState.Attack)
            StopAttackLoop();
    }

    private IEnumerator AttackLoop()
    {
        while (currentState == EnemyState.Attack && !isDead)
        {
            if (enemyHealth != null && (enemyHealth.IsDead || enemyHealth.IsHurt))
                break;

            if (eliteGuard != null && eliteGuard.IsGuarding)
                break;

            if (enemyAttack != null && enemyAttack.CanAttack())
            {
                enemyAttack.StartAttack();

                float timer = 0f;
                float timeout = 2f;

                while (enemyAttack.IsAttacking && timer < timeout)
                {
                    if (eliteGuard != null && eliteGuard.IsGuarding)
                    {
                        enemyAttack.ForceStopAttack();
                        break;
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                yield return new WaitForSeconds(enemyAttack.AttackCooldown);
            }
            else
            {
                yield return null;
            }
        }

        attackLoopCoroutine = null;
    }

    private void StopAttackLoop()
    {
        if (attackLoopCoroutine != null)
        {
            StopCoroutine(attackLoopCoroutine);
            attackLoopCoroutine = null;
        }

        if (enemyAttack != null)
            enemyAttack.ForceStopAttack();
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", movement != null && movement.IsMoving);
    }

    private Vector3 GetFlatPosition(Vector3 pos)
    {
        return new Vector3(pos.x, 0f, pos.z);
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        ChangeState(EnemyState.Dead);

        if (movement != null)
        {
            movement.StopMoving();
            movement.SetCanMove(false);
        }

        StopAttackLoop();
    }
}