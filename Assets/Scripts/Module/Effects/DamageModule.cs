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
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }
    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveDamage(state) });
        state.buffedLevel += levelBonus;
        state.currentStat += levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveDamage(state) });
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            state.currentStat -= levelBonus * levelMultiplier;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveDamage(state) });
        state.buffedLevel -= levelBonus;
        state.currentStat -= levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveDamage(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent += percent;
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
        state.totalBuffPercent -= percent;
        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveDamage(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveDamage(state);
        if (effective != baseStat & state.isActive)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective * 100f:F0}% Damage";
        return $"+{baseStat * 100f:F0}% Damage";
    }

    private float GetEffectiveDamage(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}