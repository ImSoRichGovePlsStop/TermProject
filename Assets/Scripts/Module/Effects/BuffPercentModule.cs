using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Buff Percent")]
public class BuffPercentModule : ModuleEffect
{
    [Header("Buff")]
    public float buffPercent;

    protected override void OnEquip(PlayerStats stats) { }
    protected override void OnUnequip(PlayerStats stats) { }
}