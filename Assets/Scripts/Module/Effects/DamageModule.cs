using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageEffect : ModuleEffect
{
    public float bonusDamage;

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { damage = bonusDamage });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = bonusDamage });
    }
}