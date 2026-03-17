using UnityEngine;

public abstract class BaseBossController : MonoBehaviour
{
    public enum BossState
    {
        Idle,
        Chase,
        Attack,
        PhaseTransition,
        Hurt,
        Dead
    }

    [Header("Base References")]
    [SerializeField] protected Transform player;
    [SerializeField] protected Animator animator;

    protected BossState currentState = BossState.Idle;
    protected bool isDead = false;

    public BossState CurrentState => currentState;
    public bool IsDead => isDead;

    protected virtual void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

       if (player == null)
        {
    GameObject playerInScene = GameObject.FindWithTag("Player");
    if (playerInScene != null)
        player = playerInScene.transform;
        }
    }

    protected virtual void Update()
    {
        if (isDead || player == null)
            return;

        TickStateLogic();
        UpdateAnimation();
    }

    protected abstract void TickStateLogic();
    protected abstract void UpdateAnimation();

    protected virtual Vector3 GetFlatPosition(Vector3 pos)
    {
        return new Vector3(pos.x, 0f, pos.z);
    }

    protected virtual float GetFlatDistanceToPlayer()
    {
        if (player == null) return Mathf.Infinity;

        return Vector3.Distance(
            GetFlatPosition(transform.position),
            GetFlatPosition(player.position)
        );
    }

    protected virtual void ChangeState(BossState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }

    public virtual void Die()
    {
        if (isDead) return;

        isDead = true;
        ChangeState(BossState.Dead);
    }
}