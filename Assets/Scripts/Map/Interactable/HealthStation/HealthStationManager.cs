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

    private void ApplyUpgradeEffect(int lv)
    {
        var s = FindFirstObjectByType<PlayerStats>();
        switch (lv)
        {
            case 2: s?.AddFlatModifier(new StatModifier { health = 25f }); break;
            case 3: s?.AddFlatModifier(new StatModifier { health = 3f });  break;
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
        s.Heal(missing * 0.10f);
    }

    private void OnEnemyKilledWithTier(EnemyTier tier)
    {
        if (CurrentLevel < 4) return;
        var s = FindFirstObjectByType<PlayerStats>();
        if (s == null || s.IsDead) return;

        if (tier == EnemyTier.Boss)                                    { s.Heal(25f); return; }
        if (tier == EnemyTier.Elite || tier == EnemyTier.Miniboss)    { s.Heal(10f); return; }

        if (++killCount >= 10) { s.Heal(5f); killCount = 0; }
    }

    private bool TryRevive()
    {
        if (!canRevive || revived) return false;
        var s = FindFirstObjectByType<PlayerStats>();
        if (s == null) return false;
        revived = true;
        s.Heal(50f);
        return true;
    }
}
