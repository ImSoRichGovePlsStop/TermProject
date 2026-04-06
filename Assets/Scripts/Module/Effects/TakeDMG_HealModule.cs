using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/TakeDamage2xHeal")]
public class TakeDMG_HealModule : ModuleEffect
{
    [Header("Stat per Rarity (Rare -> Legendary)")]
    [Tooltip("Extra damage taken multiplier per rarity (e.g. 0.2 = +20% damage taken)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Heal percent of damage taken per rarity (e.g. 0.5 = heal 50% of damage taken)")]
    public float[] healStatPerRarity;

    [Tooltip("Delay in seconds before the heal triggers after taking damage")]
    public int cooldown;

    [Tooltip("Level multiplier")]
    public float levelMultiplier;

    private readonly Dictionary<ModuleRuntimeState, StateData> _stateMap = new();

    private class StateData
    {
        public Action<float> DamageHandler;
        public float AppliedDamageTaken;
        public List<Coroutine> ActiveHeals = new();
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, level);

        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken });

        var data = new StateData
        {
            AppliedDamageTaken = state.dmgTaken,
        };

        // Cancel any ongoing heals then start a new one for this hit
        data.DamageHandler = damage =>
        {
            foreach (var c in data.ActiveHeals)
                if (c != null) stats.StopCoroutine(c);
            data.ActiveHeals.Clear();

            var coroutine = stats.StartCoroutine(HealAfterDelay(stats, damage * GetEffectiveStat(state), cooldown, data.ActiveHeals));
            data.ActiveHeals.Add(coroutine);
        };

        stats.OnPlayerDamaged += data.DamageHandler;
        _stateMap[state] = data;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;

        stats.RemoveMultiplierModifier(new StatModifier { damageTaken = data.AppliedDamageTaken });
        stats.OnPlayerDamaged -= data.DamageHandler;

        _stateMap.Remove(state);
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;
        if (state.buffRarity > rarity)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }

        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        stats.RemoveMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        if (state.buffRarity > rarity)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;

        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (state.buffRarity > newRarity) return;
        state.buffRarity = newRarity;
        if (state.buffedLevel > level)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, level);
        }

        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);

        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        stats.RemoveMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        if (state.buffedLevel > level)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, level);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, level);
        }
        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent += percent;
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent -= percent;
    }

    private IEnumerator HealAfterDelay(PlayerStats stats, float healAmount, float duration, List<Coroutine> activeHeals)
    {
        if (duration <= 0f)
        {
            if (stats != null) stats.Heal(healAmount);
            yield break;
        }

        float healPerSec = healAmount / duration;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            yield return null;
            if (stats == null) yield break;

            float tick = Time.deltaTime;
            elapsed += tick;
            stats.Heal(healPerSec * tick);
        }
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
        => $"Take more damage, but recover a portion of it over {cooldown}s";

    public override string PassiveDescription => $"Recover a portion of damage taken over {cooldown}s";
    public override PassiveLayout GetPassiveLayout() => PassiveLayout.Single;

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float unbuffed = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        bool hasRarityBuff = state.buffRarity != 0 && state.buffRarity != rarity
                             && System.Array.Exists(state.baseRarity, v => v > 0);
        Rarity effectiveRarity = hasRarityBuff ? state.buffRarity : rarity;
        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        float buffed = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, effectiveLevel);
        return (unbuffed, buffed);
    }

    public override (string leftLabel, float before, float after, string format) GetStatPreview(
        Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats)
    {
        float moduleStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = state.isActive ? state.dmgTaken : moduleStat;
        bool isBuffed = state.isActive && effective != moduleStat;
        string leftLabel = isBuffed
            ? $"+{effective * 100f:F0}% Damage Taken"
            : $"+{moduleStat * 100f:F0}% Damage Taken";

        if (playerStats == null) return (leftLabel, -1f, -1f, "F0%");

        float before, after;
        if (state.isActive)
        {
            before = (playerStats.DamageTaken - effective) * 100f;
            after = playerStats.DamageTaken * 100f;
        }
        else
        {
            before = playerStats.DamageTaken * 100f;
            after = (playerStats.DamageTaken + moduleStat) * 100f;
        }
        return (leftLabel, before, after, "F0%");
    }

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseHeal = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, level);
        float effectiveHeal = state.isActive ? GetEffectiveStat(state) : baseHeal;
        bool healBuffed = state.isActive && effectiveHeal != baseHeal;

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"{effectiveHeal * 100f:F0}%",
                label         = "Heal of Damage",
                sublabel      = "Conditional",
                isBuffed      = healBuffed,
                unbuffedValue = $"{baseHeal * 100f:F0}%"
            }
        };
    }
}