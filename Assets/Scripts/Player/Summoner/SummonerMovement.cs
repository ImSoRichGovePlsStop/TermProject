using UnityEngine;

public class SummonerMovement : MovementBase
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    protected override void FaceDirection(Vector3 direction)
    {
        if (animator == null) return;

        Vector2 dir = new Vector2(direction.x, direction.z).normalized;

        Vector2 snapped;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
            snapped = new Vector2(Mathf.Sign(dir.x), 0f);
        else
            snapped = new Vector2(0f, Mathf.Sign(dir.y));

        animator.SetFloat("moveX", snapped.x);
        animator.SetFloat("moveY", snapped.y);
    }
}