using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Buff Percent")]
public class BuffPercentModule : ModuleEffect
{
    [Header("Buff Percent per Rarity (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0.05f, 0.08f, 0.12f, 0.17f, 0.25f };
    public float levelMultiplier = 0.10f;

    private float _currentBuff = 0f;

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level)
    {
        _currentBuff = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level) { }

    public float GetBuffPercent() => _currentBuff;
}