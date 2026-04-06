using UnityEngine;
using System;

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
    public virtual void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state) { }
    public virtual void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state) { }

    public virtual void FindNextRarity(Rarity oldRarity, ModuleRuntimeState state)
    {
        state.buffRarity = oldRarity;
        for (int i = state.baseRarity.Length - 1; i >= 0; i--)
        {
            if (state.baseRarity[i] > 0)
            {
                state.buffRarity = (Rarity)Math.Max(i, (int)oldRarity);
                break;
            }
        }
    }

    protected abstract void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state);
    protected abstract void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state);

    public abstract string GetDescription(Rarity rarity, int level, ModuleRuntimeState state);

    public virtual string[] BoldKeywords => System.Array.Empty<string>();

    public virtual (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
    {
        return (-1f, -1f);
    }

    protected (float unbuffed, float buffed) BuildBaseModuleStat(float[] baseStatPerRarity, float levelMultiplier, Rarity rarity, int level, ModuleRuntimeState state)
    {
        bool hasRarityBuff = state.buffRarity != 0 && state.buffRarity != rarity
                             && System.Array.Exists(state.baseRarity, v => v > 0);
        Rarity effectiveRarity = hasRarityBuff ? state.buffRarity : rarity;
        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        float unbuffed = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float buffed = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, effectiveLevel)
                       * (1f + state.totalBuffPercent);
        return (unbuffed, buffed);
    }
    public virtual (string label, float baseStat, float effectiveStat, bool isPercent) GetStatLine(Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats = null)
    {
        return (null, 0f, 0f, false);
    }

    // Returns "+50% Damage" or buffed version "+60% Damage" depending on state
    protected string BuildLeftLabel(float moduleStat, float effective, ModuleRuntimeState state, string statName, bool isPercent)
    {
        bool isBuffed = state.isActive && effective != moduleStat;
        float displayStat = isBuffed ? effective : moduleStat;
        return isPercent
            ? $"+{displayStat * 100f:F0}% {statName}"
            : $"+{displayStat:F0} {statName}";
    }

    // Returns unbuffed label string for strikethrough display
    protected string BuildUnbuffedLabel(float moduleStat, string statName, bool isPercent)
    {
        return isPercent
            ? $"+{moduleStat * 100f:F0}% {statName}"
            : $"+{moduleStat:F0} {statName}";
    }

    // Returns: leftLabel (e.g. "+50% Damage"), playerStatBefore, playerStatAfter, formatString (e.g. "F1", "F0", "F0%")
    public virtual (string leftLabel, float before, float after, string format) GetTooltipStats(
        Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats)
    {
        return (null, -1f, -1f, "F1");
    }

    public virtual string GetLevelText(int level, ModuleRuntimeState state)
    {
        if (state.buffedLevel != 0 && state.buffedLevel != level)
            return $"<s>Lv.{level}</s> Lv.{state.buffedLevel}";
        return $"Lv.{level}";
    }
    public virtual string GetRarityText(Rarity rarity, ModuleRuntimeState state)
    {
        if (state.buffRarity != 0 && state.buffRarity != rarity)
            return $"<s>{rarity}</s> {state.buffRarity}";
        return $"{rarity}";
    }

    public virtual float GetEffectiveStat(ModuleRuntimeState state)
    {
        return state.currentStat * (1f + state.totalBuffPercent);
    }

    protected float GetFinalStat(float[] baseStatPerRarity, float levelMultiplier, Rarity rarity, int level)
    {
        if (baseStatPerRarity == null || baseStatPerRarity.Length == 0) return 0f;
        int index = Mathf.Clamp((int)rarity, 0, baseStatPerRarity.Length - 1);
        return baseStatPerRarity[index] * (1f + level * levelMultiplier);
    }
}