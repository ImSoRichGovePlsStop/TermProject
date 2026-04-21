using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/OnKillAddMaxHp")]
public class OnKillAddMaxHpModule : ModuleEffect
{
    [Header("HP per stack (Common -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Level multiplier")]
    public float levelMultiplier;

    [Tooltip("Maximum number of stacks allowed")]
    public int maxStacks = 100;

    private readonly Dictionary<ModuleRuntimeState, Action<HealthBase>> _stateMap = new();
    private readonly Dictionary<ModuleRuntimeState, int> _stackMap = new();

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);

        if (!_stackMap.ContainsKey(state)) _stackMap[state] = 0;

        state.hpAdded = _stackMap[state] * state.currentStat;

        float existingEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (existingEffectiveHp > 0)
        {
            stats.AddFlatModifier(new StatModifier { health = existingEffectiveHp });
        }

        var ctx = stats.GetComponent<PlayerCombatContext>();

        Action<HealthBase> handler = (enemy) =>
        {
            if (enemy == null) return;
            if (_stackMap[state] >= maxStacks) return;

            float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
            if (oldEffectiveHp > 0)
            {
                stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });
            }

            _stackMap[state]++;

            state.hpAdded = _stackMap[state] * state.currentStat;

            float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
            stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
        };

        ctx.OnEntityKilled += handler;
        _stateMap[state] = handler;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var handler)) return;

        var ctx = stats.GetComponent<PlayerCombatContext>();
        if (ctx != null)
        {
            ctx.OnEntityKilled -= handler;
        }

        _stateMap.Remove(state);

        float existingEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (existingEffectiveHp > 0)
        {
            stats.RemoveFlatModifier(new StatModifier { health = existingEffectiveHp });
        }
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);

        // Recalculate to apply the level up to all existing stacks retroactively
        int stacks = _stackMap.TryGetValue(state, out int s) ? s : 0;
        state.hpAdded = stacks * state.currentStat;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.buffedLevel -= levelBonus;
            return;
        }

        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        state.buffedLevel -= levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);

        int stacks = _stackMap.TryGetValue(state, out int s) ? s : 0;
        state.hpAdded = stacks * state.currentStat;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;

        if (state.buffRarity > newRarity | oldRarity > newRarity) return;

        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        state.buffRarity = newRarity;
        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;

        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);

        int stacks = _stackMap.TryGetValue(state, out int s) ? s : 0;
        state.hpAdded = stacks * state.currentStat;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);

        if (!state.isActive) return;

        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);

        int stacks = _stackMap.TryGetValue(state, out int s) ? s : 0;
        state.hpAdded = stacks * state.currentStat;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        state.totalBuffPercent += percent;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.isActive && newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }

        float oldEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (oldEffectiveHp > 0) stats.RemoveFlatModifier(new StatModifier { health = oldEffectiveHp });

        state.totalBuffPercent -= percent;

        float newEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        if (newEffectiveHp > 0) stats.AddFlatModifier(new StatModifier { health = newEffectiveHp });
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (state.hpAdded <= 0) return "";

        float totalEffective = state.hpAdded * (1f + state.totalBuffPercent);
        if (state.totalBuffPercent != 0)
            return $"Total Added: <s>{state.hpAdded:F1}</s> {totalEffective:F1} HP";
        else
            return $"Total Added: {state.hpAdded:F1} HP";
    }

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
        => BuildBaseModuleStat(baseStatPerRarity, levelMultiplier, rarity, level, state);

    public override string PassiveDescription => $"Gain 1 stack on enemy kill (Max {maxStacks}), each stack grants Max HP.";
    public override PassiveLayout GetPassiveLayout() => PassiveLayout.TwoEqual;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float currentPerStack = state.isActive ? state.currentStat : baseStat;
        bool isBuffed = state.isActive && currentPerStack != baseStat;

        float totalEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
        int stacks = _stackMap.TryGetValue(state, out int s) ? s : 0;

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"{currentPerStack:F1}",
                label         = "HP per stack",
                sublabel      = $"Total stack : {stacks}/{maxStacks}",
                isBuffed      = isBuffed,
                unbuffedValue = $"{baseStat:F1}"
            },
            new PassiveEntry
            {
                value         = $"{totalEffectiveHp:F0}",
                label         = "Total HP Added",
                isBuffed      = state.isActive && state.totalBuffPercent != 0,
                unbuffedValue = $"{state.hpAdded:F0}"
            }
        };
    }

    public override (string leftLabel, float before, float after, string format) GetStatPreview(
        Rarity rarity, int level, ModuleRuntimeState state, PlayerStats playerStats)
    {
        float totalEffective = state.hpAdded * (1f + state.totalBuffPercent);

        string leftLabel = BuildLeftLabel(state.hpAdded, totalEffective, state, "Max Health", false);

        if (playerStats == null) return (leftLabel, -1f, -1f, "F0");

        float baseHp = playerStats.BaseHealth;
        float multHp = playerStats.MultiplierModifier.health;

        float totalEffectiveHp = state.isActive ? (state.hpAdded * (1f + state.totalBuffPercent)) : 0f;

        float before, after;
        if (state.isActive)
        {
            before = (baseHp - totalEffectiveHp) * (1f + multHp);
            after = playerStats.MaxHealth;
        }
        else
        {
            float potentialEffectiveHp = state.hpAdded * (1f + state.totalBuffPercent);
            before = playerStats.MaxHealth;
            after = (baseHp + potentialEffectiveHp) * (1f + multHp);
        }

        return (leftLabel, before, after, "F0");
    }
}