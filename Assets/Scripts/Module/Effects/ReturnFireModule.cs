using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/ReturnFire")]
public class ReturnFireModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    [Tooltip("Return damage % per rarity (e.g. 0.2 = 20% of player's damage returned to attacker)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Level multiplier")]
    public float levelMultiplier;

    private readonly Dictionary<ModuleRuntimeState, Action> _stateMap = new();

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);

        var ctx = stats.GetComponent<PlayerCombatContext>();

        Action handler = () =>
        {
            if (ctx.LastAttacker == null || ctx.LastAttacker.IsDead) return;

            float returnDamage = stats.Damage * GetEffectiveStat(state);

            ctx.LastAttacker.TakeDamage(returnDamage);
        };

        ctx.OnTakeDamage += handler;
        _stateMap[state] = handler;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var handler)) return;

        var ctx = stats.GetComponent<PlayerCombatContext>();
        ctx.OnTakeDamage -= handler;

        _stateMap.Remove(state);
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.ContainsKey(state)) return;

        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;

        if (!_stateMap.ContainsKey(state)) return;
        if (!state.isActive) return;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;

        if (!_stateMap.ContainsKey(state)) return;
        if (state.buffRarity > newRarity) return;

        state.buffRarity = newRarity;
        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);

        if (!_stateMap.ContainsKey(state)) return;
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.ContainsKey(state)) return;
        state.totalBuffPercent += percent;
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent -= percent;
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state) => null;

    public override string PassiveDescription => "Reflect a portion of your damage back to any attacker";
    public override PassiveLayout GetPassiveLayout() => PassiveLayout.Single;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effective = state.isActive ? GetEffectiveStat(state) : baseStat;
        bool isBuffed = state.isActive && effective != baseStat;

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"{effective * 100f:F0}%",
                label         = "Return Damage",
                sublabel      = "Conditional",
                isBuffed      = isBuffed,
                unbuffedValue = $"{baseStat * 100f:F0}%"
            }
        };
    }
}