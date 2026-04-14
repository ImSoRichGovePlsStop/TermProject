using UnityEngine;

public class EnemyMovementBase : MovementBase
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Sprite")]
    [SerializeField] private bool flipXByDefault = false;

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
        Transform spriteTransform = spriteRenderer.transform;
        Vector3 scale = spriteTransform.localScale;

        if (direction.x > 0.05f)
            scale.x = flipXByDefault ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        else if (direction.x < -0.05f)
            scale.x = flipXByDefault ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);

        spriteTransform.localScale = scale;
    }
}