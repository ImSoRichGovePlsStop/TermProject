using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 0.8f;

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private EnemyStatusHandler statusHandler;
    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool canMove = true;

    public float MoveSpeed => moveSpeed;
    public float StopDistance => stopDistance;
    public Vector3 MoveDirection => moveDirection;
    public bool IsMoving => canMove && moveDirection.sqrMagnitude > 0.001f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        statusHandler = GetComponent<EnemyStatusHandler>();

        if (spriteRenderer == null && visualRoot != null)
            spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            moveDirection = Vector3.zero;
            rb.linearVelocity = Vector3.zero;
        }
    }

    public void MoveToTarget(Vector3 targetPosition)
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

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            FaceDirection(direction.normalized);
    }

    public void StopMoving()
    {
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (rb.isKinematic) return;
        if (!canMove || moveDirection.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        float currentSpeed = moveSpeed;
        if (statusHandler != null)
            currentSpeed *= statusHandler.MoveSpeedMultiplier;

        Vector3 velocity = moveDirection * currentSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (dir.x > 0.05f)
            spriteRenderer.flipX = false;
        else if (dir.x < -0.05f)
            spriteRenderer.flipX = true;
    }
}