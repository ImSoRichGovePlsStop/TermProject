using UnityEngine;

public abstract class ModuleEffect : ScriptableObject
{
    public void Equip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (state.isActive) return;
        state.isActive = true;
        OnEquip(stats, rarity, level, state);
    }

    public void Unequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!state.isActive) return;
        state.isActive = false;
        OnUnequip(stats, rarity, level, state);
    }

    public void ApplyBuff(ModuleEffect target, PlayerStats stats, float percent, ModuleRuntimeState targetState)
    {
        target.OnBuffReceived(percent, stats, targetState);
    }

    public void RemoveBuff(ModuleEffect target, PlayerStats stats, float percent, ModuleRuntimeState targetState)
    {
        target.OnBuffRemoved(percent, stats, targetState);
    }

    public virtual void OnUpdate(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state) { }
    public virtual void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state) { }

    protected abstract void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state);
    protected abstract void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state);

    public abstract string GetDescription(Rarity rarity, int level, ModuleRuntimeState state);

    public virtual string GetLevelText( int level, ModuleRuntimeState state)
    {
        if (state.buffedLevel != 0)
            return $"<s>Lv.{level}</s> Lv.{level + state.buffedLevel}";
        return $"Lv.{level}";
    }
    public virtual string GetRarityText(Rarity rarity, ModuleRuntimeState state)
    {
        return $"{rarity}";
    }


    protected float GetFinalStat(float[] baseStatPerRarity, float levelMultiplier, Rarity rarity, int level)
    {
        if (baseStatPerRarity == null || baseStatPerRarity.Length == 0) return 0f;
        int index = Mathf.Clamp((int)rarity, 0, baseStatPerRarity.Length - 1);
        return baseStatPerRarity[index] * (1f + level * levelMultiplier);
    }
}