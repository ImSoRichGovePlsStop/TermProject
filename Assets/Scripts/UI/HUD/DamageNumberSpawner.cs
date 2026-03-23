using System;
using UnityEngine;

public class DamageNumberSpawner : MonoBehaviour
{
    [SerializeField] private DamageNumberUI prefab;

    private float baseRandomRangeX = 0.3f;
    private float baseRandomRangeY = 0.2f;

    private PlayerStats playerStats;
    private Vector3 playerDamageOffset = new Vector3(0f, 0.4f, 0f);

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();

        if (playerStats != null)
            playerStats.OnPlayerDamaged += OnPlayerDamaged;
    }

    private void OnDestroy()
    {
        if (playerStats != null)
            playerStats.OnPlayerDamaged -= OnPlayerDamaged;
    }

    public void RegisterEnemy(EnemyHealth enemy)
    {
        enemy.OnDamageReceived += (damage, isCrit) =>
        {
            float rangeX = baseRandomRangeX * enemy.HealthBarScale.x;
            float rangeY = baseRandomRangeY * enemy.HealthBarScale.y;
            SpawnNumber(enemy.transform.position + enemy.HealthBarOffset, damage, isCrit, false, rangeX, rangeY);
        };
    }

    private void OnPlayerDamaged(float damage)
    {
        if (playerStats == null) return;
        SpawnNumber(playerStats.transform.position + playerDamageOffset, damage, false, true);
    }

    private void SpawnNumber(Vector3 basePos, float damage, bool isCrit, bool isPlayerDamage, float rangeX = -1f, float rangeY = -1f)
    {
        if (prefab == null) return;

        float rx = rangeX < 0f ? baseRandomRangeX : rangeX;
        float ry = rangeY < 0f ? baseRandomRangeY : rangeY;

        Vector3 spawnPos = basePos;
        spawnPos.x += UnityEngine.Random.Range(-rx, rx);
        spawnPos.y += UnityEngine.Random.Range(0f, ry);

        var num = Instantiate(prefab, spawnPos, Quaternion.identity);
        num.Init(damage, isCrit, isPlayerDamage);
    }
}