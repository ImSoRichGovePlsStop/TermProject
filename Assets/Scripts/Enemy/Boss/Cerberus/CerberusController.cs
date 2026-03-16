using UnityEngine;

public class CerberusController : BaseBossController
{
    public enum CerberusPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("Cerberus Phase")]
    [SerializeField] private CerberusPhase currentPhase = CerberusPhase.Phase1;

    [Header("Ranges")]
    [SerializeField] private float detectRange = 10f;
    [SerializeField] private float attackMaxRange = 6f;

    [Header("Timing")]
    [SerializeField] private float thinkInterval = 0.1f;

    [Header("Cerberus References")]
    [SerializeField] private BossMovement movement;
    [SerializeField] private CerberusAttack attack;
    [SerializeField] private CerberusHealth health;

    private float thinkTimer;

    public CerberusPhase CurrentPhase => currentPhase;

    private void Log(string msg)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[CerberusController] {msg}");
    }

    protected override void Awake()
    {
        base.Awake();

        if (movement == null)
            movement = GetComponent<BossMovement>();

        if (attack == null)
            attack = GetComponent<CerberusAttack>();

        if (health == null)
            health = GetComponent<CerberusHealth>();
    }

    protected override void TickStateLogic()
    {
        if (health != null && health.IsDead)
        {
            // Log("Health says dead -> Die()");
            Die();
            return;
        }

        if (health != null && health.IsHurt)
        {
            // Log("Health says hurt -> enter Hurt state");
            ChangeState(BossState.Hurt);

            if (movement != null)
                movement.StopMoving();

            if (attack != null)
                attack.ForceStopAllAttacks();

            return;
        }

        if (currentState == BossState.PhaseTransition)
        {
            // Log("Currently in PhaseTransition");
            if (movement != null)
                movement.StopMoving();
            return;
        }

        thinkTimer -= Time.deltaTime;
        if (thinkTimer > 0f)
            return;

        thinkTimer = thinkInterval;

        float distance = GetFlatDistanceToPlayer();

        // Log($"Tick | state={currentState} | phase={currentPhase} | distance={distance:F3} | detectRange={detectRange:F2} | attackMaxRange={attackMaxRange:F2} | attackBusy={(attack != null && attack.IsBusy)}");

        if (attack != null && attack.IsBusy)
        {
            // Log("Attack is busy -> keep Attack state");
            ChangeState(BossState.Attack);
            return;
        }

        if (distance > detectRange)
        {
            // Log("Distance > detectRange -> Idle");
            ChangeState(BossState.Idle);
            movement.StopMoving();
            return;
        }

        if (distance > attackMaxRange)
        {
            // Log("Distance > attackMaxRange -> Chase");
            ChangeState(BossState.Chase);
            movement.SetCanMove(true);
            movement.MoveToTarget(player.position);
            return;
        }

        bool started = false;
        if (attack != null)
            started = attack.TryStartBestAttack(distance, player.position, currentPhase);

        if (started)
        {
            // Log("Attack started successfully -> Attack state");
            ChangeState(BossState.Attack);
            movement.StopMoving();
            movement.FaceTarget(player.position);
            return;
        }

        float closeRange = attack != null ? Mathf.Max(attack.BiteRange, attack.FlameRange) : 1f;
        // Log($"No attack started | closeRange={closeRange:F3}");

        if (distance > closeRange)
        {
            // Log("Distance > closeRange -> Chase closer");
            ChangeState(BossState.Chase);
            movement.SetCanMove(true);
            movement.MoveToTarget(player.position);
        }
        else
        {
            // Log("Distance <= closeRange but no attack available -> face target and wait");
            ChangeState(BossState.Attack);
            movement.StopMoving();
            movement.FaceTarget(player.position);
        }
    }

    public void EnterPhase2()
    {
        if (currentPhase != CerberusPhase.Phase1) return;

        Log("EnterPhase2()");
        currentPhase = CerberusPhase.Phase2;
        ChangeState(BossState.PhaseTransition);

        if (attack != null)
            attack.ForceStopAllAttacks();

        if (movement != null)
            movement.StopMoving();

        if (animator != null)
            animator.SetTrigger("Split");
    }

    public void EnterPhase3()
    {
        if (currentPhase == CerberusPhase.Phase3) return;

        Log("EnterPhase3()");
        currentPhase = CerberusPhase.Phase3;
        ChangeState(BossState.PhaseTransition);

        if (attack != null)
        {
            attack.ForceStopAllAttacks();
            attack.ApplyPhase3Buff();
        }

        if (movement != null)
        {
            movement.StopMoving();
            movement.MultiplyMoveSpeed(1.2f);
        }

        if (animator != null)
            animator.SetTrigger("Merge");
    }

    public void FinishPhaseTransition()
    {
        if (isDead) return;
        Log("FinishPhaseTransition() -> Chase");
        ChangeState(BossState.Chase);
    }

    protected override void UpdateAnimation()
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", movement != null && movement.IsMoving);
    }

    public override void Die()
    {
        if (isDead) return;

        Log("Die()");
        base.Die();

        if (movement != null)
        {
            movement.StopMoving();
            movement.SetCanMove(false);
        }

        if (attack != null)
            attack.ForceStopAllAttacks();
    }
}