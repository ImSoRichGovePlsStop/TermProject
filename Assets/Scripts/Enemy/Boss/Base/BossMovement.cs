using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 2f;

    [Header("References")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool canMove = true;

    public float MoveSpeed => moveSpeed;
    public float StopDistance => stopDistance;
    public bool IsMoving => canMove && moveDirection.sqrMagnitude > 0.001f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (spriteRenderer == null && visualRoot != null)
            spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
    }

    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = Mathf.Max(0f, newSpeed);
    }

    public void MultiplyMoveSpeed(float multiplier)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed * multiplier);
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
            StopMoving();
            return;
        }

        moveDirection = direction.normalized;
        FaceDirection(moveDirection);
    }

    public void StopMoving()
    {
        moveDirection = Vector3.zero;
        rb.linearVelocity = Vector3.zero;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            FaceDirection(direction.normalized);
    }

    public void DashTo(Vector3 worldDirection, float dashSpeed)
    {
        Vector3 flatDir = worldDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.001f)
            return;

        flatDir.Normalize();
        FaceDirection(flatDir);
        rb.linearVelocity = flatDir * dashSpeed;
    }

    private void FixedUpdate()
    {
        if (!canMove || moveDirection.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;
    }

    private void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (dir.x > 0.05f)
            spriteRenderer.flipX = true;
        else if (dir.x < -0.05f)
            spriteRenderer.flipX = false;
    }
}