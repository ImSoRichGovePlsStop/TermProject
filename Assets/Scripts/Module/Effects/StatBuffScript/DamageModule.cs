using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageEffect : ModuleEffect
{
    public float bonusDamage;
    public int level;
    public float bonusPerLevel;
    public int sizeCount;

    private void OnLevelup()
    {
        bonusDamage = bonusDamage + bonusPerLevel;
        level++;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddMultiplierModifier(new StatModifier { damage = bonusDamage });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveMultiplierModifier(new StatModifier { damage = bonusDamage });
    }
}