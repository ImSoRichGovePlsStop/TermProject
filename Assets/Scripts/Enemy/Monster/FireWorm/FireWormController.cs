using System.Collections;
using UnityEngine;

public class FireWormController : EnemyBase
{
    public enum FireWormState { Wander, Chase, Strafe, Attack }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Shoot")]
    [SerializeField] private float shootRange = 6f;
    [SerializeField] private float shootCooldown = 2.5f;
    [SerializeField] private float projectileDamageScale = 1f;
    [SerializeField] private float spreadAngle = 0f;
    [SerializeField] private float maxTrackRotateSpeed = 90f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;

    private FireWormState currentState = FireWormState.Wander;
    private bool isShooting = false;
    private float lastShootTime = -Mathf.Infinity;
    private Vector3 lockedTargetPosition;
    private bool hasFired = false;

    public override bool CanBeInterrupted() => !hasFired;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        strafe.Init(shootRange);
    }

    protected override void UpdateState()
    {
        if (isShooting) return;

        if (!HasTarget)
        {
            if (currentState != FireWormState.Wander)
                wander.Reset(movement, stats);
            currentState = FireWormState.Wander;
            return;
        }

        FireWormState prev = currentState;
        if (currentState == FireWormState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canShoot = Time.time >= lastShootTime + shootCooldown;

        if (canShoot && dist <= shootRange)
            currentState = FireWormState.Attack;
        else if (dist > shootRange)
        {
            if (prev == FireWormState.Strafe) strafe.Reset();
            currentState = FireWormState.Chase;
        }
        else
            currentState = FireWormState.Strafe;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case FireWormState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;
            case FireWormState.Chase:
                movement.MoveToTarget(TargetPosition);
                break;
            case FireWormState.Strafe:
                strafe.Tick(transform, TargetPosition, movement);
                break;
            case FireWormState.Attack:
                TickAttack();
                break;
        }
    }

    private void TickAttack()
    {
        movement.StopMoving();

        if (HasTarget)
        {
            Vector3 currentDir = lockedTargetPosition - transform.position;
            Vector3 targetDir = TargetPosition - transform.position;
            currentDir.y = 0f;
            targetDir.y = 0f;
            if (currentDir.sqrMagnitude > 0.001f && targetDir.sqrMagnitude > 0.001f)
            {
                float maxDeg = maxTrackRotateSpeed * Time.deltaTime;
                Vector3 newDir = Vector3.RotateTowards(currentDir.normalized, targetDir.normalized, maxDeg * Mathf.Deg2Rad, 0f);
                lockedTargetPosition = transform.position + newDir * targetDir.magnitude;
            }
        }

        movement.FaceTarget(lockedTargetPosition);

        bool canShoot = Time.time >= lastShootTime + shootCooldown;
        if (canShoot) TryAttack();
    }

    private bool TryAttack()
    {
        if (isShooting) return true;
        if (Time.time < lastShootTime + shootCooldown) return false;
        isShooting = true;
        lockedTargetPosition = TargetPosition;
        animator?.SetTrigger("Attack");
        return true;
    }

    // Animation Events

    public void StartFlashBuildup(string args)
    {
        var parts = args.Split(',');
        int frames = int.Parse(parts[0]);
        int fps = int.Parse(parts[1]);
        health.StartFlashBuildup(Color.white, frames / (float)fps, 0.4f);
    }

    public void FlashWhite()
    {
        health.StopFlashBuildup();
        health.TryFlash(Color.white);
    }

    public void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        hasFired = true;
        SpawnProjectile();
    }

    public void FireLastProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnProjectile();
        isShooting = false;
        hasFired = false;
        lastShootTime = Time.time;
        TriggerPostAttackDelay();
    }

    // Helpers

    private void SpawnProjectile()
    {
        Vector3 baseDir = lockedTargetPosition - firePoint.position;
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = transform.forward;
        baseDir.Normalize();

        float angle = Random.Range(-spreadAngle, spreadAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        var proj = go.GetComponent<FireWormProjectile>();
        proj?.Initialize(firePoint.position + dir * 20f, stats.Damage * projectileDamageScale, health);
    }

    protected override void OnHurtTriggered()
    {
        isShooting = false;
        hasFired = false;
        strafe.Reset();
    }

    public override void OnDeath()
    {
        base.OnDeath();
        isShooting = false;
        StopAllCoroutines();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}