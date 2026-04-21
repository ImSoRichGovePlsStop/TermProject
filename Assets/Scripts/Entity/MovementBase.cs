using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public abstract class MovementBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float stopDistance = 0.8f;

    [Header("References")]
    [SerializeField] protected SpriteRenderer spriteRenderer;

    protected NavMeshAgent agent;
    protected EntityStats stats;
    protected bool canMove = true;

    public float StopDistance => stopDistance;
    public bool IsMoving => canMove && agent != null && agent.velocity.sqrMagnitude > 0.001f;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stopDistance;
        agent.angularSpeed = 0f;
        agent.acceleration = 50f;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        stats = GetComponent<EntityStats>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public NavMeshAgent GetAgent() => agent;

    public void SetStopDistance(float distance)
    {
        stopDistance = distance;
        if (agent != null)
            agent.stoppingDistance = distance;
    }

    protected virtual void Update()
    {
        if (!canMove) return;
        if (agent.velocity.sqrMagnitude > 0.001f)
            FaceDirection(agent.velocity.normalized);

        agent.speed = stats != null ? stats.MoveSpeed : 3f;
    }

    public virtual void MoveToTarget(Vector3 targetPosition)
    {
        if (!canMove) return;
        if (!agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(targetPosition);
    }

    public virtual void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            FaceDirection(direction.normalized);
    }

    public virtual void StopMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    public virtual void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
            StopMoving();
    }

    protected abstract void FaceDirection(Vector3 direction);
}