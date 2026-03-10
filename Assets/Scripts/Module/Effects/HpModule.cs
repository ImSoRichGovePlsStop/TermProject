using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 50f, 80f, 120f, 170f, 230f };
    public float levelMultiplier = 0.10f;

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
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
        totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { totalBuffPercent -= percent; return; }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
        totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    private float GetEffectiveStat() => currentStat * (1f + totalBuffPercent);
}