using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Damage")]
public class DamageModule : ModuleEffect
{
    public float bonusDamage;
    private float totalBuffPercent = 0f;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive)
        {
            totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    private float GetEffectiveDamage()
    {
        return bonusDamage * (1 + totalBuffPercent);
    }
}