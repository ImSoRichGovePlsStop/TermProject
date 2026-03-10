using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Crit Chance")]
public class CritChanceModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0.03f, 0.05f, 0.07f, 0.1f, 0.14f };
    public float levelMultiplier = 0.08f;

    private float totalBuffPercent = 0f;
    private float currentStat = 0f;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
        currentStat = 0f;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level)
    {
        currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
        totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { totalBuffPercent -= percent; return; }
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
        totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveStat() });
    }

    private float GetEffectiveStat() => currentStat * (1f + totalBuffPercent);
}