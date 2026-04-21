using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/SelfDetonation")]
public class SelfDetonationModule : ModuleEffect
{
    [Header("Attack% scale per Rarity (Uncommon → Legendary)  [X]")]
    public float[] baseAttackPercentPerRarity = { 0f, 0f, 0.40f, 0.60f, 0.80f };

    [Header("Max-HP% damage per Rarity (Uncommon → Legendary)  [Y]")]
    public float[] baseHpPercentPerRarity = { 0f, 0f, 0.10f, 0.15f, 0.20f };

    [Header("Level multiplier applied to both components")]
    public float levelMultiplier = 0.05f;

    [Header("Core Settings")]
    public float activationHpCostPercent = 0.10f;
    public float healBackPercent = 0.10f;
    public float countdownDuration = 5f;
    public float moduleCooldown = 10f;
    public float minHpBuffer = 1f;

    [Header("Burst AoE radius (world units)")]
    public float burstRadius = 5f;

    [Tooltip("Spawned on the player during the countdown (parented, destroyed on detonate).")]
    public GameObject countdownVFXPrefab;

    [Tooltip("Spawned at the player position when the burst fires.")]
    public GameObject burstVFXPrefab;

    [Tooltip("World-space UI prefab with SelfDetonationIndicator component.")]
    public GameObject indicatorPrefab;

    private float GetAttackPercent(Rarity rarity, int level)
        => GetFinalStat(baseAttackPercentPerRarity, levelMultiplier, rarity, level);

    private float GetHpPercent(Rarity rarity, int level)
        => GetFinalStat(baseHpPercentPerRarity, levelMultiplier, rarity, level);

    private void ApplyToPassive(PlayerStats stats, ModuleRuntimeState state)
    {
        var passive = stats.GetComponentInChildren<SelfDetonationPassive>();
        if (passive == null) return;
        passive.attackPercent = state.attackPercent * (1f + state.totalBuffPercent);
        passive.hpPercent = state.hpPercent * (1f + state.totalBuffPercent);
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.attackPercent = GetAttackPercent(rarity, level);
        state.hpPercent = GetHpPercent(rarity, level);
        state.currentStat = state.attackPercent;

        var passive = GetOrCreatePassive(stats);
        if (passive == null) return;

        passive.SelfDetonationModule = this;
        passive.burstRadius = burstRadius;
        passive.attackPercent = state.attackPercent * (1f + state.totalBuffPercent);
        passive.hpPercent = state.hpPercent * (1f + state.totalBuffPercent);
        passive.enabled = true;

        passive.Init(stats, stats.GetComponent<PlayerCombatContext>(), LayerMask.GetMask("Enemy"));
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        var passive = stats.GetComponentInChildren<SelfDetonationPassive>();
        if (passive != null)
            passive.enabled = false;
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        if (state.buffRarity > rarity)
        {
            state.attackPercent = GetAttackPercent(state.buffRarity, state.buffedLevel);
            state.hpPercent = GetHpPercent(state.buffRarity, state.buffedLevel);
        }
        else
        {
            state.attackPercent = GetAttackPercent(rarity, state.buffedLevel);
            state.hpPercent = GetHpPercent(rarity, state.buffedLevel);
        }
        state.currentStat = state.attackPercent;
        ApplyToPassive(stats, state);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : baselevel;
        if (state.buffRarity > rarity)
        {
            state.attackPercent = GetAttackPercent(state.buffRarity, effectiveLevel);
            state.hpPercent = GetHpPercent(state.buffRarity, effectiveLevel);
        }
        else
        {
            state.attackPercent = GetAttackPercent(rarity, effectiveLevel);
            state.hpPercent = GetHpPercent(rarity, effectiveLevel);
        }
        state.currentStat = state.attackPercent;
        ApplyToPassive(stats, state);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity || oldRarity > newRarity) return;

        state.buffRarity = newRarity;
        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        state.attackPercent = GetAttackPercent(state.buffRarity, effectiveLevel);
        state.hpPercent = GetHpPercent(state.buffRarity, effectiveLevel);
        state.currentStat = state.attackPercent;
        ApplyToPassive(stats, state);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        state.attackPercent = GetAttackPercent(state.buffRarity, effectiveLevel);
        state.hpPercent = GetHpPercent(state.buffRarity, effectiveLevel);
        state.currentStat = state.attackPercent;
        ApplyToPassive(stats, state);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent += percent;
        ApplyToPassive(stats, state);
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        if (!state.isActive)
        {
            state.totalBuffPercent -= percent;
            return;
        }
        state.totalBuffPercent -= percent;
        ApplyToPassive(stats, state);
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state) => null;

    public override string PassiveDescription =>
        $"Press [Q] to consume {activationHpCostPercent * 100f:F0}% Max HP. " +
        $"After {countdownDuration:F0}s, deal damage to all enemies around player then return the consumed HP. " +
        $"(Cooldown: {moduleCooldown:F0}s)";

    public override PassiveLayout GetPassiveLayout() => PassiveLayout.TwoEqual;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseAtk = GetAttackPercent(rarity, level);
        float effectiveAtk = state.isActive ? state.attackPercent * (1f + state.totalBuffPercent) : baseAtk;
        bool atkBuffed = state.isActive && effectiveAtk != baseAtk;

        float baseHp = GetHpPercent(rarity, level);
        float effectiveHp = state.isActive ? state.hpPercent * (1f + state.totalBuffPercent) : baseHp;
        bool hpBuffed = state.isActive && effectiveHp != baseHp;

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"{effectiveAtk * 100f:F0}%",
                label         = "ATK Damage",
                sublabel      = "On Detonate",
                isBuffed      = atkBuffed,
                unbuffedValue = $"+{baseAtk * 100f:F0}%"
            },
            new PassiveEntry
            {
                value         = $"{effectiveHp * 100f:F0}%",
                label         = "Max HP Damage",
                sublabel      = "On Detonate",
                isBuffed      = hpBuffed,
                unbuffedValue = $"+{baseHp * 100f:F0}%"
            }
        };
    }

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
        => BuildBaseModuleStat(baseAttackPercentPerRarity, levelMultiplier, rarity, level, state);

    private static SelfDetonationPassive GetOrCreatePassive(PlayerStats stats)
    {
        if (stats == null) return null;
        var existing = stats.GetComponentInChildren<SelfDetonationPassive>();
        return existing != null ? existing : stats.gameObject.AddComponent<SelfDetonationPassive>();
    }
}