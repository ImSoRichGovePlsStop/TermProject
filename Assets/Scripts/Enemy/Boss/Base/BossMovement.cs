using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossMovement : EnemyMovement
{
    [Header("Boss Movement")]
    [SerializeField] private bool invertFacing = true;

    protected override void Awake()
    {
        base.Awake();
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

    protected override void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (!invertFacing)
        {
            base.FaceDirection(dir);
            return;
        }

        if (dir.x > 0.05f)
            spriteRenderer.flipX = true;
        else if (dir.x < -0.05f)
            spriteRenderer.flipX = false;
    }
}