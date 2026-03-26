using UnityEngine;
using System.Collections.Generic;

public class AttackHitbox : MonoBehaviour
{
    private ComboHit currentHit;
    private PlayerStats stats;
    private PlayerCombatContext context;
    private int currentComboIndex = 0;
    private bool isSecondary = false;

    private void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
        context = GetComponentInParent<PlayerCombatContext>();
    }

    public void SetComboHit(ComboHit hit, int comboIndex = 0, bool secondary = false)
    {
        currentHit = hit;
        currentComboIndex = comboIndex;
        isSecondary = secondary;
    }

    public ComboHit GetCurrentHit() => currentHit;
    public int GetCurrentComboIndex() => currentComboIndex;

    public void Attack()
    {
        if (currentHit == null) return;

        var result = new HashSet<EnemyHealth>();
        var hitEnemies = new HashSet<Collider>();

        Collider[] sphereHits = Physics.OverlapSphere(transform.position, currentHit.range);
        foreach (Collider hit in sphereHits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            Vector3 dir = (hit.transform.position - transform.position);
            dir.y = 0;
            float angleTo = Vector3.Angle(transform.forward, dir);
            if (angleTo <= currentHit.angle / 2f)
                hitEnemies.Add(hit);
        }

        if (currentHit.extraRange > 0)
        {
            float width = 2 * currentHit.range * Mathf.Sin(currentHit.angle * 0.5f * Mathf.Deg2Rad);
            float angleOffset = Mathf.Cos(currentHit.angle * 0.5f * Mathf.Deg2Rad) * currentHit.range;
            Vector3 center = transform.position + transform.forward * (angleOffset + currentHit.extraRange / 2f);
            Collider[] boxHits = Physics.OverlapBox(
                center,
                new Vector3(width / 2f, 0.05f, currentHit.extraRange / 2f),
                transform.rotation
            );
            foreach (Collider hit in boxHits)
                if (hit.CompareTag("Enemy"))
                    hitEnemies.Add(hit);
        }

        foreach (Collider hit in hitEnemies)
        {
            float dmg = stats.CalculateDamage(currentHit.damageScale);
            Debug.Log($"Hit {hit.name} for {dmg}!");
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(dmg, stats.LastHitWasCrit);
                result.Add(enemyHealth);
            }
        }

        if (context != null)
        {
            if (isSecondary)
                context.NotifySecondaryAttack(transform.position);
            else
                context.NotifyAttack(result, currentComboIndex);
        }
    }

    public void PlayHitVFX()
    {
        if (currentHit?.hitVFXPrefab == null) return;

        Vector3 spawnPos = transform.position + transform.forward * currentHit.vfxOffset;
        Quaternion prefabRot = currentHit.hitVFXPrefab.transform.rotation;
        Quaternion spawnRot = Quaternion.Euler(
            prefabRot.eulerAngles.x,
            transform.rotation.eulerAngles.y + prefabRot.eulerAngles.y,
            prefabRot.eulerAngles.z
        );
        GameObject vfx = Instantiate(currentHit.hitVFXPrefab, spawnPos, spawnRot);

        float totalDuration = (currentHit.animationDuration / stats.AttackSpeed) * currentHit.vfxDurationMultiplier;
        float totalLoops = Mathf.Max(0.01f, currentHit.vfxLoops * currentHit.vfxDurationMultiplier);
        float singleLoopDuration = totalDuration / totalLoops;

        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = singleLoopDuration;
            main.startLifetime = singleLoopDuration;
            main.loop = totalLoops > 1;

            ps.Play();
        }

        Destroy(vfx, totalDuration);
    }

    void OnDrawGizmosSelected()
    {
        if (currentHit == null) return;

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        int segments = 20;
        float halfAngle = currentHit.angle / 2f;
        float height = 0.1f;
        Vector3 origin = transform.position;

        for (int h = 0; h <= 1; h++)
        {
            float yOffset = h == 0 ? -height / 2f : height / 2f;
            Vector3 prevPoint = origin + new Vector3(0, yOffset, 0) +
                                Quaternion.Euler(0, -halfAngle, 0) * transform.forward * currentHit.range;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
                Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
                Vector3 point = origin + new Vector3(0, yOffset, 0) + dir * currentHit.range;
                Gizmos.DrawLine(prevPoint, point);
                if (i == 0 || i == segments)
                    Gizmos.DrawLine(origin + new Vector3(0, yOffset, 0), point);
                prevPoint = point;
            }
        }

        Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * transform.forward;
        Gizmos.DrawLine(origin + new Vector3(0, -height / 2f, 0) + leftDir * currentHit.range,
                        origin + new Vector3(0, height / 2f, 0) + leftDir * currentHit.range);
        Gizmos.DrawLine(origin + new Vector3(0, -height / 2f, 0) + rightDir * currentHit.range,
                        origin + new Vector3(0, height / 2f, 0) + rightDir * currentHit.range);

        if (currentHit.extraRange > 0)
        {
            float width = 2 * currentHit.range * Mathf.Sin(currentHit.angle * 0.5f * Mathf.Deg2Rad);
            float angleOffset = Mathf.Cos(currentHit.angle * 0.5f * Mathf.Deg2Rad) * currentHit.range;
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Vector3 center = transform.position + transform.forward * (angleOffset + currentHit.extraRange / 2f);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, 0.1f, currentHit.extraRange));
        }
    }
}