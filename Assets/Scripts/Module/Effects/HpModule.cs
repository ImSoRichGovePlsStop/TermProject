using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 50f, 80f, 120f, 170f, 230f };
    public float levelMultiplier = 0.10f;

    private float _totalBuffPercent = 0f;
    private float _currentStat = 0f;

    private void OnEnable()
    {
        _totalBuffPercent = 0f;
        _currentStat = 0f;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level)
    {
        _currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
        _totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { _totalBuffPercent -= percent; return; }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveStat() });
        _totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveStat() });
    }

    private float GetEffectiveStat() => _currentStat * (1f + _totalBuffPercent);
}