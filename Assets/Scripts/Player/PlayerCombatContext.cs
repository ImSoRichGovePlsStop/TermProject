using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatContext : MonoBehaviour
{
    public HashSet<EnemyHealth> LastHitEnemies { get; set; } = new HashSet<EnemyHealth>();
    public EnemyHealth LastAttacker { get; set; }
    public List<EnemyHealth> EnemiesAround { get; private set; } = new List<EnemyHealth>();
    public int LastComboIndex { get; private set; } = 0;
    public Vector3 LastSecondaryPosition { get; private set; }
    public float LastAttackDamage { get; private set; }

    public event Action OnAttack;
    public event Action OnCritHit;
    public event Action OnTakeDamage;
    public event Action OnGetHit;
    public event Action OnSecondaryAttack;
    public event Action OnSecondaryAttackForced;
    public event Action<EnemyHealth> OnEnemyKilled;
    public event Action<HealthBase> OnEntityKilled;

    public void NotifyAttack(HashSet<EnemyHealth> hitEnemies, int comboIndex = 0)
    {
        LastHitEnemies = hitEnemies;
        LastComboIndex = comboIndex;
        OnAttack?.Invoke();
    }

    public void NotifyCritHit()
    {
        OnCritHit?.Invoke();
    }

    public void NotifySecondaryAttack(HashSet<EnemyHealth> hitEnemies, Vector3 position)
    {
        LastHitEnemies = hitEnemies;
        LastSecondaryPosition = position;
        OnSecondaryAttack?.Invoke();
    }

    public void NotifySecondaryAttackForced(Vector3 position)
    {
        LastSecondaryPosition = position;
        OnSecondaryAttackForced?.Invoke();
    }

    public void NotifyTakeDamage(EnemyHealth attacker)
    {
        LastAttacker = attacker;
        OnTakeDamage?.Invoke();
    }

    public void NotifyGetHit(EnemyHealth attacker)
    {
        LastAttacker = attacker;
        OnGetHit?.Invoke();
    }

    public void RefreshEnemiesAround(float radius)
    {
        EnemiesAround.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead)
                EnemiesAround.Add(enemy);
        }
    }

    public void NotifyEnemyKilled(EnemyHealth enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
        OnEntityKilled?.Invoke(null);
    }

    public void NotifyEnemyKilled(HealthBase entity)
    {
        OnEntityKilled?.Invoke(entity);
    }
}