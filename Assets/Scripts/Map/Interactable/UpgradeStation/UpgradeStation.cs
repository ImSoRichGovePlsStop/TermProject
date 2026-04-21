using System.Collections.Generic;
using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    protected UIManager _uiManager;


    public virtual string GetPromptText() => "[ E ]  Upgrade Your Modules";
    public virtual InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Anvil",
        description = "Randomly pick modules from your inventory to upgrade.",
        actionText  = "Upgrade",
        cost        = null
    };

    public virtual void OnUpgradeClosed() { }

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public virtual void Interact(PlayerController playerController)
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

        if (candidates.Count == 0)
        {
            DamageNumberSpawner.Instance?.SpawnMessage(
                transform.position,
                "Nothing to upgrade!",
                new Color(1f, 0.6f, 0.2f));
            return;
        }


        if (candidates.Count > 0)
        {
            if (_uiManager == null) { Debug.LogError("[UpgradeStation] UIManager not found!"); return; }
            _uiManager.OpenUpgrade(this);
            Destroy(gameObject);
        }

    }
}