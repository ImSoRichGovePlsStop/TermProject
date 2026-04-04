using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
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
    [SerializeField] private float biteDamageScale = 1f;
    [SerializeField] private float flameDamageScale = 1.5f;
    [SerializeField] private float pounceDamageScale = 1.8f;
    [SerializeField] private float swordDamageScale = 1.3f;
    private EntityStats entityStats;

    [Header("Attack Selection Ranges")]
    [SerializeField] private float biteRange = 1.5f;
    [SerializeField] private float flameRange = 3f;
    [SerializeField] private float pounceStartRange = 5f;
    [SerializeField] private float swordStartRange = 8f;

    [Header("Bite Combo")]
    [SerializeField] private float biteHitRadius = 1.8f;

    [Header("Flame Breath Cone")]
    [SerializeField] private float flameConeRadius = 4f;
    [SerializeField] private float flameConeAngle = 70f;
    [SerializeField] private Transform flameOrigin;
    [SerializeField] private GameObject flameConeIndicatorPrefab;

    [Header("Pounce")]
    [SerializeField] private float pounceDashSpeed = 10f;
    [SerializeField] private float pounceDashDistance = 3f;
    [SerializeField] private float pounceImpactRadius = 1.6f;
    [SerializeField] private Transform pounceHitPoint;

    [Header("Sword Throw")]
    [SerializeField] private Transform swordSpawnPoint;
    [SerializeField] private GameObject swordProjectilePrefab;

    [Header("References")]
    [SerializeField] private CerberusHealth health;
    [SerializeField] private BossMovement movement;
    [SerializeField] private LayerMask playerLayer;

    private Rigidbody rb;

    private float biteTimer;
    private float flameTimer;
    private float pounceTimer;
    private float swordTimer;

    private Vector3 cachedTargetPosition;
    private Vector3 cachedPounceDirection;
    private Vector3 cachedFlameDirection;
    private Vector3 cachedSwordDirection;

    private bool pounceHasDealtDamage;
    private Coroutine pounceRoutine;
    private GameObject activeFlameIndicator;

    public float BiteRange => biteRange;
    public float FlameRange => flameRange;
    public float PounceStartRange => pounceStartRange;
    public float SwordStartRange => swordStartRange;

    protected override void Awake()
    {
        base.Awake();

        rb = GetComponent<Rigidbody>();

        if (health == null)
            health = GetComponent<CerberusHealth>();

        if (movement == null)
            movement = GetComponent<BossMovement>();

        entityStats = GetComponent<EntityStats>();
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

        if (dead) return false;
        if (hurt) return false;
        if (isBusy) return false;

        return true;
    }

    public bool TryStartBestAttack(float distance, Vector3 targetPosition, CerberusController.CerberusPhase phase)
    {
        if (!CanChooseNewAttack())
            return false;

        List<AttackKind> candidates = new List<AttackKind>();

        if (phase == CerberusController.CerberusPhase.Phase1 ||
            phase == CerberusController.CerberusPhase.Phase3)
        {
            if (distance <= biteRange && biteTimer <= 0f)
                candidates.Add(AttackKind.BiteCombo);

            if (distance > biteRange && distance <= flameRange && flameTimer <= 0f)
                candidates.Add(AttackKind.FlameBreath);

            if (distance > flameRange && distance <= pounceStartRange && pounceTimer <= 0f)
                candidates.Add(AttackKind.Pounce);

            if (distance > pounceStartRange && distance <= swordStartRange && swordTimer <= 0f)
                candidates.Add(AttackKind.SwordThrow);
        }

        if (candidates.Count == 0)
            return false;

        AttackKind chosen = candidates[Random.Range(0, candidates.Count)];

        switch (chosen)
        {
            case AttackKind.BiteCombo:
                StartBiteCombo(targetPosition);
                return true;

            case AttackKind.FlameBreath:
                StartFlameBreath(targetPosition);
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
    }

    public void StartBiteCombo(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || biteTimer > 0f)
            return;

        FaceTowardTarget(targetPosition);
        isBusy = true;
        biteTimer = biteCooldown;
        animator.SetTrigger("BiteCombo");
    }

    public void StartFlameBreath(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || flameTimer > 0f)
            return;

        cachedTargetPosition = targetPosition;
        cachedFlameDirection = GetFlatDirectionTo(targetPosition);
        if (cachedFlameDirection.sqrMagnitude <= 0.001f)
            cachedFlameDirection = transform.forward;

        FaceTowardDirection(cachedFlameDirection);

        isBusy = true;
        flameTimer = flameCooldown;
        animator.SetTrigger("FlameBreath");
    }

    public void StartPounce(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || pounceTimer > 0f)
            return;

        cachedTargetPosition = targetPosition;

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
            cachedPounceDirection = dir.normalized;
        else
            cachedPounceDirection = transform.forward;

        FaceTowardDirection(cachedPounceDirection);

        isBusy = true;
        pounceTimer = pounceCooldown;
        pounceHasDealtDamage = false;
        animator.SetTrigger("Pounce");

        Log($"StartPounce -> dir={cachedPounceDirection}, target={targetPosition}");
    }

    public void StartSwordThrow(Vector3 targetPosition)
    {
        if (!CanChooseNewAttack() || swordTimer > 0f)
            return;

        cachedTargetPosition = targetPosition;
        cachedSwordDirection = GetFlatDirectionTo(targetPosition);
        if (cachedSwordDirection.sqrMagnitude <= 0.001f)
            cachedSwordDirection = transform.forward;

        FaceTowardDirection(cachedSwordDirection);

        isBusy = true;
        swordTimer = swordCooldown;
        animator.SetTrigger("SwordThrow");
    }

    public override void ForceStopAllAttacks()
    {
        StopPounceRoutine();
        pounceHasDealtDamage = false;
        DestroyFlameIndicator();
        base.ForceStopAllAttacks();
    }

    public override void EndAttack()
    {
        StopPounceRoutine();
        pounceHasDealtDamage = false;
        DestroyFlameIndicator();
        base.EndAttack();
    }

    public void DealBiteHit1()
    {
        DealSphereDamage(transform.position, biteHitRadius, GetDamage(biteDamageScale), "BiteHit1");
    }

    public void DealBiteHit2()
    {
        DealSphereDamage(transform.position, biteHitRadius, GetDamage(biteDamageScale), "BiteHit2");
    }

    public void DealBiteHit3()
    {
        DealSphereDamage(transform.position, biteHitRadius, GetDamage(biteDamageScale), "BiteHit3");
    }

    public void ShowFlameCone()
    {
        DestroyFlameIndicator();

        if (flameConeIndicatorPrefab == null)
            return;

        Transform origin = flameOrigin != null ? flameOrigin : transform;
        Quaternion rot = Quaternion.LookRotation(
            cachedFlameDirection.sqrMagnitude > 0.001f ? cachedFlameDirection : transform.forward,
            Vector3.up
        );

        activeFlameIndicator = Instantiate(flameConeIndicatorPrefab, origin.position, rot);

        Vector3 baseScale = activeFlameIndicator.transform.localScale;

        // สมมติ prefab cone เดิมกว้าง 90 องศา
        // ใช้ tan(halfAngle) บีบ/ขยายความกว้างตามมุมที่ต้องการ
        float widthFactor = Mathf.Tan(Mathf.Deg2Rad * (flameConeAngle * 0.5f));
        widthFactor = Mathf.Max(0.05f, widthFactor);

        // สมมติแกน Z คือความยาว, แกน X คือความกว้าง
        activeFlameIndicator.transform.localScale = new Vector3(
            flameConeRadius * 2f * widthFactor,
            baseScale.y,
            flameConeRadius * 2f
        );
    }

    public void HideFlameCone()
    {
        DestroyFlameIndicator();
    }

    public void DealFlameCone()
    {
        DestroyFlameIndicator();

        Transform origin = flameOrigin != null ? flameOrigin : transform;
        Vector3 attackOrigin = origin.position;
        Vector3 forward = cachedFlameDirection.sqrMagnitude > 0.001f
            ? cachedFlameDirection.normalized
            : transform.forward;

        Collider[] hits = Physics.OverlapSphere(attackOrigin, flameConeRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Transform target = hits[i].transform;
            Vector3 toTarget = target.position - attackOrigin;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= 0.001f)
                continue;

            float angle = Vector3.Angle(forward, toTarget.normalized);
            if (angle <= flameConeAngle * 0.5f)
            {
                PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();
                if (playerStats == null)
                    playerStats = hits[i].GetComponentInParent<PlayerStats>();

                if (playerStats != null)
                {
                    playerStats.TakeDamage(GetDamage(flameDamageScale), health);
                    Log($"FlameCone hit player for {GetDamage(flameDamageScale)}");
                }
            }
        }
    }

    public void BeginPounceDash()
    {
        StopPounceRoutine();

        if (cachedPounceDirection.sqrMagnitude <= 0.001f && cachedTargetPosition != Vector3.zero)
        {
            Vector3 dir = cachedTargetPosition - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                cachedPounceDirection = dir.normalized;
        }

        pounceRoutine = StartCoroutine(PounceDashRoutine());
    }

    private IEnumerator PounceDashRoutine()
    {
        Vector3 direction = cachedPounceDirection;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            Log("Pounce cancelled: direction is zero");
            yield break;
        }

        direction.Normalize();

        if (movement != null)
        {
            movement.SetCanMove(false);
            movement.StopMoving();
            movement.FaceTarget(transform.position + direction);
        }

        float moved = 0f;

        Log($"BeginPounceDash -> dir={direction}, distance={pounceDashDistance}, speed={pounceDashSpeed}");

        while (moved < pounceDashDistance)
        {
            float step = pounceDashSpeed * Time.fixedDeltaTime;
            if (moved + step > pounceDashDistance)
                step = pounceDashDistance - moved;

            transform.position += direction * step;
            moved += step;

            yield return new WaitForFixedUpdate();
        }

        if (movement != null)
            movement.SetCanMove(true);

        pounceRoutine = null;
        Log("Pounce finished");
    }

    private void StopPounceRoutine()
    {
        if (pounceRoutine != null)
        {
            StopCoroutine(pounceRoutine);
            pounceRoutine = null;
        }

        if (movement != null)
        {
            movement.StopMoving();
            movement.SetCanMove(true);
        }
    }

    public void DealPounceImpact()
    {
        if (pounceHasDealtDamage)
            return;

        Transform hitPoint = pounceHitPoint != null ? pounceHitPoint : transform;
        DealSphereDamage(hitPoint.position, pounceImpactRadius, GetDamage(pounceDamageScale), "PounceImpact");
        pounceHasDealtDamage = true;
    }

    public void SpawnSwordProjectile()
    {
        if (swordProjectilePrefab == null)
            return;

        Transform spawn = swordSpawnPoint != null ? swordSpawnPoint : transform;

        GameObject go = Instantiate(swordProjectilePrefab, spawn.position, Quaternion.identity);
        BossSwordProjectile projectile = go.GetComponent<BossSwordProjectile>();

        if (projectile != null)
        {
            projectile.Initialize(
                transform,
                cachedTargetPosition,
                GetDamage(swordDamageScale),
                playerLayer
            );
        }
    }

    private Vector3 GetFlatDirectionTo(Vector3 targetPosition)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.zero;
    }

    private void FaceTowardTarget(Vector3 targetPosition)
    {
        Vector3 dir = GetFlatDirectionTo(targetPosition);
        if (dir.sqrMagnitude > 0.001f)
            FaceTowardDirection(dir);
    }

    private void FaceTowardDirection(Vector3 direction)
    {
        if (movement != null)
            movement.FaceTarget(transform.position + direction);
    }

    private void DealSphereDamage(Vector3 center, float radius, float damage, string source)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats playerStats = hits[i].GetComponent<PlayerStats>();
            if (playerStats == null)
                playerStats = hits[i].GetComponentInParent<PlayerStats>();

            if (playerStats != null)
            {
                playerStats.TakeDamage(damage, health);
                Log($"{source} hit player for {damage}");
            }
        }
    }

    private void DestroyFlameIndicator()
    {
        if (activeFlameIndicator != null)
        {
            Destroy(activeFlameIndicator);
            activeFlameIndicator = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, biteHitRadius);

        Gizmos.color = Color.blue;
        Transform pounceCenter = pounceHitPoint != null ? pounceHitPoint : transform;
        Gizmos.DrawWireSphere(pounceCenter.position, pounceImpactRadius);

        Gizmos.color = Color.green;
        Transform swordCenter = swordSpawnPoint != null ? swordSpawnPoint : transform;
        Gizmos.DrawWireSphere(swordCenter.position, 1.0f);

        Gizmos.color = Color.yellow;
        Transform flameCenter = flameOrigin != null ? flameOrigin : transform;
        Gizmos.DrawWireSphere(flameCenter.position, flameConeRadius);
    }

    private float GetDamage(float scale) => (entityStats != null ? entityStats.Damage : 10f) * scale;
}