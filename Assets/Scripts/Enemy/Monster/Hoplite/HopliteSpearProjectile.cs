using System.Collections;
using UnityEngine;

public class HopliteSpearProjectile : EnemyProjectileBase
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Slow")]
    [SerializeField] private float slowMultiplier = -0.75f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Split")]
    [SerializeField] private GameObject splitSpearPrefab;
    [SerializeField] private float splitAngle = 35f;
    [SerializeField] private float splitDamageScale = 0.6f;
    [SerializeField] private float splitSlowMultiplier = -0.4f;
    [SerializeField] private float splitSlowDuration = 1.5f;
    [SerializeField] private float splitStraightDuration = 0.5f;
    [SerializeField] private float splitRotateDuration = 0.2f;

    private Transform playerTransform;
    private Vector3 reflectedDir;

    public void InitSpear(Vector3 targetPosition, float dmg, HealthBase attackerHealth, Transform player)
    {
        base.Initialize(targetPosition, dmg, attackerHealth);
        playerTransform = player;
        RotateSprite();
    }

    protected override void Update()
    {
        if (hasHit) return;
        if (Physics.Raycast(transform.position, moveDirection, out RaycastHit hit,
            speed * Time.deltaTime + 0.1f, obstacleLayers))
        {
            reflectedDir = Vector3.Reflect(moveDirection, hit.normal);
            reflectedDir.y = 0f;
            if (reflectedDir.sqrMagnitude > 0.001f) reflectedDir.Normalize();
            OnHit();
            return;
        }
        Move();
        if (traveled >= maxTravelDistance) OnHit();
    }

    private void RotateSprite()
    {
        if (spriteRenderer == null) return;
        float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
        spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angle + 270f, 0f);
    }

    protected override void OnHit(Collider hitTarget = null)
    {
        if (hasHit) return;
        hasHit = true;

        if (hitTarget != null)
        {
            ApplySlow(hitTarget, slowMultiplier, slowDuration);
            DealDamageTo(hitTarget);
            Destroy(gameObject);
            return;
        }

        SpawnSplitSpears();
        Destroy(gameObject);
    }

    private void SpawnSplitSpears()
    {
        if (splitSpearPrefab == null) return;

        float[] angles = { 0f, splitAngle, -splitAngle };
        foreach (float angle in angles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * reflectedDir;
            GameObject go = Instantiate(splitSpearPrefab, transform.position, Quaternion.LookRotation(dir));
            var split = go.GetComponent<HopliteSpearSplitProjectile>();
            split?.InitSplit(dir, damage * splitDamageScale, attacker, playerTransform,
                splitSlowMultiplier, splitSlowDuration, splitStraightDuration, splitRotateDuration);
        }
    }

    private void ApplySlow(Collider col, float multiplier, float duration)
    {
        var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
        if (ps != null)
            ps.TakeDebuffMultiplier(new StatModifier { moveSpeed = multiplier }, duration);
    }
}