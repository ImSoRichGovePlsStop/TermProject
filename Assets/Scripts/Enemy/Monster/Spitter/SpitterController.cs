using UnityEngine;

public class SpitterController : EnemyBase
{
    public enum SpitterState { Wander, Attack }

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 4f;
    [SerializeField] private int projectileCountMin = 3;
    [SerializeField] private int projectileCountMax = 5;
    [SerializeField] private float spreadAngle = 90f;
    [SerializeField] private float projectileDamageScale = 1f;
    [SerializeField] private GameObject projectilePrefab;

    private SpitterState currentState = SpitterState.Wander;
    private bool isAttacking = false;
    private float lastAttackTime = -Mathf.Infinity;
    private SpriteRenderer spriteRenderer;

    private static readonly Vector3[] CardinalDirs = new Vector3[]
    {
        Vector3.right,
        Vector3.back,
        Vector3.left,
        Vector3.forward,
    };

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    protected override void UpdateState()
    {
        if (isAttacking) return;

        if (Time.time >= lastAttackTime + attackCooldown)
            currentState = SpitterState.Attack;
        else
            currentState = SpitterState.Wander;
    }

    protected override void TickState()
    {
        switch (currentState)
        {
            case SpitterState.Wander:
                wander.Tick(transform, transform, movement, stats);
                break;
            case SpitterState.Attack:
                if (!isAttacking)
                {
                    isAttacking = true;
                    movement.StopMoving();
                    animator?.SetTrigger("Attack");
                }
                break;
        }
    }

    public void FireSpread(int dirIndex)
    {
        if (projectilePrefab == null) return;

        bool flipped = spriteRenderer != null && spriteRenderer.flipX;
        if (flipped)
        {
            if (dirIndex == 0) dirIndex = 2;
            else if (dirIndex == 2) dirIndex = 0;
        }

        Vector3 baseDir = CardinalDirs[dirIndex % CardinalDirs.Length];
        int count = Random.Range(projectileCountMin, projectileCountMax + 1);
        Vector3 spawnPos = transform.position;

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(-spreadAngle * 0.5f, spreadAngle * 0.5f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * baseDir;
            var go = Instantiate(projectilePrefab, spawnPos, projectilePrefab.transform.rotation);
            go.GetComponent<SpitterProjectile>()?.Initialize(spawnPos + dir * 20f, stats.Damage * projectileDamageScale, health);
        }
    }

    public void FinishAttack()
    {
        lastAttackTime = Time.time;
        isAttacking = false;
        TriggerPostAttackDelay();
    }

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

    protected override void OnHurtTriggered()
    {
        isAttacking = false;
    }

    public override void OnDeath()
    {
        base.OnDeath();
        isAttacking = false;
    }

    public override bool CanBeInterrupted() => true;
}