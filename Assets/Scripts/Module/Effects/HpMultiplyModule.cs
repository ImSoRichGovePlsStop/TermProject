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


    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveStat(state);
        if (state.totalBuffPercent > 0f)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective * 100f:F0}% Hp";
        return $"+{baseStat * 100f:F0}% Hp";
    }

    private float GetEffectiveStat(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}