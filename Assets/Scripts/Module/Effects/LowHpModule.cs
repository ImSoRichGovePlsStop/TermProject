using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Low HP Damage")]
public class LowHpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    [Header("HP Threshold per Rarity (Common -> Legendary)")]
    public float[] baseThresholdPerRarity = { 0f, 0f, 0f, 0f, 0f };

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (state.buffActive)
        {
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
            state.buffActive = false;
        }
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
            int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            return;
        }
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.buffedLevel -= levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
            int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity) return;
        state.buffRarity = newRarity;

        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);

        if (!state.isActive)
        {
            state.buffRarity = newRarity;
            return;
        }
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
            int index = Mathf.Clamp((int)state.buffRarity, 0, baseThresholdPerRarity.Length - 1);
            state.currentThreshold = baseThresholdPerRarity[index]; // no level scaling
        }
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnUpdate(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < state.currentThreshold;

        if (belowThreshold && !state.buffActive)
        {
            state.buffActive = true;
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
        else if (!belowThreshold && state.buffActive)
        {
            state.buffActive = false;
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        if (state.buffActive)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        if (state.buffActive)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state) => null;

    public override string PassiveDescription => "When you are low on health, you deal increased damage";
    public override PassiveLayout GetPassiveLayout() => PassiveLayout.TwoEqual;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        float baseThreshold = baseThresholdPerRarity[index];
        float effectiveThreshold = state.isActive ? state.currentThreshold : baseThreshold;
        bool thresholdBuffed = state.isActive && effectiveThreshold != baseThreshold;

        float baseDmg = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effectiveDmg = state.isActive ? GetEffectiveStat(state) : baseDmg;
        bool dmgBuffed = state.isActive && effectiveDmg != baseDmg;

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"{effectiveThreshold * 100f:F0}%",
                label         = "Threshold",
                sublabel      = null,
                isBuffed      = thresholdBuffed,
                unbuffedValue = $"{baseThreshold * 100f:F0}%"
            },
            new PassiveEntry
            {
                value         = $"+{effectiveDmg * 100f:F0}%",
                label         = "Damage",
                sublabel      = "Conditional",
                isBuffed      = dmgBuffed,
                unbuffedValue = $"+{baseDmg * 100f:F0}%"
            }
        };
    }
}