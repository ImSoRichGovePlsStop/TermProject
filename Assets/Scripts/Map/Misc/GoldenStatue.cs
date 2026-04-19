using UnityEngine;

public class GoldenStatue : MonoBehaviour
{
    [Header("Stat Boost Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float statBoostChance = 0.5f;
    [SerializeField] private float boostPercentMin = 0.01f;
    [SerializeField] private float boostPercentMax = 0.03f;

    private HealthBase health;

    private void Awake()
    {
        health = GetComponent<HealthBase>();
        health.OnDeath += OnDestroyed;
    }


    private void OnDestroyed()
    {
        if (Random.value <= statBoostChance)
        {
            var stats = FindFirstObjectByType<PlayerStats>();
            if (stats != null)
            {
                float boost = Random.Range(boostPercentMin, boostPercentMax);
                ApplyStatBoost(stats, boost);
                return;
            }
        }

        Destroy(gameObject);
    }

    private void ApplyStatBoost(PlayerStats stats, float boost)
    {
        int statIndex = Random.Range(0, 6);
        string statName;
        string displayValue;
        StatModifier bonus = new StatModifier();

        switch (statIndex)
        {
            case 0: bonus.health = stats.BaseHP * boost; statName = "Max HP"; displayValue = $"{bonus.health:F1}"; break;
            case 1: bonus.damage = stats.BaseDMG * boost; statName = "Damage"; displayValue = $"{bonus.damage:F1}"; break;
            case 2: bonus.attackSpeed = stats.BaseATKSPD * boost; statName = "Attack Speed"; displayValue = $"{bonus.attackSpeed:P1}"; break;
            case 3: bonus.moveSpeed = stats.BaseMOVSPD * boost; statName = "Move Speed"; displayValue = $"{bonus.moveSpeed:F1}"; break;
            case 4: bonus.critChance = stats.BaseCrit * boost; statName = "Crit Chance"; displayValue = $"{bonus.critChance:P1}"; break;
            case 5: bonus.critDamage = stats.BaseCritDMG * boost; statName = "Crit Damage"; displayValue = $"{bonus.critDamage:P1}"; break;
            default: statName = "Stat"; displayValue = "0"; break;
        }

        DamageNumberSpawner.Instance?.SpawnMessage(
            transform.position,
            $"+{displayValue} {statName}!",
            new Color(1f, 0.9f, 0.3f));

        stats.AddFlatModifier(bonus);
        Destroy(gameObject);
    }
}