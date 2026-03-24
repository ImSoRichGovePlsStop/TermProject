using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }
    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.buffedLevel = levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
        }
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel = levelBonus;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.buffedLevel = levelBonus;
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
        }
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffReceived(int level, Rarity NewRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.buffRarity = NewRarity;
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnRarityBuffRemoved(int level, Rarity NewRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffRarity = NewRarity;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.buffRarity = NewRarity;
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        if (effective != baseStat & state.isActive)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective * 100f:F0}% Damage";
        return $"+{baseStat * 100f:F0}% Damage";
    }

}