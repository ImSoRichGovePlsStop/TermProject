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

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        state.currentThreshold = baseThresholdPerRarity[index] + level * thresholdPerLevel;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (state.buffActive)
        {
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
            state.buffActive = false;
        }
    }

    public override void OnUpdate(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < state.currentThreshold;

        if (belowThreshold && !state.buffActive)
        {
            state.buffActive = true;
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        }
        else if (!belowThreshold && state.buffActive)
        {
            state.buffActive = false;
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent += percent;
        if (state.buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        if (state.buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent -= percent;
        if (state.buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveDamage(state);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        float threshold = (baseThresholdPerRarity[index] + level * thresholdPerLevel) * 100f;

        if (state.totalBuffPercent > 0f)
            return $"When below {threshold:F0}% HP:\n<s>+{baseStat:F0}</s> +{effective:F0} Damage";
        return $"When below {threshold:F0}% HP: +{baseStat:F0} Damage";
    }

    private float GetEffectiveDamage(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}