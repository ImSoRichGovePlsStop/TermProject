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
    [SerializeField] private float attackBuffer = 0.2f;

    [Header("References")]
    private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;

    private EnemyState currentState = EnemyState.Idle;
    private bool isDead = false;

    private void Awake()
    {
        GameObject playerInScene = GameObject.FindWithTag("Player");
        if (playerInScene != null)
            player = playerInScene.transform;

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        if (enemyHealth != null && enemyHealth.IsDead)
        {
            ChangeState(EnemyState.Dead);
            movement.StopMoving();
            movement.SetCanMove(false);
            UpdateAnimation();
            return;
        }

        if (enemyHealth != null && enemyHealth.IsHurt)
        {
            ChangeState(EnemyState.Hurt);
            movement.StopMoving();
            UpdateAnimation();
            return;
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

                if (enemyAttack != null && enemyAttack.CanAttack())
                {
                    Debug.Log("[EnemyController] StartAttack");
                    enemyAttack.StartAttack();
                }
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
            return;

        currentState = newState;
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
    }
}