using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.2f;

    [Header("References")]
    [SerializeField] private Transform visualRoot;

    private Rigidbody rb;
    private Vector3 moveDirection;
    private bool canMove = true;
    private Vector3 defaultVisualScale;

    public float MoveSpeed => moveSpeed;
    public float StopDistance => stopDistance;
    public Vector3 MoveDirection => moveDirection;
    public bool IsMoving => canMove && moveDirection.sqrMagnitude > 0.001f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (visualRoot != null)
            defaultVisualScale = visualRoot.localScale;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;

        if (!canMove)
        {
            moveDirection = Vector3.zero;
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
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
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private void FixedUpdate()
    {
        if (!canMove || moveDirection.sqrMagnitude < 0.001f)
        {
            // rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        // Vector3 velocity = moveDirection * moveSpeed;
        // velocity.y = rb.linearVelocity.y;
        // rb.linearVelocity = velocity;
        Vector3 move = moveDirection * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);
    }

    private void FaceDirection(Vector3 dir)
    {
        if (visualRoot == null) return;
        if (dir.sqrMagnitude < 0.001f) return;

        Vector3 scale = defaultVisualScale;

        if (dir.x > 0.05f)
            scale.x = Mathf.Abs(defaultVisualScale.x);   // หันขวา
        else if (dir.x < -0.05f)
            scale.x = -Mathf.Abs(defaultVisualScale.x);  // หันซ้าย
        else
            scale.x = visualRoot.localScale.x; // ถ้าแทบไม่ขยับแกน x ก็ไม่เปลี่ยนหน้า

        visualRoot.localScale = scale;
    }
}