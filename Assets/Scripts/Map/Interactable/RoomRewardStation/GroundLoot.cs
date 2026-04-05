using System.Collections.Generic;
using UnityEngine;

public class GroundLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private TestModuleEntry[] lootModules;
    [SerializeField] private InventoryUI       inventoryUI;
    [SerializeField] private UIManager         uiManager;

    private bool _hasBeenOpened = false;
    private readonly List<ModuleInstance> _savedLoot = new List<ModuleInstance>();

    public string GetPromptText() => "[ E ]  Open Loot";

    public void Interact(PlayerController playerController)
    {
        if (!uiManager.IsInventoryOpen)
            uiManager.ToggleInventory();

        if (!_hasBeenOpened)
        {
            var overflow = new List<ModuleInstance>();

            foreach (var entry in lootModules)
            {
                if (entry.data == null) continue;

                ModuleInstance inst = entry.data is MaterialData mat
                    ? (ModuleInstance)new MaterialInstance(mat)
                    : new ModuleInstance(entry.data, entry.rarity, entry.level);

                if (!InventoryManager.Instance.TryAddToBag(inst))
                    overflow.Add(inst);
                else
                    SpawnToBag(inst);

                _savedLoot.Add(inst);
            }

            if (overflow.Count > 0)
                DiscardGridUI.Instance?.ShowForOverflow(overflow);

            _hasBeenOpened = true;
        }
        else
        {
            var overflow = new List<ModuleInstance>();

            foreach (var inst in _savedLoot)
            {
                if (inst.CurrentGrid == InventoryManager.Instance.BagGrid) continue;
                if (inst.CurrentGrid != null) continue;

                if (!InventoryManager.Instance.TryAddToBag(inst))
                    overflow.Add(inst);
                else
                    SpawnToBag(inst);
            }

            if (overflow.Count > 0)
                DiscardGridUI.Instance?.ShowForOverflow(overflow);
        }
    }

    private void SpawnToBag(ModuleInstance inst)
    {
        if (inst.UIElement != null) return;

        if (inst is MaterialInstance matInst)
            inventoryUI.SpawnMaterial(matInst.MaterialData);
        else
            inventoryUI.SpawnModule(inst.Data, inst.Rarity, inst.Level);
    }
}
