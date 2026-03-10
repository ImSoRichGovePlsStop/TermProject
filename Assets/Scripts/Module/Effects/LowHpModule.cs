using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Low HP Damage")]
public class LowHpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 10f, 15f, 22f, 32f, 45f };
    public float levelMultiplier = 0.15f;

    [Header("HP Threshold per Rarity (Common -> Legendary)")]
    public float[] baseThresholdPerRarity = { 0.30f, 0.33f, 0.36f, 0.40f, 0.45f };
    public float thresholdPerLevel = 0.01f;

    private float totalBuffPercent = 0f;
    private float currentStat = 0f;
    private float currentThreshold = 0f;
    private bool buffActive = false;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
        currentStat = 0f;
        currentThreshold = 0f;
        buffActive = false;
    }

    private void OnDisable()
    {
        totalBuffPercent = 0f;
        buffActive = false;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level)
    {
        currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        currentThreshold = baseThresholdPerRarity[index] + level * thresholdPerLevel;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        if (buffActive)
        {
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
            buffActive = false;
        }
    }

    public override void OnUpdate(PlayerStats stats, Rarity rarity, int level)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < currentThreshold;

        if (belowThreshold && !buffActive)
        {
            buffActive = true;
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
        else if (!belowThreshold && buffActive)
        {
            buffActive = false;
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        if (buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent += percent;
        if (buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { totalBuffPercent -= percent; return; }
        if (buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent -= percent;
        if (buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    private float GetEffectiveDamage() => currentStat * (1f + totalBuffPercent);
}