using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
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
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffedLevel -= levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity | oldRarity > newRarity) return;
        state.buffRarity = newRarity;

        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
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
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        if (effective != baseStat && state.isActive)
            return $"<s>+{baseStat:F0}</s> +{effective:F0} HP";
        return $"+{baseStat:F0} HP";
    }

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
        => BuildBaseModuleStat(baseStatPerRarity, levelMultiplier, rarity, level, state);

    public override (string leftLabel, float before, float after, string format) GetStatPreview(
        Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats)
    {
        float moduleStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        string leftLabel = BuildLeftLabel(moduleStat, effective, state, "Max Health", false);

        if (playerStats == null) return (leftLabel, -1f, -1f, "F0");

        float baseHp = playerStats.BaseHealth;
        float multHp = playerStats.MultiplierModifier.health;
        float before, after;
        if (state.isActive)
        {
            before = (baseHp - effective) * (1f + multHp);
            after = playerStats.MaxHealth;
        }
        else
        {
            before = playerStats.MaxHealth;
            after = (baseHp + moduleStat) * (1f + multHp);
        }
        return (leftLabel, before, after, "F0");
    }

}