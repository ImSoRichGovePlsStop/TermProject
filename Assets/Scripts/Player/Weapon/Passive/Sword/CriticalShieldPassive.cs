using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CriticalShieldPassive : MonoBehaviour
{
    [Header("UI Icons")]
    public Sprite iconShield;
    private StatusEntry aegisEntry;

    public bool keenEdge = false;
    public bool persistence = false;
    public bool fortifiedStrike = false;
    public bool fortify = false;
    public bool shatterBurst = false;
    public bool absorb = false;
    public bool temperedSoul = false;

    private const float BaseShieldValue = 20f;
    private const float FortifyShieldValue = 35f;
    private const float BaseShieldDuration = 3f;
    private const float FortifyDuration = 5f;
    private const float ShieldCooldown = 3f;

    // L2 - L6 Constants
    private const float KeenEdgeCritDmgBonus = 0.1f;
    private const float FortifiedExtraDuration = 1f;
    private const float FortifiedExtraValue = 8f;
    private const int FortifiedMaxTimes = 3;
    private const float ShatterBurstRadius = 3f;
    private const float ShatterBurstDamagePercent = 0.5f;
    private const float AbsorbHealPercent = 0.10f;
    private const float TemperedKillShieldPercent = 0.10f;
    private const int MaxMilestones = 5;
    private const int TemperStacksPerBreak = 1;
    private const float TemperShieldPerForty = 40f;
    private const float TemperShieldValuePer = 2f;
    private const float TemperScalingPer = 0.0025f;
    private const float TemperCritDmgPer = 0.01f;
    private const int TemperMilestonePer = 20;
    private const float TemperCritChanceMilestone = 0.05f;
    private const float TemperMaxHpMilestone = 0.08f;

    private PlayerStats stats;
    private PlayerCombatContext context;

    private bool onCooldown = false;
    private PlayerStats.ShieldInstance aegisShieldInstance = null;
    private int fortifiedTimesThisInstance = 0;
    private float currentCooldownTimer = 0f;

    // Modifiers
    private StatModifier keenEdgeModifier = new StatModifier();
    private bool keenEdgeActive = false;
    private int consecutiveNonCritSwings = 0;
    private int temperStacks = 0;
    private StatModifier temperPerStackModifier = new StatModifier();
    private StatModifier temperMilestoneFlatModifier = new StatModifier();
    private StatModifier temperMilestoneMultModifier = new StatModifier();

    private void Update()
    {
        UpdateHUD();
    }

    public void RegisterHUD()
    {
        aegisEntry = new StatusEntry("aegis", iconShield);
        PlayerStatusHUD.Instance.Register(aegisEntry);
    }

    private void UpdateHUD()
    {
        if (aegisEntry == null) return;

        aegisEntry.outerBorderType = (aegisShieldInstance != null)
            ? StatusBorderType.Gold : StatusBorderType.Default;

        if (aegisShieldInstance != null)
        {
            float totalDuration = GetShieldDuration();
            float remaining = aegisShieldInstance.expiresAt - Time.time;
            aegisEntry.sweepFill = Mathf.Clamp01(1f - (remaining / totalDuration));

            aegisEntry.count = Mathf.CeilToInt(aegisShieldInstance.value);
            aegisEntry.isActive = true;
        }
        else if (onCooldown)
        {
            aegisEntry.sweepFill = Mathf.Clamp01(currentCooldownTimer / ShieldCooldown);

            aegisEntry.count = 0;
            aegisEntry.isActive = false;
            aegisEntry.sweepClockwise = false;
        }
        else
        {
            aegisEntry.sweepFill = 0f;
            aegisEntry.count = 0;
            aegisEntry.isActive = false;
            aegisEntry.sweepClockwise = true;
        }

        PlayerStatusHUD.Instance.Refresh("aegis");
    }

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        stats = playerStats;
        context = combatContext;

        context.OnCritHit += OnCritHit;
        context.OnAttack += OnSwing;
        context.OnEntityKilled += OnEnemyKilled;
        stats.OnShieldInstanceLost += OnShieldLost;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.OnCritHit -= OnCritHit;
            context.OnAttack -= OnSwing;
            context.OnEntityKilled -= OnEnemyKilled;
        }
        if (stats != null)
            stats.OnShieldInstanceLost -= OnShieldLost;

        ClearAllModifiers();
    }

    public void OnFlagsChanged()
    {
        RefreshKeenEdge();
    }

    private float GetShieldValue()
    {
        float baseVal = fortify ? FortifyShieldValue : BaseShieldValue;
        float hpBonus = fortify ? stats.MaxHealth * 0.05f : 0f;
        float temperBonus = temperStacks * TemperShieldValuePer;
        float scalingBonus = temperStacks * TemperScalingPer * stats.MaxHealth;
        return baseVal + hpBonus + temperBonus + scalingBonus;
    }

    private float GetShieldDuration()
    {
        return fortify ? FortifyDuration : BaseShieldDuration;
    }

    private void OnCritHit()
    {
        if (!enabled) return;
        if (onCooldown) return;
        if (context.LastHitEnemies.All(e => e == null || e.IsInvincible)) return;

        if (fortifiedStrike && aegisShieldInstance != null)
        {
            TryFortifiedStrike();
            return;
        }

        if (aegisShieldInstance != null) return;

        SpawnShield();
    }

    private void SpawnShield()
    {
        float value = GetShieldValue();
        float duration = GetShieldDuration();

        aegisShieldInstance = stats.GainShield(value, duration);
        fortifiedTimesThisInstance = 0;
        RefreshKeenEdge();
    }

    private void TryFortifiedStrike()
    {
        if (fortifiedTimesThisInstance >= FortifiedMaxTimes) return;
        stats.ExtendShield(aegisShieldInstance, FortifiedExtraDuration, FortifiedExtraValue);
        fortifiedTimesThisInstance++;
    }

    private void OnSwing()
    {
        if (!enabled || !persistence || context.LastHitEnemies.Count == 0) return;
        if (context.LastHitEnemies.All(e => e == null || e.IsInvincible)) return;
        if (stats.LastHitWasCrit || onCooldown || aegisShieldInstance != null)
        {
            consecutiveNonCritSwings = 0;
            return;
        }

        consecutiveNonCritSwings++;
        if (consecutiveNonCritSwings >= 6)
        {
            consecutiveNonCritSwings = 0;
            SpawnShield();
        }
    }

    private void OnShieldLost(PlayerStats.ShieldInstance instance, float remainingValue, bool wasTimedOut)
    {
        if (instance != aegisShieldInstance) return;
        aegisShieldInstance = null;

        if (shatterBurst)
        {
            float maxShieldValue = GetShieldValue() + fortifiedTimesThisInstance * FortifiedExtraValue;
            float aoeValue = wasTimedOut ? remainingValue * ShatterBurstDamagePercent : maxShieldValue * ShatterBurstDamagePercent;
            TriggerShatterBurst(aoeValue);
        }

        fortifiedTimesThisInstance = 0;
        RefreshKeenEdge();

        if (absorb && wasTimedOut) stats.Heal(remainingValue * AbsorbHealPercent);

        if (temperedSoul)
        {
            int gained = wasTimedOut ? Mathf.FloorToInt(remainingValue / TemperShieldPerForty) : TemperStacksPerBreak;
            AddTemperStacks(gained);
        }

        StartCoroutine(ShieldCooldownCoroutine());
    }

    private IEnumerator ShieldCooldownCoroutine()
    {
        onCooldown = true;
        currentCooldownTimer = ShieldCooldown;
        while (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            yield return null;
        }
        currentCooldownTimer = 0f;
        onCooldown = false;
    }

    private void TriggerShatterBurst(float aoeValue)
    {
        context.RefreshEnemiesAround(ShatterBurstRadius);
        foreach (var enemy in context.EnemiesAround)
        {
            if (enemy == null || enemy.IsDead) continue;
            enemy.TakeDamage(aoeValue);
        }
        context.NotifySecondaryAttackForced(transform.position);
    }

    private void RefreshKeenEdge()
    {
        bool shouldBeActive = keenEdge && aegisShieldInstance != null;
        if (shouldBeActive && !keenEdgeActive)
        {
            keenEdgeModifier.critDamage = KeenEdgeCritDmgBonus;
            stats.AddMultiplierModifier(keenEdgeModifier);
            keenEdgeActive = true;
        }
        else if (!shouldBeActive && keenEdgeActive)
        {
            stats.RemoveMultiplierModifier(keenEdgeModifier);
            keenEdgeModifier.critDamage = 0f;
            keenEdgeActive = false;
        }
    }

    private void OnEnemyKilled(HealthBase enemy)
    {
        if (!enabled || !temperedSoul || aegisShieldInstance == null) return;
        stats.ExtendShield(aegisShieldInstance, 0f, enemy.MaxHP * TemperedKillShieldPercent);
    }

    private void AddTemperStacks(int amount)
    {
        if (amount <= 0) return;
        int prevMilestones = Mathf.Min(temperStacks / TemperMilestonePer, MaxMilestones);
        temperStacks += amount;
        int newMilestones = Mathf.Min(temperStacks / TemperMilestonePer, MaxMilestones);

        stats.RemoveFlatModifier(temperPerStackModifier);
        temperPerStackModifier.critDamage = temperStacks * TemperCritDmgPer;
        stats.AddFlatModifier(temperPerStackModifier);

        if (newMilestones > prevMilestones)
        {
            stats.RemoveFlatModifier(temperMilestoneFlatModifier);
            stats.RemoveMultiplierModifier(temperMilestoneMultModifier);
            temperMilestoneFlatModifier.critChance = newMilestones * TemperCritChanceMilestone;
            temperMilestoneMultModifier.health = newMilestones * TemperMaxHpMilestone;
            stats.AddFlatModifier(temperMilestoneFlatModifier);
            stats.AddMultiplierModifier(temperMilestoneMultModifier);
        }
    }

    public void ForceClean()
    {
        StopAllCoroutines();
        onCooldown = false;
        aegisShieldInstance = null;
        consecutiveNonCritSwings = 0;
        ClearAllModifiers();
    }

    private void ClearAllModifiers()
    {
        if (stats == null) return;
        if (keenEdgeActive)
        {
            stats.RemoveFlatModifier(keenEdgeModifier);
            keenEdgeActive = false;
        }
        stats.RemoveFlatModifier(temperPerStackModifier);
        stats.RemoveFlatModifier(temperMilestoneFlatModifier);
        stats.RemoveMultiplierModifier(temperMilestoneMultModifier);
    }
}