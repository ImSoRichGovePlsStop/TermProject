using UnityEngine;

public class EnemyMovementBase : MovementBase
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected override void Update()
    {
        base.Update();

        if (animator != null)
            animator.SetBool("IsMoving", IsMoving);
    }

    protected override void FaceDirection(Vector3 direction)
    {
        if (spriteRenderer == null) return;

        if (direction.x > 0.05f)
            spriteRenderer.flipX = false;
        else if (direction.x < -0.05f)
            spriteRenderer.flipX = true;
    }
}