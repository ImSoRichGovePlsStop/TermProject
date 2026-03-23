using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Buff Rarity")]
public class BuffRarityModule : ModuleEffect
{
    [Header("Buff Level per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state) { }

    public float GetBuffRarity(ModuleRuntimeState state)
    {
        return state.currentStat;
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float stat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        return $"Buffs adjacent module to {rarity}";
    }
}
