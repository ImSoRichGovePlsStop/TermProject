using UnityEngine;
using System;

public class HealthStationManager : MonoBehaviour
{
    public static HealthStationManager Instance { get; private set; }

    [SerializeField] private HealthStationUpgradeConfig config;

    public int CurrentLevel { get; private set; } = 0;
    public int MaxLevel => config != null ? config.levels.Length : 0;

    public event Action OnLevelChanged;

    private bool canRevive;
    private bool revived;
    private int killCount;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        RunManager.OnRoomClearedEvent        += OnRoomCleared;
        RunManager.OnEnemyKilledWithTierEvent += OnEnemyKilledWithTier;
    }

    private void OnDisable()
    {
        RunManager.OnRoomClearedEvent        -= OnRoomCleared;
        RunManager.OnEnemyKilledWithTierEvent -= OnEnemyKilledWithTier;
    }

    public HealthStationLevelData GetLevelData(int level)
    {
        if (config == null || level < 1 || level > config.levels.Length) return null;
        return config.levels[level - 1];
    }

    public MaterialRequirement[] GetUpgradeCost()
    {
        if (CurrentLevel >= MaxLevel) return null;
        return config.levels[CurrentLevel].upgradeCost;
    }

    public bool CanUpgrade()
    {
        if (CurrentLevel >= MaxLevel) return false;
        var cost = GetUpgradeCost();
        if (cost == null || cost.Length == 0) return true;
        foreach (var r in cost)
        {
            if (r.material == null) continue;
            if (!MaterialStorage.Instance.HasEnough(r.material, r.count)) return false;
        }
        return true;
    }

    /// <summary>Directly sets the upgrade level without cost. Used by the save system only.</summary>
    public void SetLevel(int level)
    {
        CurrentLevel = Mathf.Clamp(level, 0, MaxLevel);
        OnLevelChanged?.Invoke();
    }

    public bool TryUpgrade()
    {
        if (!CanUpgrade()) return false;
        var cost = GetUpgradeCost();
        if (cost != null)
            foreach (var r in cost)
            {
                if (r.material == null) continue;
                MaterialStorage.Instance.TryRemove(r.material, r.count);
            }
        CurrentLevel++;
        ApplyUpgradeEffect(CurrentLevel);
        OnLevelChanged?.Invoke();
        return true;
    }

    public void ResetRun()
    {
        var s = FindFirstObjectByType<PlayerStats>();
        if (s != null) s.OnReviveRequested -= TryRevive;

        canRevive = false;
        revived   = false;
        killCount = 0;

        if (CurrentLevel >= 2)
            s?.AddFlatModifier(new StatModifier { health = 40f });

        if (CurrentLevel >= 5)
        {
            canRevive = true;
            if (s != null) s.OnReviveRequested += TryRevive;
        }

        OnLevelChanged?.Invoke();
    }

    private void ApplyUpgradeEffect(int lv)
    {
        var s = FindFirstObjectByType<PlayerStats>();
        switch (lv)
        {
            case 2: s?.AddFlatModifier(new StatModifier { health = 40f }); break;
            case 5:
                canRevive = true;
                revived   = false;
                if (s != null) s.OnReviveRequested += TryRevive;
                break;
        }
    }

    private void OnRoomCleared()
    {
        if (CurrentLevel < 1) return;
        var s = FindFirstObjectByType<PlayerStats>();
        if (s == null || s.IsDead) return;
        float missing = s.MaxHealth - s.CurrentHealth;
        s.Heal(missing * 0.20f);
        if (CurrentLevel >= 3)
            s.AddFlatModifier(new StatModifier { health = 5f });
    }

    private void OnEnemyKilledWithTier(EnemyTier tier)
    {
        if (CurrentLevel < 4) return;
        var s = FindFirstObjectByType<PlayerStats>();
        if (s == null || s.IsDead) return;

        if (tier == EnemyTier.Miniboss)  { s.Heal(s.MaxHealth * 0.35f); return; }
        if (tier == EnemyTier.Elite)     { s.Heal(s.MaxHealth * 0.05f); return; }

        if (++killCount >= 10) { s.Heal(s.MaxHealth * 0.05f); killCount = 0; }
    }

    private bool TryRevive()
    {
        if (!canRevive || revived) return false;
        var s = FindFirstObjectByType<PlayerStats>();
        if (s == null) return false;
        revived = true;
        s.Heal(s.MaxHealth * 0.50f);
        return true;
    }
}
