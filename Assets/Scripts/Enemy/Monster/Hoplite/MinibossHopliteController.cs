using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinibossHopliteController : HopliteController
{
    [Header("Guard")]
    [SerializeField] private float guardDuration = 1.5f;
    [SerializeField] private float guardCooldown = 5f;

    [Header("Charge")]
    [SerializeField] private float chargeRange = 4f;
    [SerializeField] private float chargeDamageScale = 1.5f;
    [SerializeField] private float chargeCooldown = 6f;
    [SerializeField] private float chargeWarningDurationMin = 0.8f;
    [SerializeField] private float chargeWarningDurationMax = 1.5f;
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeDuration = 0.4f;
    [SerializeField] private float chargeHitRadius = 0.8f;
    [SerializeField] private float chargeRotateSpeed = 90f;
    [SerializeField] private GameObject chargeWarningPrefab;

    [Header("Guard Dome")]
    [SerializeField] private GuardDomeVFX guardDome;

    private float guardTimer = 0f;
    private GameObject activeWarning;
    private float lastGuardTime = -Mathf.Infinity;
    private float lastChargeTime;
    private bool isCharging = false;

    public bool IsGuarding { get; private set; }
    public bool IsCharging => isCharging;

    public override bool CanBeInterrupted() => false;

    protected override void Awake()
    {
        base.Awake();
        lastChargeTime = Time.time;
    }

    public bool TryProcGuard()
    {
        if (IsGuarding) return false;
        if (Time.time < lastGuardTime + guardCooldown) return false;

        IsGuarding = true;
        guardTimer = guardDuration;
        lastGuardTime = Time.time;
        movement.StopMoving();
        movement.SetCanMove(false);
        animator?.SetBool("IsGuarding", true);
        guardDome?.Show();
        return true;
    }

    protected override void UpdateState()
    {
        if (IsGuarding || isCharging) return;
        base.UpdateState();
    }

    protected override void TickState()
    {
        if (IsGuarding)
        {
            movement.StopMoving();
            guardTimer -= Time.deltaTime;
            if (guardTimer <= 0f) ExitGuard();
            return;
        }

        if (!isCharging && HasTarget && (currentState == HopliteState.Chase || currentState == HopliteState.Attack))
        {
            float dist = Vector3.Distance(transform.position, TargetPosition);
            if (dist <= chargeRange && Time.time >= lastChargeTime + chargeCooldown)
            {
                StartCoroutine(ChargeRoutine());
                return;
            }
        }

        base.TickState();
    }

    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        movement.StopMoving();
        movement.SetCanMove(false);
        animator?.SetTrigger("ChargeWarning");

        yield return null;
        while (animator != null && !animator.GetCurrentAnimatorStateInfo(0).IsName("ChargeWarning"))
            yield return null;

        float warningDuration = Random.Range(chargeWarningDurationMin, chargeWarningDurationMax);

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / warningDuration;
        }

        Vector3 chargeDir = GetFlatDirToTarget();
        activeWarning = SpawnWarning(chargeDir);
        GameObject warning = activeWarning;

        float elapsed = 0f;

        while (elapsed < warningDuration)
        {
            Vector3 toTarget = GetFlatDirToTarget();
            float maxDeg = chargeRotateSpeed * Time.deltaTime;
            chargeDir = Vector3.RotateTowards(chargeDir, toTarget, maxDeg * Mathf.Deg2Rad, 0f);

            movement.FaceTarget(transform.position + chargeDir);

            if (warning != null)
            {
                Vector3 warningPos = transform.position + chargeDir * (chargeRange * 0.5f);
                warningPos.y = warning.transform.position.y;
                warning.transform.position = warningPos;
                warning.transform.rotation = Quaternion.LookRotation(chargeDir, Vector3.up);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (warning != null) Destroy(warning);
        activeWarning = null;

        if (animator != null) animator.speed = 1f;

        var agent = movement.GetAgent();

        animator?.SetTrigger("Charge");

        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        var alreadyHit = new HashSet<GameObject>();
        elapsed = 0f;

        while (elapsed < chargeDuration)
        {
            if (Physics.SphereCast(transform.position, chargeHitRadius, chargeDir, out RaycastHit _, chargeSpeed * stats.MoveSpeedRatio * Time.deltaTime, wallMask))
                break;

            Vector3 nextPos = transform.position + chargeDir * chargeSpeed * stats.MoveSpeedRatio * Time.deltaTime;
            if (agent != null)
                agent.Warp(nextPos);
            else
                transform.position = nextPos;

            elapsed += Time.deltaTime;

            Collider[] hits = Physics.OverlapSphere(transform.position, chargeHitRadius, hitMask);
            foreach (var col in hits)
            {
                if (alreadyHit.Contains(col.gameObject)) continue;
                alreadyHit.Add(col.gameObject);

                var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (ps != null && !ps.IsDead) { ps.TakeDamage(stats.Damage * chargeDamageScale, health); continue; }

                var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
                if (hb != null && !hb.IsDead) hb.TakeDamage(stats.Damage * chargeDamageScale);
            }

            yield return null;
        }

        movement.SetCanMove(true);
        lastChargeTime = Time.time;
        lastAttackTime = Time.time;
        currentAttackCooldown = Mathf.Max(2f, currentAttackCooldown);
        isCharging = false;
        TriggerPostAttackDelay();
    }

    private Vector3 GetFlatDirToTarget()
    {
        Vector3 dir = TargetPosition - transform.position;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
    }

    private GameObject SpawnWarning(Vector3 chargeDir)
    {
        if (chargeWarningPrefab == null) return null;

        Vector3 pos = transform.position + chargeDir * (chargeRange * 0.5f);
        pos.y = transform.position.y;
        Quaternion rot = Quaternion.LookRotation(chargeDir, Vector3.up);
        GameObject warning = Instantiate(chargeWarningPrefab, pos, rot);

        var vfx = warning.GetComponent<ChargeWarningVFX>();
        if (vfx != null)
            vfx.SetDynamicScale(chargeHitRadius * 2f, () => chargeSpeed * stats.MoveSpeedRatio * chargeDuration);
        else
        {
            float chargeDistance = chargeSpeed * stats.MoveSpeedRatio * chargeDuration;
            warning.transform.localScale = new Vector3(chargeHitRadius * 2f, 1f, chargeDistance);
        }
        return warning;
    }

    private void ExitGuard()
    {
        IsGuarding = false;
        movement.SetCanMove(true);
        animator?.SetBool("IsGuarding", false);
        guardDome?.Hide();
    }

    public override void OnDeath()
    {
        if (activeWarning != null)
        {
            Destroy(activeWarning);
            activeWarning = null;
        }
        guardDome?.Hide();
        base.OnDeath();
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chargeRange);
    }
}