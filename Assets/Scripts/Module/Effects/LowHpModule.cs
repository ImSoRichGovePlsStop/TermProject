using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Low HP Damage")]
public class LowHpModule : ModuleEffect
{
    public float healthThreshold;
    public float bonusDamage;

    private float totalBuffPercent = 0f;
    private bool buffActive = false;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
        buffActive = false;
    }

    private void OnDisable()
    {
        totalBuffPercent = 0f;
        buffActive = false;
    }

    protected override void OnEquip(PlayerStats stats) { }

    protected override void OnUnequip(PlayerStats stats)
    {
        if (buffActive)
        {
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
            buffActive = false;
        }
    }

    public override void OnUpdate(PlayerStats stats)
    {
        bool belowThreshold = stats.CurrentHealth / stats.MaxHealth < healthThreshold;

        if (belowThreshold && !buffActive)
        {
            buffActive = true;
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
        else if (!belowThreshold && buffActive)
        {
            buffActive = false;
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        if (!IsActive)
        {
            totalBuffPercent += percent;
            return;
        }
        if (buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent += percent;
        if (buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive)
        {
            totalBuffPercent -= percent;
            return;
        }
        if (buffActive)
            stats.RemoveFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
        totalBuffPercent -= percent;
        if (buffActive)
            stats.AddFlatModifier(new StatModifier { damage = GetEffectiveDamage() });
    }

    private float GetEffectiveDamage()
    {
        return bonusDamage * (1 + totalBuffPercent);
    }
}