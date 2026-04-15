using System.Collections;
using UnityEngine;

public class HopliteSpearSplitProjectile : EnemyProjectileBase
{
    private float slowMultiplier;
    private float slowDuration;
    private float straightDuration;
    private float rotateDuration;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Transform playerTransform;

    private enum SplitPhase { Straight, Rotating, Flying }
    private SplitPhase phase = SplitPhase.Straight;
    private float phaseTimer = 0f;
    private Vector3 targetDir;
    private float rotateSpeed;

    public void InitSplit(Vector3 dir, float dmg, HealthBase attackerHealth, Transform player,
        float slowMult, float slowDur, float straightDur, float rotateDur)
    {
        moveDirection = dir;
        damage = dmg;
        attacker = attackerHealth;
        playerTransform = player;
        slowMultiplier = slowMult;
        slowDuration = slowDur;
        straightDuration = straightDur;
        rotateDuration = rotateDur;

        if (attackerHealth != null)
            ignoredColliders = attackerHealth.GetComponentsInChildren<Collider>();
        RotateSprite();
    }

    protected override void Update()
    {
        if (hasHit) return;

        switch (phase)
        {
            case SplitPhase.Straight:
                Move();
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= straightDuration)
                {
                    phaseTimer = 0f;
                    StartRotatePhase();
                }
                break;

            case SplitPhase.Rotating:
                TickRotate();
                Move();
                phaseTimer += Time.deltaTime;
                if (phaseTimer >= rotateDuration)
                {
                    moveDirection = targetDir;
                    phase = SplitPhase.Flying;
                }
                break;

            case SplitPhase.Flying:
                if (Physics.Raycast(transform.position, moveDirection, speed * Time.deltaTime + 0.1f, obstacleLayers))
                {
                    OnHit();
                    return;
                }
                Move();
                break;
        }

        if (traveled >= maxTravelDistance)
            OnHit();
    }

    private void StartRotatePhase()
    {
        if (playerTransform != null)
        {
            Vector3 toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            targetDir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : moveDirection;
        }
        else
        {
            targetDir = moveDirection;
        }

        float angle = Vector3.Angle(moveDirection, targetDir);
        rotateSpeed = angle / Mathf.Max(rotateDuration, 0.01f);
        phase = SplitPhase.Rotating;
    }

    private void TickRotate()
    {
        moveDirection = Vector3.RotateTowards(moveDirection, targetDir, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f);
        transform.rotation = Quaternion.LookRotation(moveDirection);
        RotateSprite();
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
            ApplySlow(hitTarget);
            DealDamageTo(hitTarget);
        }

        Destroy(gameObject);
    }

    private void ApplySlow(Collider col)
    {
        var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
        if (ps != null)
            ps.TakeDebuffMultiplier(new StatModifier { moveSpeed = slowMultiplier }, slowDuration);
    }
}