using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(WandProjectile))]
public class WandAoEPulse : MonoBehaviour
{
    [HideInInspector] public float pulseDamageScale;
    [HideInInspector] public float pulseRadius;
    [HideInInspector] public float pulseInterval;
    [HideInInspector] public PlayerStats shooterStats;
    [HideInInspector] public PlayerCombatContext context;

    [Header("Pulse VFX")]
    [SerializeField] private GameObject pulseVFXPrefab;
    [SerializeField] private float vfxDuration = 0.5f;

    private float pulseTimer = 0f;

    void Update()
    {
        pulseTimer += Time.deltaTime;

        if (pulseTimer >= pulseInterval)
        {
            pulseTimer = 0f;
            DoPulse();
        }
    }

    private void DoPulse()
    {
        if (shooterStats == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRadius);
        var result = new HashSet<HealthBase>();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var healthBase = hit.GetComponentInParent<HealthBase>();
            if (healthBase == null || healthBase.IsDead) continue;
            float dmg = shooterStats.CalculateDamage(pulseDamageScale);
            healthBase.TakeDamage(dmg, shooterStats.LastHitWasCrit);
            result.Add(healthBase);
        }

        if (result.Count > 0)
            context?.NotifyAttack(result, 4);

        SpawnPulseVFX();
    }

    private void SpawnPulseVFX()
    {
        if (pulseVFXPrefab == null) return;

        Vector3 spawnPos = new Vector3(transform.position.x, 0.01f, transform.position.z);
        Quaternion prefabRot = pulseVFXPrefab.transform.rotation;
        GameObject vfx = Instantiate(pulseVFXPrefab, spawnPos, prefabRot);

        var ps = vfx.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = vfxDuration;
            main.startLifetime = vfxDuration;
            main.loop = false;
            main.startSize = pulseRadius * 2f;

            ps.Play();
        }

        Destroy(vfx, vfxDuration);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, pulseRadius);
    }
}