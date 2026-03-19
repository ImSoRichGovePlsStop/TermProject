 using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/AttackSpeed")]
public class AttackSpeedModule: ModuleEffect
{
    [Header("Stat per Rarity (Rare -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f ,0f};
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
    }

    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveAttackSpeed(state) });
        state.buffedLevel += levelBonus;
        state.currentStat += levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveAttackSpeed(state) });
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            state.currentStat -= levelBonus * levelMultiplier;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveAttackSpeed(state) });
        state.buffedLevel -= levelBonus;
        state.currentStat -= levelBonus * levelMultiplier * baseStatPerRarity[(int)rarity];
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveAttackSpeed(state) });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        stats.RemoveFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
        state.totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
        state.totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { attackSpeed = GetEffectiveAttackSpeed(state) });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = GetEffectiveAttackSpeed(state);
        if (effective != baseStat & state.isActive)
            return $"<s>+{baseStat * 100f:F0}%</s> +{effective * 100f:F0}% Attack Speed";
        return $"+{baseStat * 100f:F0}% Attack speed";
    }

    private float GetEffectiveAttackSpeed(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }
}
