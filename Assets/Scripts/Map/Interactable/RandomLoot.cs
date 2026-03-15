using System.Collections.Generic;
using UnityEngine;

public class RandomLoot : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    [SerializeField] private int minLootCount = 1;
    [SerializeField] private int maxLootCount = 3;

    [Header("Rarity Distribution")]
    [SerializeField] private float meanRarity = 1f;  // 0=Common 1=Uncommon 2=Rare 3=Epic 4=GOD
    [SerializeField] private float sd = 0.8f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIManager uiManager;

    private bool _hasBeenOpened = false;
    private readonly HashSet<ModuleInstance> lootItems = new HashSet<ModuleInstance>();
    private static RandomLoot _activeBox;

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
        if (_activeBox == this)
            lootItems.Add(inst);
    }

    public void Interact(PlayerController playerController)
    {
        if (_activeBox != null && _activeBox != this)
            _activeBox.SaveRemainingLoot();

        _activeBox = this;

        var mgr = InventoryManager.Instance;
        if (!uiManager.IsInventoryOpen)
            uiManager.ToggleInventory();

        foreach (var inst in new List<ModuleInstance>(mgr.EnvGrid.GetAllModules()))
        {
            if (inst.UIElement != null)
                Object.Destroy(inst.UIElement.gameObject);
            mgr.EnvGrid.Remove(inst);
        }

        if (!_hasBeenOpened)
        {
            var rolled = Randomizer.Roll(minLootCount, maxLootCount, meanRarity, sd);

            if (debugLog)
                Debug.Log($"[RandomLoot] {gameObject.name} — spawning {rolled.Count} item(s) | mean={meanRarity} sd={sd}");

            foreach (var entry in rolled)
            {
                if (entry.data == null) continue;

                if (debugLog)
                    Debug.Log($"[RandomLoot]  → {entry.data.name} | rarity={entry.rarity}");

                inventoryUI.SpawnModuleToEnv(entry.data, entry.rarity);
            }
            _hasBeenOpened = true;
        }
        else
        {
            if (debugLog)
                Debug.Log($"[RandomLoot] {gameObject.name} — restoring {lootItems.Count} saved item(s)");

            foreach (var inst in new List<ModuleInstance>(lootItems))
            {
                if (inst.CurrentGrid != null && inst.CurrentGrid != mgr.EnvGrid) continue;

                if (debugLog)
                    Debug.Log($"[RandomLoot]  → restoring {inst}");

                if (inst is MaterialInstance matInst)
                    inventoryUI.SpawnExistingMaterialToEnv(matInst);
                else
                    inventoryUI.SpawnExistingModuleToEnv(inst);
            }
        }
    }

    private void SaveRemainingLoot()
    {
        lootItems.RemoveWhere(inst =>
            inst.CurrentGrid != null &&
            inst.CurrentGrid != InventoryManager.Instance.EnvGrid);
    }
}