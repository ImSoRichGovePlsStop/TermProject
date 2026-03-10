using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 10f, 15f, 22f, 32f, 45f };
    public float levelMultiplier = 0.15f;

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
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        _totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { _totalBuffPercent -= percent; return; }
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        _totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    private float GetEffectiveDamage() => _currentStat * (1f + _totalBuffPercent);
}