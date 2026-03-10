using UnityEngine;

public abstract class ModuleEffect : ScriptableObject
{
    public bool IsActive { get; private set; }

    private void OnEnable()
    {
        IsActive = false;
    }

    private void OnDisable()
    {
        IsActive = false;
    }

    public void Equip(PlayerStats stats)
    {
        if (IsActive) return;
        IsActive = true;
        OnEquip(stats);
    }

    public void Unequip(PlayerStats stats)
    {
        if (!IsActive) return;
        IsActive = false;
        OnUnequip(stats);
    }

    public void ApplyBuff(ModuleEffect target, PlayerStats stats, float percent)
    {
        target.OnBuffReceived(percent, stats);
    }

    public void RemoveBuff(ModuleEffect target, PlayerStats stats, float percent)
    {
        target.OnBuffRemoved(percent, stats);
    }

    public virtual void OnUpdate(PlayerStats stats) { }
    public virtual void OnBuffReceived(float percent, PlayerStats stats) { }
    public virtual void OnBuffRemoved(float percent, PlayerStats stats) { }

    protected abstract void OnEquip(PlayerStats stats);
    protected abstract void OnUnequip(PlayerStats stats);
}