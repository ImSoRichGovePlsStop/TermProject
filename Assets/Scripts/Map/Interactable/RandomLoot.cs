using System.Collections.Generic;
using UnityEngine;


public class RandomLoot : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    [SerializeField] private int minLootCount = 1;
    [SerializeField] private int maxLootCount = 3;
    [SerializeField] private bool allowDuplicates = false;

    [Header("Cost Distribution")]
    [SerializeField] private float meanCost = 10f;
    [SerializeField] private float sd = 10f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private UIManager uiManager;

    private bool _hasBeenOpened = false;
    private readonly HashSet<ModuleInstance> lootItems = new HashSet<ModuleInstance>();
    private static RandomLoot _activeBox;

    public string GetPromptText() => "[ E ]  Open Loot";

    public void Configure(int floor, int roomsCleared)
    {
        minLootCount = 1;
        maxLootCount = 2;

        float floorBase = (floor ) * 30f;
        float roomBonus = roomsCleared * 5f;
        meanCost = 50f + floorBase + roomBonus + 10f;

        sd = 20f + (floor - 1) * 3f;
    }

    private void Start()
    {
        InventoryManager.Instance.EnvGrid.OnModulePlaced += OnEnvModulePlaced;
    }



    private void Update()
    {
        if (inventoryUI == null)
            inventoryUI = Object.FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (uiManager == null)
            uiManager = Object.FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
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

        inventoryUI.SetEnvGridVisible(true);

        if (!_hasBeenOpened)
        {
            var rolled = Randomizer.Roll(minLootCount, maxLootCount, meanCost, sd, allowDuplicates);

            if (debugLog)
                Debug.Log($"[RandomLoot] {gameObject.name} — spawning {rolled.Count} item(s) | mean={meanCost} sd={sd}");

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

        Destroy(gameObject);
    }

    private void SaveRemainingLoot()
    {
        lootItems.RemoveWhere(inst =>
            inst.CurrentGrid != null &&
            inst.CurrentGrid != InventoryManager.Instance.EnvGrid);
    }
}