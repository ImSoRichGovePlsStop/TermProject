using UnityEngine;

public abstract class ModuleEffect : ScriptableObject
{
    public void Equip(PlayerStats stats) => OnEquip(stats);
    public void Unequip(PlayerStats stats) => OnUnequip(stats);

    public virtual void OnUpdate(PlayerStats stats) { }
    public virtual void OnLevelUp() { }
    public virtual void OnLevelDown() { }

    protected abstract void OnEquip(PlayerStats stats);
    protected abstract void OnUnequip(PlayerStats stats);
}