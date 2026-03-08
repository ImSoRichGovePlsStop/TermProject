using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public abstract class ModuleEffect : ScriptableObject
{
    public bool IsActive { get; private set; }

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

    public virtual void OnUpdate(PlayerStats stats) { }

    protected abstract void OnEquip(PlayerStats stats);
    protected abstract void OnUnequip(PlayerStats stats);
}