using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Low HP Damage")]
public class LowHpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    [Header("HP Threshold per Rarity (Common -> Legendary)")]
    public float[] baseThresholdPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float thresholdPerLevel;


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
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
            state.buffActive = false;
        }
    }

    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.buffedLevel += levelBonus;
        state.currentStat += levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        state.currentThreshold += levelBonus * thresholdPerLevel;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        Debug.Log($"New threshold: {state.currentThreshold}");
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            state.currentStat -= levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
            state.currentThreshold -= levelBonus * thresholdPerLevel;
            return;
        }
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.buffedLevel -= levelBonus;
        state.currentStat -= levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        state.currentThreshold -= levelBonus * thresholdPerLevel;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnUpdate(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < state.currentThreshold;

        if (belowThreshold && !state.buffActive)
        {
            state.buffActive = true;
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        }
        else if (!belowThreshold && state.buffActive)
        {
            state.buffActive = false;
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent += percent;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent -= percent;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveDamage(state);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        float baseThreshold = (baseThresholdPerRarity[index] + level * thresholdPerLevel) * 100f;
        float effectiveThreshold = state.currentThreshold * 100f;

        if (effective != baseStat && state.isActive)
            return $"When below <s>{baseThreshold:F0}%</s> {effectiveThreshold:F0}% HP:\n<s>+{baseStat * 100f:F0}</s> +{effective * 100f:F0}% Damage";
        return $"When below {baseThreshold:F0}% HP: +{baseStat * 100f:F0}% Damage";
    }

    private float GetEffectiveDamage(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}