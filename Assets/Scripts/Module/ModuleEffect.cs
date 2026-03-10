using UnityEngine;

public abstract class ModuleEffect : ScriptableObject
{
    public bool IsActive { get; private set; }

    private void OnEnable() => IsActive = false;
    private void OnDisable() => IsActive = false;

    public void Equip(PlayerStats stats, Rarity rarity, int level)
    {
        if (IsActive) return;
        IsActive = true;
        OnEquip(stats, rarity, level);
    }

    public void Unequip(PlayerStats stats, Rarity rarity, int level)
    {
        if (!IsActive) return;
        IsActive = false;
        OnUnequip(stats, rarity, level);
    }

    public void ApplyBuff(ModuleEffect target, PlayerStats stats, float percent)
        => target.OnBuffReceived(percent, stats);

    public void RemoveBuff(ModuleEffect target, PlayerStats stats, float percent)
        => target.OnBuffRemoved(percent, stats);

    public virtual void OnUpdate(PlayerStats stats, Rarity rarity, int level) { }
    public virtual void OnBuffReceived(float percent, PlayerStats stats) { }
    public virtual void OnBuffRemoved(float percent, PlayerStats stats) { }

    protected abstract void OnEquip(PlayerStats stats, Rarity rarity, int level);
    protected abstract void OnUnequip(PlayerStats stats, Rarity rarity, int level);

    protected float GetFinalStat(float[] baseStatPerRarity, float levelMultiplier, Rarity rarity, int level)
    {
        if (baseStatPerRarity == null || baseStatPerRarity.Length == 0) return 0f;
        int index = Mathf.Clamp((int)rarity, 0, baseStatPerRarity.Length - 1);
        return baseStatPerRarity[index] * (1f + level * levelMultiplier);
    }
}