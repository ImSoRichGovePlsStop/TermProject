using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LasherReaverAnchorProjectile : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float duration;
    private float damageScale;
    private float hitRadius;
    private float hitInterval;
    private float spinSpeed;
    private float curveWidth;
    private HealthBase attacker;
    private EntityStats attackerStats;
    private Action onReturned;
    private Transform owner;

    private readonly Dictionary<GameObject, float> lastHitTime = new Dictionary<GameObject, float>();

    public void Initialize(Vector3 start, Vector3 target, float duration,
        float damageScale, float hitRadius, float hitInterval, float spinSpeed, float curveWidth,
        HealthBase attacker, Action onReturned)
    {
        this.startPos = start;
        this.targetPos = target;
        this.duration = duration;
        this.damageScale = damageScale;
        this.hitRadius = hitRadius;
        this.hitInterval = hitInterval;
        this.spinSpeed = spinSpeed;
        this.curveWidth = curveWidth;
        this.attacker = attacker;
        this.attackerStats = attacker?.GetComponent<EntityStats>();
        this.onReturned = onReturned;

        var controller = attacker?.GetComponent<LasherReaverController>();
        if (controller != null) owner = controller.transform;

        StartCoroutine(OvalRoutine());
    }

    private IEnumerator OvalRoutine()
    {
        // Oval center = midpoint between Lasher and target
        Vector3 center = (startPos + targetPos) * 0.5f;
        center.y = startPos.y;

        // Semi-axes
        Vector3 axisA = (targetPos - startPos) * 0.5f; // major axis (toward target)
        axisA.y = 0f;
        Vector3 axisB = new Vector3(-axisA.z, 0f, axisA.x).normalized * axisA.magnitude * curveWidth; // minor axis (perpendicular)

        float elapsed = 0f;
        float totalDuration = duration * 2f; // full oval = go + return
        float currentSpin = 0f;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / totalDuration;
            currentSpin += spinSpeed * 360f * Time.deltaTime;

            // Parametric oval: angle goes from PI to -PI (start at Lasher side)
            float angle = Mathf.PI - t * Mathf.PI * 2f;
            Vector3 pos = center + axisA * Mathf.Cos(angle) + axisB * Mathf.Sin(angle);
            pos.y = startPos.y;
            transform.position = pos;

            // Align with ground (flat) + spin
            transform.rotation = Quaternion.Euler(90f, currentSpin, 0f);

            CheckHits();
            yield return null;
        }

        onReturned?.Invoke();
        Destroy(gameObject);
    }

    private void CheckHits()
    {
        LayerMask hitMask = (1 << LayerMask.NameToLayer("Player"))
                          | (1 << LayerMask.NameToLayer("Summoner"))
                          | (1 << LayerMask.NameToLayer("Totem"));

        float damage = (attackerStats?.Damage ?? 0f) * damageScale;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, hitMask);
        foreach (var col in hits)
        {
            GameObject go = col.gameObject;
            if (lastHitTime.TryGetValue(go, out float last) && Time.time - last < hitInterval)
                continue;

            lastHitTime[go] = Time.time;

            var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, attacker); continue; }

            var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead && hb != attacker) hb.TakeDamage(damage);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}