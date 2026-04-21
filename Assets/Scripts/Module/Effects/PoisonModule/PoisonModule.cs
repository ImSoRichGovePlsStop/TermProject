using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/Poison Dual-Tick")]
public class PoisonModule : ModuleEffect
{
    [Header("Attack Damage Settings")]
    [Tooltip("Flat damage percent per tick (Stored in currentStat)")]
    public float[] baseStatPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float atkTickInterval = 1f;

    [Header("Max HP Damage Settings")]
    [Tooltip("Enemy max HP percent per tick (Stored in hpPercent)")]
    public float[] hpPercentPerRarity = { 0f, 0f, 0f, 0f, 0f };
    public float hpTickInterval = 2f;

    [Header("General Settings")]
    [Tooltip("Poison duration in seconds (Stored in duration)")]
    public float[] durationPerRarity = { 0f, 0f, 0f, 0f, 0f };
    [Tooltip("Hit stacks required (Stored in stacks)")]
    public int[] stacksRequiredPerRarity = { 5, 5, 4, 4, 3 };
    [Tooltip("Time in seconds before a stack decays if no new hits occur")]
    public float stackDecayDelay = 3f;
    public float levelMultiplier;

    [Header("Indicator")]
    [Tooltip("Prefab with a PoisonIndicator component (Canvas/FillCircle + Canvas/StackText children)")]
    public GameObject indicatorPrefab;

    private readonly Dictionary<ModuleRuntimeState, StateData> _stateMap = new();

    private class StateData
    {
        public Action HitHandler;
        public Dictionary<HealthBase, int> EnemyStacks = new();
        public Dictionary<HealthBase, Coroutine> StackDecayCoroutines = new();
        public Dictionary<HealthBase, (Coroutine atk, Coroutine hp)> ActiveProcs = new();
        public Dictionary<HealthBase, PoisonIndicator> Indicators = new();
    }

    private void RefreshStateStats(ModuleRuntimeState state, Rarity rarity, int level)
    {
        state.currentStat = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);
        state.hpPercent = GetFinalStat(hpPercentPerRarity, levelMultiplier, rarity, level);
        state.duration = GetFinalStat(durationPerRarity, levelMultiplier, rarity, level);
        state.stacks = stacksRequiredPerRarity[(int)rarity];
    }

    private float GetEffDmg(ModuleRuntimeState s) => s.currentStat * (1f + s.totalBuffPercent);
    private float GetEffHp(ModuleRuntimeState s) => s.hpPercent * (1f + s.totalBuffPercent);
    private float GetEffDur(ModuleRuntimeState s) => s.duration * (1f + s.totalBuffPercent);

    private PoisonIndicator GetOrCreateIndicator(HealthBase enemy, StateData data)
    {
        if (data.Indicators.TryGetValue(enemy, out var existing) && existing != null)
            return existing;

        if (indicatorPrefab == null) return null;

        var go = Instantiate(indicatorPrefab, enemy.transform);
        var indicator = go.GetComponent<PoisonIndicator>();
        indicator.Init();

        data.Indicators[enemy] = indicator;
        return indicator;
    }

    private void DestroyIndicator(HealthBase enemy, StateData data)
    {
        if (!data.Indicators.TryGetValue(enemy, out var indicator)) return;
        if (indicator != null) Destroy(indicator.gameObject);
        data.Indicators.Remove(enemy);
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        RefreshStateStats(state, rarity, level);

        var data = new StateData();
        var ctx = stats.GetComponent<PlayerCombatContext>();

        data.HitHandler = () => HandleHit(ctx, stats, state, data);
        ctx.OnAttack += data.HitHandler;
        ctx.OnSecondaryAttack += data.HitHandler;

        _stateMap[state] = data;
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        if (!_stateMap.TryGetValue(state, out var data)) return;

        var ctx = stats.GetComponent<PlayerCombatContext>();
        ctx.OnAttack -= data.HitHandler;
        ctx.OnSecondaryAttack -= data.HitHandler;

        foreach (var proc in data.ActiveProcs.Values)
        {
            if (proc.atk != null) stats.StopCoroutine(proc.atk);
            if (proc.hp != null) stats.StopCoroutine(proc.hp);
        }
        foreach (var decay in data.StackDecayCoroutines.Values)
            if (decay != null) stats.StopCoroutine(decay);

        foreach (var enemy in new List<HealthBase>(data.Indicators.Keys))
            DestroyIndicator(enemy, data);

        _stateMap.Remove(state);
    }

    private void HandleHit(PlayerCombatContext ctx, PlayerStats stats, ModuleRuntimeState state, StateData data)
    {
        if (ctx.LastHitEnemies == null || ctx.LastHitEnemies.Count == 0) return;

        int required = (int)state.stacks;

        foreach (var enemy in ctx.LastHitEnemies)
        {
            if (enemy == null || data.ActiveProcs.ContainsKey(enemy)) continue;

            data.EnemyStacks.TryGetValue(enemy, out int current);
            current++;

            if (current >= required)
            {
                data.EnemyStacks[enemy] = 0;

                if (data.StackDecayCoroutines.TryGetValue(enemy, out var decay) && decay != null)
                {
                    stats.StopCoroutine(decay);
                    data.StackDecayCoroutines.Remove(enemy);
                }

                float duration = GetEffDur(state);

                GetOrCreateIndicator(enemy, data)?.ShowPoisoned(duration);

                var atkRoutine = stats.StartCoroutine(AtkPoisonRoutine(enemy, stats, state, data));
                Coroutine hpRoutine = null;
                if (GetEffHp(state) > 0f)
                    hpRoutine = stats.StartCoroutine(HpPoisonRoutine(enemy, stats, state, data));

                data.ActiveProcs[enemy] = (atkRoutine, hpRoutine);
            }
            else
            {
                data.EnemyStacks[enemy] = current;

                if (data.StackDecayCoroutines.TryGetValue(enemy, out var decay) && decay != null)
                    stats.StopCoroutine(decay);
                data.StackDecayCoroutines[enemy] =
                    stats.StartCoroutine(StackDecayRoutine(enemy, stats, state, data, required));

                GetOrCreateIndicator(enemy, data)?.ShowStacking(current, required);
            }
        }
    }

    private IEnumerator StackDecayRoutine(
        HealthBase enemy, PlayerStats stats, ModuleRuntimeState state, StateData data, int required)
    {
        while (enemy != null)
        {
            yield return new WaitForSeconds(stackDecayDelay);

            if (!data.EnemyStacks.TryGetValue(enemy, out int current) || current <= 0) break;

            current--;

            if (current <= 0)
            {
                data.EnemyStacks.Remove(enemy);

                if (data.Indicators.TryGetValue(enemy, out var ind) && ind != null)
                    ind.Hide();

                break;
            }

            data.EnemyStacks[enemy] = current;

            if (data.Indicators.TryGetValue(enemy, out var indicator) && indicator != null)
                indicator.ShowStacking(current, required);
        }

        if (enemy != null) data.StackDecayCoroutines.Remove(enemy);
    }

    private IEnumerator AtkPoisonRoutine(
        HealthBase enemy, PlayerStats stats, ModuleRuntimeState state, StateData data)
    {
        float elapsed = 0f;
        float duration = GetEffDur(state);

        while (elapsed < duration && enemy != null)
        {
            yield return new WaitForSeconds(atkTickInterval);
            elapsed += atkTickInterval;
            if (enemy == null) break;

            enemy.TakeDamage(stats.Damage * GetEffDmg(state));
        }

        CleanupEnemy(enemy, data);
    }

    private IEnumerator HpPoisonRoutine(
        HealthBase enemy, PlayerStats stats, ModuleRuntimeState state, StateData data)
    {
        float elapsed = 0f;
        float duration = GetEffDur(state);

        while (elapsed < duration && enemy != null)
        {
            yield return new WaitForSeconds(hpTickInterval);
            elapsed += hpTickInterval;
            if (enemy == null) break;

            enemy.TakeDamage(enemy.MaxHP * GetEffHp(state));
        }
    }

    private void CleanupEnemy(HealthBase enemy, StateData data)
    {
        if (enemy == null) return;

        data.ActiveProcs.Remove(enemy);
        data.EnemyStacks.Remove(enemy);

        if (data.Indicators.TryGetValue(enemy, out var indicator) && indicator != null)
            indicator.Hide();
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;
        RefreshStateStats(state, state.buffRarity > rarity ? state.buffRarity : rarity, state.buffedLevel);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!state.isActive) return;
        RefreshStateStats(state,
            state.buffRarity > rarity ? state.buffRarity : rarity,
            state.buffedLevel > 0 ? state.buffedLevel : baselevel);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity || oldRarity > newRarity) return;
        state.buffRarity = newRarity;
        RefreshStateStats(state, state.buffRarity, state.buffedLevel > level ? state.buffedLevel : level);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);
        if (!state.isActive) return;
        RefreshStateStats(state, state.buffRarity, state.buffedLevel > level ? state.buffedLevel : level);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state) => state.totalBuffPercent += percent;
    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state) => state.totalBuffPercent -= percent;

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float d = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level) * 100f;
        float h = GetFinalStat(hpPercentPerRarity, levelMultiplier, rarity, level) * 100f;
        int s = stacksRequiredPerRarity[(int)rarity];

        string desc = $"Hits apply Poison: <color=#FFD700>{d:F0}% ATK</color> every {atkTickInterval}s.";
        if (h > 0) desc += $" & <color=#FFD700>{h:F1}% Max HP</color> every {hpTickInterval}s.";
        desc += $"\n(Requires {s} stacks. Stacks decay every {stackDecayDelay}s if not hit)";
        return desc;
    }

    public override string PassiveDescription =>
        $"Poison damage on hit stacks. Deals ATK damage every {atkTickInterval}s " +
        $"and Max HP damage every {hpTickInterval}s. Stacks decay over time.";

    public override PassiveLayout GetPassiveLayout() => PassiveLayout.TwoNarrowWide;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float bDur = GetFinalStat(durationPerRarity, levelMultiplier, rarity, level);
        float bHp = GetFinalStat(hpPercentPerRarity, levelMultiplier, rarity, level);
        float bAtk = GetFinalStat(baseStatPerRarity, levelMultiplier, rarity, level);

        float eDur = state.isActive ? GetEffDur(state) : bDur;
        float eHp = state.isActive ? GetEffHp(state) : bHp;
        float eAtk = state.isActive ? GetEffDmg(state) : bAtk;

        int stacks = stacksRequiredPerRarity[(int)rarity];

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                label         = "Duration",
                value         = $"{eDur:F1}s",
                sublabel      = $"{stacks} Stacks",
                isBuffed      = state.isActive && Math.Abs(eDur - bDur) > 0.01f,
                unbuffedValue = $"{bDur:F1}s"
            },
            new PassiveEntry
            {
                label         = "Damage",
                value         = eAtk > 0 ? $"{eAtk * 100f:F0}% Attack" : "Poison",
                sublabel      = eHp  > 0 ? $"+ {eHp * 100f:F1}% Enemy Max HP" : "Per Tick",
                isBuffed      = state.isActive && (Math.Abs(eHp - bHp) > 0.001f || Math.Abs(eAtk - bAtk) > 0.001f),
                unbuffedValue = bAtk > 0 ? $"{bAtk * 100f:F0}% Atk" : ""
            }
        };
    }
}