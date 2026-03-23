using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;
    public Color moduleColor = Color.green;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffedLevel = levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
        }
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel = levelBonus;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffedLevel = levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
        }
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffReceived(int level, Rarity NewRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffRarity = NewRarity;
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
    public override void OnRarityBuffRemoved(int level, Rarity NewRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffRarity = NewRarity;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffRarity = NewRarity;
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
        if (effective!= baseStat&state.isActive)
            return $"<s>+{baseStat:F0}</s> +{effective:F0} HP";
        return $"+{baseStat:F0} HP";
    }

}