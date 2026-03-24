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
    [SerializeField] private float attackBuffer = 0.5f; //0.2f

    [Header("References")]
    private Transform player;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private EnemyAttack enemyAttack;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private Animator animator;

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
            return;
        }

        if (enemyHealth != null && enemyHealth.IsHurt)
        {
            ChangeState(EnemyState.Hurt);
            movement.StopMoving();
            UpdateAnimation();
            return;
        }

        //ระยะจากผู้เล่นถึง Enemy
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
        else if (currentState == EnemyState.Attack) //กำลังโจมตีอยู่แล้ว
        {
            //ถ้ายังไม่ได้ออกจากระยะ ก็ Attack ต่อ ไม่งั้นก็ Chase
            Debug.Log("CASE1");
            ChangeState(distance <= attackExitRange ? EnemyState.Attack : EnemyState.Chase);
        }
        else //ใดๆก็ตาม
        {
            //ถ้าอยู่ในระยะตี ตีเลย ไม่งั้นก็ chase
            Debug.Log("CASE2");
            ChangeState(distance <= attackRange ? EnemyState.Attack : EnemyState.Chase);
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
                // if (enemyAttack != null && enemyAttack.IsAttacking)
                // {
                //     movement.StopMoving();
                // }
                // else 
                // {
                //     movement.SetCanMove(true);
                //     movement.MoveToTarget(player.position);
                // }
                if (attackLoopCoroutine == null)
                {
                    attackLoopCoroutine = StartCoroutine(AttackLoop());
                }
                movement.FaceTarget(player.position);
                break;
        }

        UpdateAnimation();
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
        {
            if (newState == EnemyState.Attack && attackLoopCoroutine == null)
            {
                attackLoopCoroutine = StartCoroutine(AttackLoop());
            }
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
                // if (attackLoopCoroutine == null)
                //     attackLoopCoroutine = StartCoroutine(AttackLoop());
                // if (attackLoopCoroutine != null)
                //     StopCoroutine(attackLoopCoroutine);
                // attackLoopCoroutine = StartCoroutine(AttackLoop());
                break;

            case EnemyState.Hurt:
            case EnemyState.Dead:
                // StopAttackLoop();
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
        // Debug.Log("AttackLoop running...");
        // while (currentState == EnemyState.Attack && !isDead)
        // {
        //     if (enemyHealth != null && (enemyHealth.IsDead || enemyHealth.IsHurt))
        //         yield break;

        //     if (enemyAttack != null && enemyAttack.CanAttack())
        //     {
        //         Debug.Log("Controller starts attack loop hit");
        //         enemyAttack.StartAttack();
        //     }

        //     // float waitTime = enemyAttack != null ? enemyAttack.AttackCooldown : 1f;
        //     // yield return new WaitForSeconds(waitTime);
        //     yield return new WaitForSeconds(0.05f);
        // }

        // attackLoopCoroutine = null;
        
        while (currentState == EnemyState.Attack && !isDead)
        {
            if (enemyAttack != null && enemyAttack.CanAttack())
            {
                enemyAttack.StartAttack();
                
                // รอจนกว่าแอนิเมชันจะจบ (FinishAttack ถูกเรียก)
                yield return new WaitUntil(() => !enemyAttack.IsAttacking);
                
                // พักตาม Cooldown จริงๆ
                yield return new WaitForSeconds(enemyAttack.AttackCooldown);
            }
            else
            {
                // ถ้ายังตีไม่ได้ (ติด Cooldown) ให้รอสั้นๆ แล้วเช็คใหม่
                yield return new WaitForSeconds(0.5f);
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
    }
}