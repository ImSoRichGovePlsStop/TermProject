using System.Collections;
using UnityEngine;

public class WarlockController : EnemyBase
{
    public enum WarlockState { Wander, Chase, Strafe, WindUp }

    [Header("Strafe")]
    [SerializeField] private StrafeBehavior strafe;

    [Header("Shoot")]
    [SerializeField] protected float shootRange = 6f;
    [SerializeField] protected float shootCooldown = 2.5f;
    [SerializeField] protected float projectileDamageScale = 1f;
    [SerializeField] protected float spreadAngle = 15f;
    [SerializeField] protected float maxTrackRotateSpeed = 90f;
    [SerializeField] protected GameObject projectilePrefab;
    [SerializeField] protected Transform firePoint;

    [Header("AoE Smash")]
    [SerializeField] protected float smashRange = 5f;
    [SerializeField] protected float smashDamageScale = 1.2f;
    [SerializeField] protected float smashWindUpDuration = 0.8f;
    [SerializeField] protected float smashWarningDuration = 1f;
    [SerializeField] protected float smashAoeRadius = 2.5f;
    [SerializeField] protected float smashCooldown = 5f;
    [SerializeField] protected GameObject aoeWarningPrefab;
    [SerializeField] protected LayerMask targetLayers;

    protected WarlockState currentState = WarlockState.Wander;
    protected bool isWindingUp = false;
    protected bool isSmashing = false;
    protected float lastShootTime = -Mathf.Infinity;
    protected float lastSmashTime = 0f;
    protected Vector3 lockedTargetPosition;
    private bool hasFired = false;
    private bool isSmashExecuting = false;

    public override bool CanBeInterrupted() => !hasFired && !isSmashExecuting;

    protected override void Awake()
    {
        base.Awake();
        movement.SetStopDistance(0.1f);
        strafe.Init(shootRange);
    }

    protected override void UpdateState()
    {
        if (isWindingUp || isSmashing) return;

        WarlockState prevState = currentState;

        if (!HasTarget)
        {
            currentState = WarlockState.Wander;
            return;
        }

        if (prevState == WarlockState.Wander)
            wander.Reset(movement, stats);

        float dist = Vector3.Distance(transform.position, TargetPosition);
        bool canShoot = Time.time >= lastShootTime + shootCooldown;

        if (canShoot && dist <= shootRange) currentState = WarlockState.WindUp;
        else if (dist > shootRange) { strafe.Reset(); currentState = WarlockState.Chase; }
        else currentState = WarlockState.Strafe;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case WarlockState.Wander: TickWander(); break;
            case WarlockState.Chase: TickChase(); break;
            case WarlockState.Strafe: TickStrafe(); break;
            case WarlockState.WindUp: TickWindUp(); break;
        }
    }

    private void TickWander()
    {
        wander.Tick(transform, transform, movement, stats);
    }

    private void TickChase()
    {
        movement.MoveToTarget(TargetPosition);
    }

    private void TickStrafe()
    {
        strafe.Tick(transform, TargetPosition, movement);
    }

    private void TickWindUp()
    {
        movement.StopMoving();

        if (HasTarget)
        {
            Vector3 currentDir = (lockedTargetPosition - transform.position);
            Vector3 targetDir = (TargetPosition - transform.position);
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
        if (!TrySmash()) TryWindUp();
    }

    protected virtual bool TrySmash()
    {
        if (isSmashing) return true;
        if (isWindingUp) return false;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        if (!HasTarget || Vector3.Distance(transform.position, TargetPosition) > smashRange) return false;

        StartCoroutine(SmashRoutine());
        return true;
    }

    private System.Collections.IEnumerator SmashRoutine()
    {
        isSmashing = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");

        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / smashWindUpDuration;
        }

        yield return new UnityEngine.WaitForSeconds(smashWindUpDuration);
        if (animator != null) animator.speed = 1f;

        isSmashExecuting = true;
        SpawnAOEWarning(TargetPosition, smashDamageScale, smashAoeRadius, smashWarningDuration);

        animator?.SetTrigger("Smash");
        yield return new UnityEngine.WaitForSeconds(smashWarningDuration);

        lastSmashTime = Time.time;
        isSmashing = false;
        isSmashExecuting = false;
        TriggerPostAttackDelay();
    }

    protected void SpawnAOEWarning(Vector3 pos, float damageScale, float radius, float duration)
    {
        if (aoeWarningPrefab == null) return;
        var go = Instantiate(aoeWarningPrefab, pos, Quaternion.identity);
        go.GetComponent<WarlockAOEWarning>()?.Initialize(stats.Damage * damageScale, radius, duration, targetLayers, health);
    }

    protected bool TryWindUp()
    {
        if (isWindingUp) return true;
        if (Time.time < lastShootTime + shootCooldown) return false;

        isWindingUp = true;
        lockedTargetPosition = TargetPosition;
        animator?.SetTrigger("WindUp");
        return true;
    }

    // Animation Events
    public virtual void StartFlashBuildup(string args)
    {
        var parts = args.Split(',');
        int frames = int.Parse(parts[0]);
        int fps = int.Parse(parts[1]);
        float duration = frames / (float)fps;
        health.StartFlashBuildup(Color.white, duration, 0.4f);
    }

    public virtual void FlashWhite()
    {
        health.TryFlash(Color.white);
    }

    public virtual void FireProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        hasFired = true;
        SpawnSingleProjectile();
    }

    public virtual void FireLastProjectile()
    {
        if (projectilePrefab == null || firePoint == null) return;
        SpawnSingleProjectile();
        isWindingUp = false;
        hasFired = false;
        lastShootTime = Time.time;
        TriggerPostAttackDelay();
    }

    protected void SpawnSingleProjectile()
    {
        Vector3 baseDir = lockedTargetPosition - firePoint.position;
        baseDir.y = 0f;
        if (baseDir.sqrMagnitude < 0.001f) baseDir = transform.forward;
        baseDir.Normalize();

        float angle = Random.Range(-spreadAngle, spreadAngle);
        Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;

        var go = Instantiate(projectilePrefab, firePoint.position, projectilePrefab.transform.rotation);
        var proj = go.GetComponent<WarlockProjectile>();
        proj?.Initialize(firePoint.position + dir * 20f, stats.Damage * projectileDamageScale, health);
    }

    protected override void OnHurtTriggered()
    {
        isWindingUp = false;
        isSmashing = false;
        isSmashExecuting = false;
        hasFired = false;
        strafe.Reset();
    }

    public override void OnDeath()
    {
        base.OnDeath();
        isWindingUp = false;
        isSmashing = false;
        StopAllCoroutines();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, shootRange);
    }
}