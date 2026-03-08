using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpEffect : ModuleEffect
{
    public float bonusHp;

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { health = bonusHp });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { health = bonusHp });
    }
}