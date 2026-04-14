using UnityEngine;

[RequireComponent(typeof(DummyHealthBase))]
public class DummyController : EnemyBase
{
    protected override void Start()
    {
        // skip spawn animation
    }

    protected override void UpdateState() { }

    protected override void TickState() { }

    protected override void OnHurtTriggered()
    {
        if (HasTarget)
            movement.FaceTarget(TargetPosition);
    }

    public override void OnDeath()
    {
        // reset HP instead of dying
        health.ResetHP();
    }
}