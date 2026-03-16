using UnityEngine;
using System.Collections.Generic;

public class CerberusAttack : BaseBossAttack
{
    public enum AttackKind
    {
        None,
        BiteCombo,
        FlameBreath,
        Pounce,
        SwordThrow
    }

    [Header("Debug")]
    [SerializeField] private bool enableDebugLog = true;

    [Header("Cooldowns")]
    [SerializeField] private float biteCooldown = 2f;
    [SerializeField] private float flameCooldown = 4f;
    [SerializeField] private float pounceCooldown = 5f;
    [SerializeField] private float swordCooldown = 6f;

    [Header("Damage")]
    [SerializeField] private float biteDamage = 12f;
    [SerializeField] private float flameDamage = 18f;
    [SerializeField] private float pounceDamage = 20f;
    [SerializeField] private float swordDamage = 16f;

    [Header("Ranges")]
    [SerializeField] private float biteRange = 0.6f;
    [SerializeField] private float flameRange = 1.2f;
    [SerializeField] private float pounceStartRange = 3.5f;
    [SerializeField] private float swordStartRange = 6f;
    [SerializeField] private float pounceImpactRange = 1f;

    [Header("Pounce")]
    [SerializeField] private float pounceDashSpeed = 10f;

    [Header("References")]
    [SerializeField] private CerberusHealth health;
    [SerializeField] private BossMovement movement;
    [SerializeField] private Transform bitePointLeft;
    [SerializeField] private Transform bitePointRight;
    [SerializeField] private Transform bitePointCenter;
    [SerializeField] private Transform flamePoint;
    [SerializeField] private Transform swordSpawnPoint;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private GameObject swordProjectilePrefab;

    private float biteTimer;
    private float flameTimer;
    private float pounceTimer;
    private float swordTimer;

    private Vector3 cachedTargetPosition;

    public float BiteRange => biteRange;
    public float FlameRange => flameRange;
    public float PounceStartRange => pounceStartRange;
    public float SwordStartRange => swordStartRange;

    protected override void Awake()
    {
        base.Awake();

        if (health == null)
            health = GetComponent<CerberusHealth>();

        if (movement == null)
            movement = GetComponent<BossMovement>();
    }

    private void Update()
    {
        biteTimer -= Time.deltaTime;
        flameTimer -= Time.deltaTime;
        pounceTimer -= Time.deltaTime;
        swordTimer -= Time.deltaTime;
    }

    private void Log(string msg)
    {
        if (!enableDebugLog) return;
        Debug.Log($"[CerberusAttack] {msg}");
    }

    public override bool CanChooseNewAttack()
    {
        bool dead = health != null && health.IsDead;
        bool hurt = health != null && health.IsHurt;

        if (dead)
        {
            // Log("CanChooseNewAttack = false (dead)");
            return false;
        }

        if (hurt)
        {
            // Log("CanChooseNewAttack = false (hurt)");
            return false;
        }

        if (isBusy)
        {
            // Log("CanChooseNewAttack = false (isBusy = true)");
            return false;
        }

        // Log("CanChooseNewAttack = true");
        return true;
    }

    public bool TryStartBestAttack(float distance, Vector3 targetPosition, CerberusController.CerberusPhase phase)
{
    if (!CanChooseNewAttack())
        return false;

    List<AttackKind> candidates = new List<AttackKind>();

    float closeRange = Mathf.Max(biteRange, flameRange);

    if (phase == CerberusController.CerberusPhase.Phase1 ||
        phase == CerberusController.CerberusPhase.Phase3)
    {
        if (distance <= closeRange)
        {
            if (biteTimer <= 0f) candidates.Add(AttackKind.BiteCombo);
            if (flameTimer <= 0f) candidates.Add(AttackKind.FlameBreath);
        }
        else if (distance <= 3.5f)
        {
            if (pounceTimer <= 0f) candidates.Add(AttackKind.Pounce);
        }
        else if (distance <= 6f)
        {
            if (swordTimer <= 0f) candidates.Add(AttackKind.SwordThrow);
        }
    }

    if (candidates.Count == 0)
        return false;

    AttackKind chosen = candidates[Random.Range(0, candidates.Count)];

    switch (chosen)
    {
        case AttackKind.BiteCombo:
            StartBiteCombo();
            return true;
        case AttackKind.FlameBreath:
            StartFlameBreath();
            return true;
        case AttackKind.Pounce:
            StartPounce(targetPosition);
            return true;
        case AttackKind.SwordThrow:
            StartSwordThrow(targetPosition);
            return true;
    }

    return false;
}

    public void ApplyPhase3Buff()
    {
        biteCooldown *= 0.8f;
        flameCooldown *= 0.85f;
        pounceCooldown *= 0.8f;
        swordCooldown *= 0.85f;

        // Log($"ApplyPhase3Buff | biteCooldown={biteCooldown:F2}, flameCooldown={flameCooldown:F2}, pounceCooldown={pounceCooldown:F2}, swordCooldown={swordCooldown:F2}");
    }

    public void StartBiteCombo()
    {
        if (!CanChooseNewAttack() || biteTimer > 0f)
        {
            // Log($"StartBiteCombo blocked | canChoose={CanChooseNewAttack()} | biteTimer={biteTimer:F2}");
            return;
        }

        isBusy = true;
        biteTimer = biteCooldown;
        // Log($"StartBiteCombo SUCCESS | set isBusy=true | biteTimer={biteTimer:F2}");

        animator.SetTrigger("BiteCombo");
    }

    public void StartFlameBreath()
    {
        if (!CanChooseNewAttack() || flameTimer > 0f)
        {
            // Log($"StartFlameBreath blocked | canChoose={CanChooseNewAttack()} | flameTimer={flameTimer:F2}");
            return;
        }

        isBusy = true;
        flameTimer = flameCooldown;
        // Log($"StartFlameBreath SUCCESS | set isBusy=true | flameTimer={flameTimer:F2}");

        animator.SetTrigger("FlameBreath");
    }

    public void StartPounce(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || pounceTimer > 0f)
        {
            // Log($"StartPounce blocked | canChoose={CanChooseNewAttack()} | pounceTimer={pounceTimer:F2}");
            return;
        }

        isBusy = true;
        pounceTimer = pounceCooldown;
        cachedTargetPosition = targetPosition;

        // Log($"StartPounce SUCCESS | set isBusy=true | pounceTimer={pounceTimer:F2} | target={targetPosition}");

        animator.SetTrigger("Pounce");
    }

    public void StartSwordThrow(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || swordTimer > 0f)
        {
            Log($"StartSwordThrow blocked | canChoose={CanChooseNewAttack()} | swordTimer={swordTimer:F2}");
            return;
        }

        isBusy = true;
        swordTimer = swordCooldown;
        cachedTargetPosition = targetPosition;

        Log($"StartSwordThrow SUCCESS | set isBusy=true | swordTimer={swordTimer:F2} | target={targetPosition}");

        animator.SetTrigger("SwordThrow");
    }

    public override void ForceStopAllAttacks()
    {
        Log("ForceStopAllAttacks called -> isBusy=false");
        base.ForceStopAllAttacks();
    }

    public override void EndAttack()
    {
        // Log("EndAttack called -> isBusy=false");
        base.EndAttack();
    }

    public void DealBiteLeft()
    {
        // Log("DealBiteLeft event");
        DealSphereDamage(bitePointLeft, biteRange, biteDamage, "BiteLeft");
    }

    public void DealBiteRight()
    {
        // Log("DealBiteRight event");
        DealSphereDamage(bitePointRight, biteRange, biteDamage, "BiteRight");
    }

    public void DealBiteCenter()
    {
        // Log("DealBiteCenter event");
        DealSphereDamage(bitePointCenter, biteRange, biteDamage, "BiteCenter");
    }

    public void DealFlame()
    {
        // Log("DealFlame event");
        DealSphereDamage(flamePoint, flameRange, flameDamage, "Flame");
    }

    public void BeginPounceDash()
    {
        // Log("BeginPounceDash event");

        if (movement == null)
        {
            // Log("BeginPounceDash aborted: movement is null");
            return;
        }

        Vector3 direction = cachedTargetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            // Log($"Dash direction = {direction.normalized}");
            movement.DashTo(direction.normalized, pounceDashSpeed);
        }
        else
        {
            // Log("Dash direction too small");
        }
    }

    public void DealPounceImpact()
    {
        // Log("DealPounceImpact event");
        DealSphereDamage(transform, pounceImpactRange, pounceDamage, "PounceImpact");
    }

    public void SpawnSwordProjectile()
    {
        Log("SpawnSwordProjectile event");

        if (swordProjectilePrefab == null || swordSpawnPoint == null)
        {
            Log("SpawnSwordProjectile aborted: prefab or spawn point is null");
            return;
        }

        GameObject go = Instantiate(swordProjectilePrefab, swordSpawnPoint.position, Quaternion.identity);
        BossSwordProjectile projectile = go.GetComponent<BossSwordProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(cachedTargetPosition, swordDamage, playerLayer);
            Log($"Sword projectile initialized toward {cachedTargetPosition}");
        }
        else
        {
            Log("Spawned projectile has no BossSwordProjectile component");
        }
    }

    private void DealSphereDamage(Transform point, float range, float damage, string source)
    {
        if (point == null)
        {
            // Log($"{source} DealSphereDamage aborted: point is null");
            return;
        }

        Collider[] hits = Physics.OverlapSphere(point.position, range, playerLayer);
        // Log($"{source} damage check | point={point.position} | range={range:F2} | hits={hits.Length}");

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats stats = hits[i].GetComponent<PlayerStats>();
            if (stats == null)
                stats = hits[i].GetComponentInParent<PlayerStats>();

            if (stats != null)
            {
                Log($"{source} hit player -> TakeDamage({damage})");
                stats.TakeDamage(damage);
                break;
            }
        }
    }
}