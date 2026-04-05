using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/CheatDeath")]
public class CheatDeathModule : ModuleEffect
{
    [Header("Heal % of Max HP on lethal hit (Epic -> Legendary)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };

    [Tooltip("Level multiplier")]
    public float levelMultiplier;

    [Tooltip("Invincibility window in seconds granted after cheat death triggers")]
    public float invincibilityDuration = 2f;

    private readonly Dictionary<ModuleRuntimeState, StateData> _stateMap = new();

    private class StateData
    {
        public Action<float> DamageHandler;
        public bool HasTriggered;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);

        var data = new StateData();

        // OnPlayerDamaged fires BEFORE the death check inside TakeDamage.
        // Healing here prevents OnPlayerDeath from ever firing.
        data.DamageHandler = _ =>
        {
            if (data.HasTriggered) return;
            if (stats.CurrentHealth > 0f) return;

            data.HasTriggered = true;

            // Heal back by the effective stat percent of max HP
            stats.HealPercent(GetEffectiveStat(state));

            // Brief invincibility so the player isn't immediately killed again
            stats.StartCoroutine(GrantInvincibility(stats));

            // Consume the module — defer by one frame to avoid mutating
            // the module list while TakeDamage / event dispatch is mid-stack
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

    // ── Buff handlers ────────────────────────────────────────────────────────

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (!state.isActive) return;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, effectiveRarity, state.buffedLevel);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (state.buffRarity > newRarity) return;

        state.buffRarity = newRarity;
        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);
        if (!_stateMap.TryGetValue(state, out _)) return;
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > level ? state.buffedLevel : level;
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, state.buffRarity, effectiveLevel);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent += percent;
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent -= percent;
    }

    // ── Description ──────────────────────────────────────────────────────────

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseHealPct = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        float effectiveHeal = GetEffectiveStat(state);
        bool healChanged = state.isActive && effectiveHeal != baseHealPct;

        string healLine = healChanged
            ? $"Heal <s>{baseHealPct * 100f:F0}%</s> {effectiveHeal * 100f:F0}% of max HP"
            : $"Heal {baseHealPct * 100f:F0}% of max HP";

        return
            $"Prevent one fatal hit\n" +
            $"{healLine} upon lethal damage\n" +
            $"Consumed on trigger";
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private IEnumerator GrantInvincibility(PlayerStats stats)
    {
        stats.SetInvincible(true);
        yield return new WaitForSeconds(invincibilityDuration);
        if (stats != null)
            stats.SetInvincible(false);
    }

    private IEnumerator ConsumeModule(PlayerStats stats, ModuleRuntimeState state)
    {
        // Wait one frame so we're fully outside the damage/event call stack
        yield return null;

        var mgr = InventoryManager.Instance;
        if (mgr == null) yield break;

        // WeaponGrid.Remove fires OnModuleRemoved → OnModuleUnequipped,
        // which drives ModuleEffectHandler.OnModuleUnequipped cleanly.
        // The module is consumed (not sent to the bag).
        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            if (inst.RuntimeState != state) continue;
            mgr.WeaponGrid.Remove(inst);
            break;
        }
    }
}