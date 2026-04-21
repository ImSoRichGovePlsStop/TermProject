using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/SoulSiphon")]
public class SoulSiphonModule : ModuleEffect
{
    [Header("ATK buff per enemy siphoned per Rarity (Common → Legendary)  [X]")]
    public float[] baseAttackBuffPerEnemyPerRarity = { 0f, 0f, 0.50f, 0.50f, 0.50f };

    [Header("Level multiplier applied to ATK buff per enemy")]
    public float levelMultiplier = 0.05f;

    [Header("Siphon AoE radius per Rarity (Common → Legendary)  [world units]")]
    public float[] burstRangePerRarity = { 0f, 0f, 6f, 8f, 10f };

    [Header("ATK buff duration per Rarity (Common → Legendary)  [seconds]")]
    public float[] buffDurationPerRarity = { 0f, 0f, 5f, 7f, 9f };

    [Header("HP drained from each enemy (% of enemy current HP, fixed)")]
    public float enemyHpDrainPercent = 0.10f;

    [Header("Core Settings")]
    public float moduleCooldown = 15f;

    [Tooltip("Spawned at the player position when siphon fires (outward pulse ring).")]
    public GameObject siphonVFXPrefab;

    [Tooltip("Parented to the player for the buff duration — shows radial clock + enemy count.")]
    public GameObject buffIndicatorPrefab;

    private float GetAttackBuffPerEnemy(Rarity rarity, int level)
        => GetFinalStat(baseAttackBuffPerEnemyPerRarity, levelMultiplier, rarity, level);

    private float GetRange(Rarity rarity)
    {
        int i = Mathf.Clamp((int)rarity, 0, burstRangePerRarity.Length - 1);
        return burstRangePerRarity[i];
    }

    private float GetDuration(Rarity rarity)
    {
        int i = Mathf.Clamp((int)rarity, 0, buffDurationPerRarity.Length - 1);
        return buffDurationPerRarity[i];
    }

    private void ApplyToPassive(PlayerStats stats, ModuleRuntimeState state)
    {
        var passive = stats.GetComponentInChildren<SoulSiphonPassive>();
        if (passive == null) return;

        passive.attackBuffPerEnemy = GetEffectiveStat(state);
        passive.burstRange = state.burstRange;
        passive.buffDuration = state.duration;
    }

    protected override void OnEquip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        state.currentStat = GetAttackBuffPerEnemy(rarity, level);
        state.burstRange = GetRange(rarity);
        state.duration = GetDuration(rarity);

        var passive = GetOrCreatePassive(stats);
        if (passive == null) return;

        passive.Module = this;
        passive.attackBuffPerEnemy = GetEffectiveStat(state);
        passive.burstRange = state.burstRange;
        passive.buffDuration = state.duration;
        passive.enabled = true;

        passive.Init(stats, LayerMask.GetMask("Enemy"));
    }

    protected override void OnUnequip(PlayerStats stats, Rarity rarity, int level, ModuleRuntimeState state)
    {
        var passive = stats.GetComponentInChildren<SoulSiphonPassive>();
        if (passive != null)
            passive.enabled = false;
    }

    public override void OnLevelBuffReceived(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        if (state.buffedLevel == 0) state.buffedLevel = baselevel;
        state.buffedLevel += levelBonus;

        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetAttackBuffPerEnemy(effectiveRarity, state.buffedLevel);
        ApplyToPassive(stats, state);
    }

    public override void OnLevelBuffRemoved(int baselevel, int levelBonus, Rarity rarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.buffedLevel -= levelBonus;
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : baselevel;
        Rarity effectiveRarity = state.buffRarity > rarity ? state.buffRarity : rarity;
        state.currentStat = GetAttackBuffPerEnemy(effectiveRarity, effectiveLevel);
        ApplyToPassive(stats, state);
    }

    public override void OnRarityBuffReceived(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]++;
        if (state.buffRarity > newRarity || oldRarity > newRarity) return;

        state.buffRarity = newRarity;

        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        state.currentStat = GetAttackBuffPerEnemy(state.buffRarity, effectiveLevel);
        state.burstRange = GetRange(state.buffRarity);
        state.duration = GetDuration(state.buffRarity);
        ApplyToPassive(stats, state);
    }

    public override void OnRarityBuffRemoved(int level, Rarity oldRarity, Rarity newRarity, PlayerStats stats, ModuleRuntimeState state)
    {
        state.baseRarity[(int)newRarity]--;
        FindNextRarity(oldRarity, state);
        if (!state.isActive) return;

        int effectiveLevel = state.buffedLevel > 0 ? state.buffedLevel : level;
        state.currentStat = GetAttackBuffPerEnemy(state.buffRarity, effectiveLevel);
        state.burstRange = GetRange(state.buffRarity);
        state.duration = GetDuration(state.buffRarity);
        ApplyToPassive(stats, state);
    }

    public override void OnBuffReceived(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent += percent;
        ApplyToPassive(stats, state);
    }

    public override void OnBuffRemoved(float percent, PlayerStats stats, ModuleRuntimeState state)
    {
        state.totalBuffPercent -= percent;
        if (!state.isActive) return;
        ApplyToPassive(stats, state);
    }

    public override string GetDescription(Rarity rarity, int level, ModuleRuntimeState state) => null;

    public override string PassiveDescription =>
        $"Press [Q] to siphon {enemyHpDrainPercent * 100f:F0}% current HP from every enemy in range. " +
        $"Each enemy siphoned grants a temporary ATK boost. " +
        $"(Cooldown: {moduleCooldown:F0}s)";

    public override PassiveLayout GetPassiveLayout() => PassiveLayout.TwoEqual;

    public override PassiveEntry[] GetPassiveEntries(Rarity rarity, int level, ModuleRuntimeState state)
    {
        float baseBuffPerEnemy = GetAttackBuffPerEnemy(rarity, level);
        float effectiveBuffPerEnemy = state.isActive ? GetEffectiveStat(state) : baseBuffPerEnemy;
        bool atkBuffed = state.isActive && !Mathf.Approximately(effectiveBuffPerEnemy, baseBuffPerEnemy);

        float baseDuration = GetDuration(rarity);
        float effectiveDuration = state.isActive ? state.duration : baseDuration;
        bool durationBuffed = state.isActive && !Mathf.Approximately(effectiveDuration, baseDuration);

        return new PassiveEntry[]
        {
            new PassiveEntry
            {
                value         = $"+{effectiveBuffPerEnemy * 100f:F0}%",
                label         = "Per Enemy Siphoned",
                sublabel      = "Conditional",
                isBuffed      = atkBuffed,
                unbuffedValue = $"+{baseBuffPerEnemy * 100f:F0}%"
            },
            new PassiveEntry
            {
                value         = $"{effectiveDuration:F0}s",
                label         = "Buff Duration",
                isBuffed      = durationBuffed,
                unbuffedValue = $"{baseDuration:F0}s"
            }
        };
    }

    public override (float unbuffed, float buffed) GetBaseModuleStat(Rarity rarity, int level, ModuleRuntimeState state)
        => BuildBaseModuleStat(baseAttackBuffPerEnemyPerRarity, levelMultiplier, rarity, level, state);

    private static SoulSiphonPassive GetOrCreatePassive(PlayerStats stats)
    {
        if (stats == null) return null;
        var existing = stats.GetComponentInChildren<SoulSiphonPassive>();
        return existing != null ? existing : stats.gameObject.AddComponent<SoulSiphonPassive>();
    }
}