using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Hp")]
public class HpModule : ModuleEffect
{
    public float bonusHp;
    private float totalBuffPercent = 0f;

    private void OnEnable()
    {
        totalBuffPercent = 0f;
    }

    protected override void OnEquip(PlayerStats stats)
    {
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveHp() });
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveHp() });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats)
    {
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveHp() });
        totalBuffPercent += percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveHp() });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats)
    {
        if (!IsActive)
        {
            totalBuffPercent -= percent;
            return;
        }
        stats.RemoveFlatModifier(new StatModifier { health = GetEffectiveHp() });
        totalBuffPercent -= percent;
        stats.AddFlatModifier(new StatModifier { health = GetEffectiveHp() });
    }

    private float GetEffectiveHp()
    {
        return bonusHp * (1 + totalBuffPercent);
    }
}