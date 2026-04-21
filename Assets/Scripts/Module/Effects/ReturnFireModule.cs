using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/ReturnFire")]
public class ReturnFireModule : ModuleEffect
{
    [Header("Stat per Rarity (Common -> Legendary)")]
    [Tooltip("Return % of damage received back to attacker (e.g. 0.5 = 50% of damage taken returned)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Level multiplier")]
    public float levelMultiplier;

    private readonly Dictionary<ModuleRuntimeState, Action<float>> _stateMap = new();

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);

        var ctx = stats.GetComponent<PlayerCombatContext>();

        Action<float> handler = (damageTaken) =>
        {
            if (ctx.LastAttacker == null || ctx.LastAttacker.IsDead) return;

            float reflectedDamage = damageTaken * GetEffectiveStat(state);

            ctx.LastAttacker.TakeDamage(reflectedDamage);
        };

        stats.OnPlayerDamaged += handler;
        _stateMap[state] = handler;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var handler)) return;

        stats.OnPlayerDamaged -= handler;
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
        if (state.buffRarity > newRarity | oldRarity > newRarity) return;

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

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state) => PassiveDescription;

    public override string PassiveDescription => "Reflect a portion of damage taken back to the attacker";
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
                label         = "Reflect Damage",
                sublabel      = "Of Damage Taken",
                isBuffed      = isBuffed,
                unbuffedValue = $"{baseStat * 100f:F0}%"
            }
        };
    }
}