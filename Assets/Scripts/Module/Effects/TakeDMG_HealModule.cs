using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

    public override void OnLevelBuffReceived(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        state.buffedLevel = levelBonus;
        if (state.buffRarity > rarity)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, levelBonus);
        }

        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnLevelBuffRemoved(int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel = levelBonus;
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        stats.RemoveMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        if (state.buffRarity > rarity)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, state.buffRarity, levelBonus);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, levelBonus);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, levelBonus);
        }
        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnRarityBuffReceived(int level, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        state.buffRarity = newRarity;
        if (state.buffedLevel > level)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, level);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, newRarity, level);
        }

        stats.AddMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        data.AppliedDamageTaken = state.dmgTaken;
    }

    public override void OnRarityBuffRemoved(int level, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffRarity = newRarity;
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        stats.RemoveMultiplierModifier(new StatModifier { damageTaken = state.dmgTaken - 1 });
        if (state.buffedLevel > level)
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
        }
        else
        {
            state.dmgTaken = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, level);
            state.currentStat = GetFinalStat(healStatPerRarity, levelMultiplier, newRarity, level);
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
    {
        float basedamageTakenPct = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float basehealPct = GetFinalStat(healStatPerRarity, levelMultiplier, rarity, level);

        float effectivedamageTakenPct = state.dmgTaken;
        float effectivehealPct = GetEffectiveStat(state);

        bool dmgChanged = effectivedamageTakenPct != basedamageTakenPct;
        bool healChanged = effectivehealPct != basehealPct;

        if (state.isActive && dmgChanged && healChanged)
        {
            return
                $"+<s>{basedamageTakenPct * 100f:F0}%</s> {effectivedamageTakenPct * 100f:F0}% damage taken\n" +
                $"Heal overtime <s>{basehealPct * 100f:F0}%</s> {effectivehealPct * 100f:F0}% of taken damage\n" +
                $"Cooldown ({cooldown}s)";
        }
        else if (state.isActive && healChanged && !dmgChanged)
        {
            return
                $"+{basedamageTakenPct * 100f:F0}% damage taken\n" +
                $"Heal overtime <s>{basehealPct * 100f:F0}%</s> {effectivehealPct * 100f:F0}% of taken damage\n" +
                $"Cooldown ({cooldown}s)";
        }
        else
        {
            return
                $"+{basedamageTakenPct * 100f:F0}% damage taken\n" +
                $"Heal overtime {basehealPct * 100f:F0}% of taken damage\n" +
                $"Cooldown ({cooldown}s)";
        }
    }
}