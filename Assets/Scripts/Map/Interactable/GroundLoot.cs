using System.Collections.Generic;
using UnityEngine;

public class GroundLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private TestModuleEntry[] lootModules;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIManager uiManager;

    private bool _hasBeenOpened = false;
    private readonly HashSet<ModuleInstance> lootItems = new HashSet<ModuleInstance>();

    private static GroundLoot IsActiveBox;

    public string GetPromptText() => "[ E ]  Open Loot";

    private void Start()
    {
        InventoryManager.Instance.EnvGrid.OnModulePlaced += OnEnvModulePlaced;
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.EnvGrid.OnModulePlaced -= OnEnvModulePlaced;
    }

    private void OnEnvModulePlaced(ModuleInstance inst)
    {
        if (IsActiveBox == this)
            lootItems.Add(inst);
        else
            lootItems.Remove(inst);
    }

    public void Interact(PlayerController playerController)
    {
        IsActiveBox = this;

        var mgr = InventoryManager.Instance;

        if (!uiManager.IsInventoryOpen)
            uiManager.ToggleInventory();

        foreach (var inst in new List<ModuleInstance>(mgr.EnvGrid.GetAllModules()))
        {
            if (inst.UIElement != null)
                Object.Destroy(inst.UIElement.gameObject);
            mgr.EnvGrid.Remove(inst);
        }

        inventoryUI.SetEnvGridVisible(true);

        if (!_hasBeenOpened)
        {
            foreach (var entry in lootModules)
            {
                if (entry.data == null) continue;
                inventoryUI.SpawnModuleToEnv(entry.data, entry.rarity, entry.level);
            }
            _hasBeenOpened = true;
        }
        else
        {
            foreach (var inst in lootItems)
            {
                if (inst.CurrentGrid != null) continue;
                if (inst is MaterialInstance matInst)
                    inventoryUI.SpawnExistingMaterialToEnv(matInst);
                else
                    inventoryUI.SpawnExistingModuleToEnv(inst);
            }
        }
    }
}

// What have i done?
// use hashset to track the modules that are currently in the box
// add field to track the active box, so we can check if it's placed in the active box or not
// reset active box when interact with another box
// add spawnmodule that uses materialItemPrefab
// add spawnmodule that uses moduleItemPrefab