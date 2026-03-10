using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Crit Chance")]
public class CritChanceModule : ModuleEffect
{
    public float bonusCritChance;
    private float totalBuffPercent = 0f;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
        totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive)
        {
            totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
        totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { critChance = GetEffectiveCritChance() });
    }

    private float GetEffectiveCritChance()
    {
        return bonusCritChance * (1 + totalBuffPercent);
    }
}