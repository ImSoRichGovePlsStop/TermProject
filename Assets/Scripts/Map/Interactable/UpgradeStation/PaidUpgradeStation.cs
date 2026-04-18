using System.Collections.Generic;
using UnityEngine;

public class PaidUpgradeStation : UpgradeStation, IInteractable
{
    private int upgradeCost = 50;
    private bool _isOpen = false;

    public override string GetPromptText() => $"[ E ] Upgrade Modules ({upgradeCost} Gold)";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
        upgradeCost = RunManager.Instance.TotalBossKilled * 25 + 50;
    }

    public override void OnUpgradeClosed() => _isOpen = false;

    public override void Interact(PlayerController playerController)
    {
        if (_isOpen) return;

        var mgr = InventoryManager.Instance;
        var candidates = new List<ModuleInstance>();

        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            candidates.Add(inst);
        }

        foreach (var inst in mgr.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance) continue;
            candidates.Add(inst);
        }

        if (candidates.Count == 0)
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position,
                "Nothing to upgrade!",
                new Color(1f, 0.6f, 0.2f));
            return;
        }

        if (!CurrencyManager.Instance.TrySpend(upgradeCost))
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position,
                $"Need {upgradeCost} coins!",
                new Color(1f, 0.35f, 0.35f));
            return;
        }

        if (_uiManager == null) { Debug.LogError("[UpgradeStation] UIManager not found!"); return; }
        _isOpen = true;
        _uiManager.OpenUpgrade(this);
        upgradeCost = upgradeCost * 2;



    }
}