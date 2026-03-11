using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Dead
    }

    [Header("Detection")]
    [SerializeField] private float detectRange = 6f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float loseTargetRange = 8f;

    [Header("References")]
    private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private Animator animator;

    private EnemyState currentState = EnemyState.Idle;
    private bool isDead = false;

    public EnemyState CurrentState => currentState;

    private void Awake()
    {
        GameObject playerInScene = GameObject.FindWithTag("Player");
        if (playerInScene != null)
            player = playerInScene.transform;

        if (movement == null)
            movement = GetComponent<EnemyMovement>();

        if (enemyAttack == null)
            enemyAttack = GetComponent<EnemyAttack>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isDead || player == null)
            return;

        float distanceToPlayer = Vector3.Distance(
            GetFlatPosition(transform.position),
            GetFlatPosition(player.position)
        );

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(distanceToPlayer);
                break;

            case EnemyState.Chase:
                HandleChase(distanceToPlayer);
                break;

            case EnemyState.Attack:
                HandleAttack(distanceToPlayer);
                break;
        }

        UpdateAnimation(distanceToPlayer);
    }

    private void HandleIdle(float distanceToPlayer)
    {
        movement.StopMoving();

        if (distanceToPlayer <= detectRange)
            ChangeState(EnemyState.Chase);
    }

    private void HandleChase(float distanceToPlayer)
    {
        if (distanceToPlayer > loseTargetRange)
        {
            movement.StopMoving();
            ChangeState(EnemyState.Idle);
            return;
        }

        if (distanceToPlayer <= attackRange)
        {
            movement.StopMoving();
            ChangeState(EnemyState.Attack);
            return;
        }

        movement.SetCanMove(true);
        movement.MoveToTarget(player.position);
    }

    private void HandleAttack(float distanceToPlayer)
    {
        movement.StopMoving();
        movement.SetCanMove(false);
        movement.FaceTarget(player.position);

        if (distanceToPlayer > attackRange)
        {
            movement.SetCanMove(true);
            ChangeState(EnemyState.Chase);
            return;
        }

        if (enemyAttack != null && enemyAttack.CanAttack())
        {
            DoAttack();
        }
    }

    private void DoAttack()
    {
        if (enemyAttack != null)
            enemyAttack.Attack();
    }

    private void UpdateAnimation(float distanceToPlayer)
    {
        if (animator == null) return;

        bool isMoving = movement != null && movement.IsMoving;

        animator.SetBool("IsMoving", isMoving);
        animator.SetBool("IsAttacking", currentState == EnemyState.Attack && distanceToPlayer <= attackRange);
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

    private Vector3 GetFlatPosition(Vector3 pos)
    {
        return new Vector3(pos.x, 0f, pos.z);
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = EnemyState.Dead;

        if (movement != null)
        {
            movement.StopMoving();
            movement.SetCanMove(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(transform.position, loseTargetRange);
    }
}