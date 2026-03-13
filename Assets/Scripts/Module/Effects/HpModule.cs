using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = {0f, 0f, 0f, 0f, 0f};
    public float levelMultiplier;
    public int[] cost = {0,0,0,0,0};

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat(state) });
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
        if (state.totalBuffPercent > 0f)
            return $"<s>+{baseStat:F0}</s> +{effective:F0} HP";
        return $"+{baseStat:F0} HP";
    }

    private float GetEffectiveStat(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}