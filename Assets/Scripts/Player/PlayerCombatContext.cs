using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatContext : MonoBehaviour
{
    public HashSet<HealthBase> LastHitEnemies { get; set; } = new HashSet<HealthBase>();
    public HealthBase LastAttacker { get; set; }
    public List<HealthBase> EnemiesAround { get; private set; } = new List<HealthBase>();
    public int LastComboIndex { get; private set; } = 0;
    public Vector3 LastSecondaryPosition { get; private set; }
    public float LastAttackDamage { get; private set; }

    public event Action OnAttack;
    public event Action OnCritHit;
    public event Action OnTakeDamage;
    public event Action OnGetHit;
    public event Action OnSecondaryAttack;
    public event Action OnSecondaryAttackForced;
    public event Action<HealthBase> OnEntityKilled;

    public void NotifyAttack(HashSet<HealthBase> hitEnemies, int comboIndex = 0)
    {
        LastHitEnemies = hitEnemies;
        LastComboIndex = comboIndex;
        OnAttack?.Invoke();
    }

    public void NotifyCritHit()
    {
        OnCritHit?.Invoke();
    }

    public void NotifySecondaryAttack(HashSet<HealthBase> hitEnemies, Vector3 position)
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

    public void NotifyTakeDamage(HealthBase attacker)
    {
        LastAttacker = attacker;
        OnTakeDamage?.Invoke();
    }

    public void NotifyGetHit(HealthBase attacker)
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
            var enemy = hit.GetComponent<HealthBase>();
            if (enemy == null) enemy = hit.GetComponentInParent<HealthBase>();
            if (enemy != null && !enemy.IsDead)
                EnemiesAround.Add(enemy);
        }
    }

    public void NotifyEnemyKilled(HealthBase enemy)
    {
        OnEntityKilled?.Invoke(enemy);
    }


}