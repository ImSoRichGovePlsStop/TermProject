using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterFieldZone : MonoBehaviour
{
    public float damagePercent = 0.15f;
    public float tickInterval = 0.5f;
    public float duration = 3f;
    public float radius = 1f;
    public bool fieldCountsAsAttack = false;

    [HideInInspector] public ShatterFieldPassive passive;

    private PlayerStats stats;
    private PlayerCombatContext context;

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        stats = playerStats;
        context = combatContext;
        transform.localScale = new Vector3(radius * 2f, transform.localScale.y, radius * 2f);
        StartCoroutine(FieldRoutine());
    }

    private IEnumerator FieldRoutine()
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;
            TickDamage();
        }
        Destroy(gameObject);
    }

    private void TickDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        var hitEnemies = new HashSet<EnemyHealth>();

        foreach (var col in hits)
        {
            if (!col.CompareTag("Enemy")) continue;
            var enemy = col.GetComponentInParent<EnemyHealth>();
            if (enemy == null || enemy.IsDead) continue;

            float dmg = fieldCountsAsAttack
                ? stats.CalculateDamage(damagePercent)
                : stats.Damage * damagePercent;
            enemy.TakeDamage(dmg);
            hitEnemies.Add(enemy);

            passive?.ApplySlow(enemy);
        }

        if (fieldCountsAsAttack && hitEnemies.Count > 0 && context != null)
            context.NotifyAttack(hitEnemies, -1);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawSphere(transform.position, radius);
    }
}