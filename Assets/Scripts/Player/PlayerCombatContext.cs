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

    [SerializeField] private float aroundRadius = 5f;

    public event Action OnAttack;
    public event Action OnTakeDamage;
    public event Action OnSecondaryAttack;

    public void NotifyAttack(HashSet<EnemyHealth> hitEnemies, int comboIndex = 0)
    {
        LastHitEnemies = hitEnemies;
        LastComboIndex = comboIndex;
        OnAttack?.Invoke();
    }

    public void NotifySecondaryAttack(Vector3 position)
    {
        LastSecondaryPosition = position;
        OnSecondaryAttack?.Invoke();
    }

    public void NotifyTakeDamage(EnemyHealth attacker)
    {
        LastAttacker = attacker;
        OnTakeDamage?.Invoke();
    }

    public void RefreshEnemiesAround()
    {
        EnemiesAround.Clear();
        Collider[] hits = Physics.OverlapSphere(transform.position, aroundRadius);
        foreach (var hit in hits)
        {
            var enemy = hit.GetComponent<EnemyHealth>();
            if (enemy != null && !enemy.IsDead)
                EnemiesAround.Add(enemy);
        }
    }
}