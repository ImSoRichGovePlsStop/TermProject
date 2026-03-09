using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/CritChance")]
public class CritRateEffect : ModuleEffect
{
    public float bonusCritChance;
    public int level;
    public float bonusPerLevel;
    public int sizeCount;

    private void OnLevelup()
    {
        bonusCritChance = bonusCritChance + bonusPerLevel;
        level++;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { critChance = bonusCritChance });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { critChance = bonusCritChance });
    }
}