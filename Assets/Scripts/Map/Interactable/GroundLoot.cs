using System.Collections.Generic;
using UnityEngine;

public class GroundLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private TestModuleEntry[] lootModules;
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIManager uiManager;

    private bool _hasBeenOpened = false;

    public void Interact(PlayerController playerController)
    {
        var mgr = InventoryManager.Instance;

        if (!uiManager.IsInventoryOpen)
            uiManager.ToggleInventory();

       
        foreach (var inst in new List<ModuleInstance>(mgr.EnvGrid.GetAllModules()))
            mgr.EnvGrid.Remove(inst);

        if (!_hasBeenOpened)
        {
            foreach (var entry in lootModules)
            {
                if (entry.data == null) continue;
                inventoryUI.SpawnModuleToEnv(entry.data, entry.rarity, entry.level);
            }
            _hasBeenOpened = true;
        }
    }
}