using UnityEngine;
using System;

public class LuckStationManager : MonoBehaviour
{
    public static LuckStationManager Instance { get; private set; }

    [SerializeField] private LuckStationUpgradeConfig config;

    public int CurrentLevel { get; private set; } = 0;
    public int MaxLevel => config != null ? config.levels.Length : 0;

    public event Action OnLevelChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public LuckStationLevelData GetLevelData(int level)
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

    // Called by RunManager.ResetRun() — re-applies all permanent effects to fresh RunModifiers
    public void ResetRun()
    {
        var rm = RunManager.Instance;
        if (rm == null) return;

        if (CurrentLevel >= 1) rm.AllowReroll = true;
        if (CurrentLevel >= 2) CurrencyManager.Instance?.AddCoins(100);

        for (int lv = 3; lv <= CurrentLevel; lv++)
            rm.PermanentMods.Add(GetLevelData(lv)?.modifier ?? new RunModifiers());

        OnLevelChanged?.Invoke();
    }

    private void ApplyUpgradeEffect(int lv)
    {
        var rm = RunManager.Instance;
        if (rm == null) return;

        switch (lv)
        {
            case 1: rm.AllowReroll = true; break;
            case 2: CurrencyManager.Instance?.AddCoins(100); break;
            default:
                rm.PermanentMods.Add(GetLevelData(lv)?.modifier ?? new RunModifiers());
                break;
        }
    }
}
