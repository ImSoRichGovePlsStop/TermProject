using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float moveSpeed = 2.5f;
    [SerializeField] protected float stopDistance = 0.8f;

    [Header("References")]
    [SerializeField] protected Transform visualRoot;
    [SerializeField] protected SpriteRenderer spriteRenderer;

    protected EnemyStatusHandler statusHandler;
    protected Rigidbody rb;
    protected Vector3 moveDirection;
    protected bool canMove = true;

    public float MoveSpeed => moveSpeed;
    public float StopDistance => stopDistance;
    public Vector3 MoveDirection => moveDirection;
    public bool IsMoving => canMove && moveDirection.sqrMagnitude > 0.001f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        statusHandler = GetComponent<EnemyStatusHandler>();

        if (spriteRenderer == null && visualRoot != null)
            spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
    }

    public virtual void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    public virtual void MultiplyMoveSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed * multiplier);
    }

    public virtual void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            if (!rb.isKinematic)
                moveDirection = Vector3.zero;
                rb.linearVelocity = Vector3.zero;
        }
    }

    public virtual void MoveToTarget(Vector3 targetPosition)
    {
        if (!canMove)
        {
            moveDirection = Vector3.zero;
            return;
        }

        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        float distance = direction.magnitude;

        if (distance <= stopDistance)
        {
            moveDirection = Vector3.zero;
            return;
        }

        moveDirection = direction.normalized;
        FaceDirection(moveDirection);
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
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    protected virtual float GetCurrentMoveSpeed()
    {
        float currentSpeed = moveSpeed;

        if (statusHandler != null)
            currentSpeed *= statusHandler.MoveSpeedMultiplier;

        return currentSpeed;
    }

    protected virtual void FixedUpdate()
    {
        if (rb.isKinematic) return;

        if (!canMove || moveDirection.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float currentSpeed = GetCurrentMoveSpeed();

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    protected virtual void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (dir.x > 0.05f)
            spriteRenderer.flipX = false;
        else if (dir.x < -0.05f)
            spriteRenderer.flipX = true;
    }
}