using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 10f, 15f, 22f, 32f, 45f };
    public float levelMultiplier = 0.15f;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveDamage(state);
        if (state.totalBuffPercent > 0f)
            return $"<s>+{baseStat:F0}</s> +{effective:F0} Damage";
        return $"+{baseStat:F0} Damage";
    }

    private float GetEffectiveDamage(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}