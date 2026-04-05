using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/CheatDeath")]
public class CheatDeathModule : ModuleEffect
{
    [Header("Heal % of Max HP on lethal hit (Epic -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Header("Invincibility duration in seconds (Epic -> Legendary)")]
    public float[] invincibilityPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Level multiplier (shared by heal and invincibility)")]
    public float levelMultiplier;

    private readonly Dictionary<ModuleRuntimeState, StateData> _stateMap = new();

    private readonly HashSet<ModuleRuntimeState> _triggeredStates = new();

    private class StateData
    {
        public Action<float> DamageHandler;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (_triggeredStates.Contains(state))
        {
            stats.StartCoroutine(ConsumeModule(stats, state));
            return;
        }

        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        state.dmgTaken = GetFinalStat(invincibilityPerRarity, levelMultiplier, rarity, level);
                
        var data = new StateData();
        data.DamageHandler = _ =>
        {
            if (_triggeredStates.Contains(state)) return;
            if (stats.CurrentHealth > 0f) return;

            _triggeredStates.Add(state);

            stats.HealPercent(GetEffectiveStat(state));
            stats.StartCoroutine(GrantInvincibility(stats, state.dmgTaken));
            stats.StartCoroutine(ConsumeModule(stats, state));
        };

        stats.OnPlayerDamaged += data.DamageHandler;
        _stateMap[state] = data;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;
        stats.OnPlayerDamaged -= data.DamageHandler;
        _stateMap.Remove(state);
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
        state.dmgTaken = GetFinalStat(invincibilityPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (!state.isActive) return;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
        state.dmgTaken = GetFinalStat(invincibilityPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (state.buffRarity > newRarity) return;

        state.buffRarity = newRarity;
        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
        state.dmgTaken = GetFinalStat(invincibilityPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
        state.dmgTaken = GetFinalStat(invincibilityPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent += percent;
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent -= percent;
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseHealPct = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float baseInvincSec = GetFinalStat(invincibilityPerRarity, levelMultiplier, rarity, level);
        float effectiveHeal = GetEffectiveStat(state);
        float effectiveInvinc = state.dmgTaken;

        bool healChanged = state.isActive && effectiveHeal != baseHealPct;
        bool invincChanged = state.isActive && effectiveInvinc != baseInvincSec;

        string healLine = healChanged
            ? $"Heal <s>{baseHealPct * 100f:F0}%</s> {effectiveHeal * 100f:F0}% of max HP"
            : $"Heal {baseHealPct * 100f:F0}% of max HP";

        string invincLine = invincChanged
            ? $"Invincible for <s>{baseInvincSec:F1}s</s> {effectiveInvinc:F1}s"
            : $"Invincible for {baseInvincSec:F1}s";

        return
            $"Prevent one fatal hit\n" +
            $"{healLine} upon lethal damage\n" +
            $"{invincLine}\n" +
            $"Consumed on trigger";
    }


    private IEnumerator GrantInvincibility(PlayerStats stats, float duration)
    {
        stats.SetInvincible(true);
        yield return new WaitForSeconds(duration);
        if (stats != null)
            stats.SetInvincible(false);
    }

    private IEnumerator ConsumeModule(PlayerStats stats, ModuleRuntimeState state)
    {
        yield return null;

        var mgr = InventoryManager.Instance;
        if (mgr == null) yield break;

        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            if (inst.RuntimeState != state) continue;
            mgr.DeleteModule(inst);
            _triggeredStates.Remove(state);
            break;
        }
    }
}