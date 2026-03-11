using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Buff Percent")]
public class BuffPercentModule : ModuleEffect
{
    [Header("Buff Percent per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0.05f, 0.08f, 0.12f, 0.17f, 0.25f };
    public float levelMultiplier = 0.10f;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state) { }

    public float GetBuffPercent(ModuleRuntimeState state)
    {
        return state.currentStat;
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float stat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        return $"Buffs adjacent modules: +{stat * 100f:F0}%";
    }
}