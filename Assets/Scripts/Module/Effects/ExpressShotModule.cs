using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/ExpressShot")]
public class ExpressShot : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    [Tooltip("Damage multiplier bonus per rarity (e.g. 0.5 = +50% damage for one shot)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float levelMultiplier;

    [Tooltip("Cooldown in seconds before the bonus shot is ready again")]
    public int cooldown;

    private readonly Dictionary<ModuleRuntimeState, StateData> _stateMap = new();

    private class StateData
    {
        public Action HitHandler;
        public bool BuffReady;
        public Coroutine CooldownCoroutine;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        var data = new StateData
        {
            BuffReady = true
        };

        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });

        var ctx = stats.GetComponent<PlayerCombatContext>();
        data.HitHandler = () =>
        {
            if (!data.BuffReady) return;
            if (ctx.LastHitEnemies == null || ctx.LastHitEnemies.Count == 0) return;

            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
            data.BuffReady = false;
            data.CooldownCoroutine = stats.StartCoroutine(CooldownCoroutine(stats, state, data));
        };

        ctx.OnAttack += data.HitHandler;
        ctx.OnSecondaryAttack += data.HitHandler;
        _stateMap[state] = data;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });

        if (data.CooldownCoroutine != null)
            stats.StopCoroutine(data.CooldownCoroutine);

        var ctx = stats.GetComponent<PlayerCombatContext>();
        ctx.OnAttack -= data.HitHandler;
        ctx.OnSecondaryAttack -= data.HitHandler;
        _stateMap.Remove(state);
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        if (data.BuffReady)
        {
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffRarity > rarity)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, state.buffedLevel);
        }
        if (data.BuffReady)
        {
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
    }

    public override void OnRarityBuffReceived(int level, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        state.buffRarity = newRarity;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, level);
        }
        if (data.BuffReady)
        {
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
    }

    public override void OnRarityBuffRemoved(int level, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffRarity = newRarity;
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (!state.isActive) return;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        if (state.buffedLevel > level)
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, state.buffedLevel);
        }
        else
        {
            state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, newRarity, level);
        }
        if (data.BuffReady)
        {
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        }
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent += percent;
        if (data.BuffReady)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data))
        {
            state.totalBuffPercent -= percent;
            return;
        }
        if (data.BuffReady)
            stats.RemoveMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        state.totalBuffPercent -= percent;
        if (data.BuffReady)
            stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
    }

    private IEnumerator CooldownCoroutine(PlayerStats stats, ModuleRuntimeState state, StateData data)
    {
        yield return new WaitForSeconds(cooldown);
        if (!_stateMap.ContainsKey(state)) yield break;

        stats.AddMultiplierModifier(new StatModifier { damage = GetEffectiveStat(state) });
        data.BuffReady = true;
        data.CooldownCoroutine = null;
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseBonusPct = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effectiveBonusPct = GetEffectiveStat(state);

        bool bonusChanged = effectiveBonusPct > baseBonusPct;

        if (state.isActive && bonusChanged)
        {
            return
                $"Next shot deals +<s>{baseBonusPct * 100f:F0}%</s> {effectiveBonusPct * 100f:F0}% bonus damage\n" +
                $"Cooldown ({cooldown}s)";
        }
        else
        {
            return
                $"Next shot deals +{baseBonusPct * 100f:F0}% bonus damage\n" +
                $"Cooldown ({cooldown}s)";
        }
    }
}