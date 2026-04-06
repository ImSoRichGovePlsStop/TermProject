using System.Collections.Generic;
using UnityEngine;

public class PaidUpgradeStation : UpgradeStation, IInteractable
{
    
    private int upgradeCost = 50;

    public override string GetPromptText() => $"[ E ] Upgrade Modules ({upgradeCost} Gold)";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
        upgradeCost = RunManager.Instance.TotalBossKilled*25 + 50 ;
    }

    public override void Interact(PlayerController playerController)
    {

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

        if (candidates.Count > 0)
        {
            if (CurrencyManager.Instance.TrySpend(upgradeCost))
            {
                if (_uiManager == null) { Debug.LogError("[UpgradeStation] UIManager not found!"); return; }
                _uiManager.OpenUpgrade(this);
                upgradeCost = upgradeCost * 2;
            }
        }



    }
}