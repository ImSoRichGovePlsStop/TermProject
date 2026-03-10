using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Low HP Damage")]
public class LowHpModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 10f, 15f, 22f, 32f, 45f };
    public float levelMultiplier = 0.15f;

    [Header("HP Threshold per Rarity (Common -> Legendary)")]
    public float[] baseThresholdPerRarity = { 0.30f, 0.33f, 0.36f, 0.40f, 0.45f };
    public float thresholdPerLevel = 0.01f;

    private float _totalBuffPercent = 0f;
    private float _currentStat = 0f;
    private float _currentThreshold = 0f;
    private bool _buffActive = false;

    private void OnEnable()
    {
        _totalBuffPercent = 0f;
        _currentStat = 0f;
        _currentThreshold = 0f;
        _buffActive = false;
    }

    private void OnDisable()
    {
        _totalBuffPercent = 0f;
        _buffActive = false;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level)
    {
        _currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        int index = Mathf.Clamp((int)rarity, 0, baseThresholdPerRarity.Length - 1);
        _currentThreshold = baseThresholdPerRarity[index] + level * thresholdPerLevel;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level)
    {
        if (_buffActive)
        {
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
            _buffActive = false;
        }
    }

    public override void OnUpdate(PlayerStats stats, Rarity rarity, int level)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < _currentThreshold;

        if (belowThreshold && !_buffActive)
        {
            _buffActive = true;
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
        else if (!belowThreshold && _buffActive)
        {
            _buffActive = false;
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        if (_buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        _totalBuffPercent += percent;
        if (_buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive) { _totalBuffPercent -= percent; return; }
        if (_buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        _totalBuffPercent -= percent;
        if (_buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    private float GetEffectiveDamage() => _currentStat * (1f + _totalBuffPercent);
}