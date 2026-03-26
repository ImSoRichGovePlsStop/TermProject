using UnityEngine;
using System.Collections.Generic;

// Attached to the hit 5 big projectile prefab alongside WandProjectile.
// Pulses AoE damage around itself at a fixed rate while the projectile is alive.
// The same enemy can be hit by multiple pulses (by design).
[RequireComponent(typeof(WandProjectile))]
public class WandAoEPulse : MonoBehaviour
{
    // Set by WandAttack after spawn
    [HideInInspector] public float pulseDamageScale;   // damage per tick as a fraction of player damage
    [HideInInspector] public float pulseRadius;
    [HideInInspector] public float pulseRate;           // pulses per second
    [HideInInspector] public PlayerStats shooterStats;
    [HideInInspector] public PlayerCombatContext context;

    private float pulseTimer = 0f;

    void Update()
    {
        pulseTimer += Time.deltaTime;
        if (pulseTimer >= 1f / pulseRate)
        {
            pulseTimer = 0f;
            DoPulse();
        }
    }

    private void DoPulse()
    {
        if (shooterStats == null) return;

        Collider[] hits = Physics.OverlapSphere(transform.position, pulseRadius);
        var result = new HashSet<EnemyHealth>();

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;
            var enemyHealth = hit.GetComponentInParent<EnemyHealth>();
            if (enemyHealth == null) continue;

            // damage = scale * playerDamage / pulseRate  (converts per-second value to per-tick)
            float dmg = shooterStats.CalculateDamage(pulseDamageScale) / pulseRate;

            enemyHealth.TakeDamage(dmg, shooterStats.LastHitWasCrit);
            result.Add(enemyHealth);
        }

        if (result.Count > 0)
            context?.NotifyAttack(result, 4); // comboIndex 4 = hit 5
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, pulseRadius);
    }
}