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

    public void ResetRun()
    {
        var rm = RunManager.Instance;
        if (rm == null) return;

        for (int lv = 1; lv <= CurrentLevel; lv++)
            ApplyUpgradeEffect(lv);

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
            case 3:
                rm.PermanentMods.shopDiscount    += 0.1f;
                rm.PermanentMods.upgradeDiscount += 0.1f;
                break;
            case 4:
                rm.PermanentMods.extraShopPool += 2;
                rm.PermanentMods.extraLootOptions += 1;
                break;
            case 5:
                rm.PermanentMods.lootChanceBias += 0.25f;
                break;
        }
    }
}
