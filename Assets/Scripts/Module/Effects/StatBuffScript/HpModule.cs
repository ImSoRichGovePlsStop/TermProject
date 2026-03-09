using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpEffect : ModuleEffect
{
    [Header("Stats")]
    public float bonusHp;
    public float bonusPerLevel;
    public int sizeCount;

    [Header("Base Level (set in Inspector, never changed at runtime)")]
    public int baseLevel;

    private PlayerStats _stats;
    private float _appliedHp;
    private int _buffLevels;

    private void OnEnable()
    {
        _stats = null;
        _appliedHp = 0f;
        _buffLevels = 0;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        _stats = stats;
        Apply();  // apply base value immediately
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        Remove();
        _buffLevels = 0;
        _stats = null;
    }

    // Called by BuffMod — Refresh replaces the current applied value cleanly
    public override void OnLevelUp()
    {
        _buffLevels++;
        Refresh();
    }

    public override void OnLevelDown()
    {
        if (_buffLevels <= 0) return;
        _buffLevels--;
        Refresh();
    }

    private void Apply()
    {
        if (_stats == null) return;
        Remove(); // always clear before applying — prevents double-stack if called twice
        _appliedHp = bonusHp + bonusPerLevel * (baseLevel + _buffLevels);
        _stats.AddFlatModifier(new StatModifier { health = _appliedHp });
    }

    private void Remove()
    {
        if (_stats == null || _appliedHp == 0f) return;
        _stats.RemoveFlatModifier(new StatModifier { health = _appliedHp });
        _appliedHp = 0f;
    }

    private void Refresh()
    {
        Remove(); // remove whatever is currently applied
        Apply();  // re-apply with updated _buffLevels
    }
}