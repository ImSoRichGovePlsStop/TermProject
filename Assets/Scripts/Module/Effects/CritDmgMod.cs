using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/CritDamage")]
public class CritDmgMod : ModuleEffect
{
    [Header("Stat per Rarity (Uncommon -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }
    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        state.buffedLevel -= levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity) return;
        state.buffRarity = newRarity;

        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
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
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { critDamage = GetEffectiveStat(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        if (effective != baseStat && state.isActive)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective * 100f:F0}% Critical Damage";
        return $"+{baseStat * 100f:F0}% Critical Damage";
    }

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
        => BuildBaseModuleStat(baseStatPerRarity, levelMultiplier, rarity, level, state);

    public override (string leftLabel, float before, float after, string format) GetTooltipStats(
        Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats)
    {
        float moduleStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        string leftLabel = BuildLeftLabel(moduleStat, effective, state, "Crit Damage", true);

        if (playerStats == null) return (leftLabel, -1f, -1f, "F0%");

        float before, after;
        if (state.isActive)
        {
            before = playerStats.CritDamage - effective;
            after = playerStats.CritDamage;
        }
        else
        {
            before = playerStats.CritDamage;
            after = playerStats.CritDamage + moduleStat;
        }
        return (leftLabel, before * 100f, after * 100f, "F0%");
    }
}