using UnityEngine;

public class BossMovement : MovementBase
{
    [Header("Boss Movement")]
    [SerializeField] private bool invertFacing = true;

    private EntityStatModifier bossMoveModifier = new EntityStatModifier();

    public void DashTo(Vector3 worldDirection, float dashSpeed)
    {
        Vector3 flatDir = worldDirection;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude <= 0.001f)
            return;

        flatDir.Normalize();
        FaceDirection(flatDir);

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = flatDir * dashSpeed;
    }

    public void MultiplyMoveSpeed(float multiplier)
    {
        if (stats == null) return;
        stats.RemoveMultiplierModifier(bossMoveModifier);
        bossMoveModifier.moveSpeed = multiplier - 1f;
        stats.AddMultiplierModifier(bossMoveModifier);
    }

    protected override void FaceDirection(Vector3 dir)
    {
        if (spriteRenderer == null) return;

        if (!invertFacing)
        {
            if (dir.x > 0.05f) spriteRenderer.flipX = false;
            else if (dir.x < -0.05f) spriteRenderer.flipX = true;
            return;
        }

        if (dir.x > 0.05f) spriteRenderer.flipX = true;
        else if (dir.x < -0.05f) spriteRenderer.flipX = false;
    }
}