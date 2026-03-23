using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/HpMultiply")]
public class HpMultiplyModule : ModuleEffect
{
    [Header("Stat per Rarity (Rare - Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffedLevel += levelBonus;
        state.currentStat += levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
        Debug.Log($"Hp increase : {levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity]}");
        Debug.Log($"Hp : {state.currentStat}");
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            state.currentStat -= levelBonus * levelMultiplier;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.buffedLevel -= levelBonus;
        state.currentStat -= levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        stats.AddMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        stats.AddMultiplierModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        if (effective != baseStat & state.isActive)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective* 100f:F0}% HP";
        return $"+{baseStat * 100f:F0}% HP";
    }

    private float GetEffectiveStat(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}