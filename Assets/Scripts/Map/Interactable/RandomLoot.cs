using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LootPoolEntry
{
    public TestModuleEntry module;
    public bool allowCommon = true;
    public bool allowUncommon = true;
    public bool allowRare = true;
    public bool allowEpic = true;
    public bool allowGOD = false;
}

public class RandomLoot : MonoBehaviour, IInteractable
{
    [Header("Loot Pool")]
    [SerializeField] private LootPoolEntry[] lootPool;
    [SerializeField] private int minLootCount = 1;
    [SerializeField] private int maxLootCount = 3;

    [Header("Rarity Weights")]
    [SerializeField] private float weightCommon = 50f;
    [SerializeField] private float weightUncommon = 25f;
    [SerializeField] private float weightRare = 15f;
    [SerializeField] private float weightEpic = 8f;
    [SerializeField] private float weightGOD = 2f;

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
            foreach (var (entry, rarity) in PickRandomLoot())
            {
                if (entry.module.data == null) continue;
                inventoryUI.SpawnModuleToEnv(entry.module.data, rarity);
            }
            _hasBeenOpened = true;
        }
        else
        {
            foreach (var inst in new List<ModuleInstance>(lootItems))
            {
                if (inst.CurrentGrid != null && inst.CurrentGrid != mgr.EnvGrid) continue;
                if (inst is MaterialInstance matInst)
                    inventoryUI.SpawnExistingMaterialToEnv(matInst);
                else
                    inventoryUI.SpawnExistingModuleToEnv(inst);
            }
        }
    }

    private List<(LootPoolEntry entry, Rarity rarity)> PickRandomLoot()
    {
        var result = new List<(LootPoolEntry, Rarity)>();

        if (lootPool == null || lootPool.Length == 0)
        {
            Debug.LogWarning($"[RandomLoot] Loot pool is empty on {gameObject.name}");
            return result;
        }

        int count = Mathf.Min(Random.Range(minLootCount, maxLootCount + 1), lootPool.Length);

        var shuffled = new List<LootPoolEntry>(lootPool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < count; i++)
        {
            var entry = shuffled[i];
            var allowed = GetAllowedRarities(entry);

            if (allowed.Count == 0)
            {
                Debug.LogWarning($"[RandomLoot] {entry.module.data?.name} has no rarities allowed, skipping.");
                continue;
            }

            result.Add((entry, RollRarity(allowed)));
        }

        return result;
    }

    private List<Rarity> GetAllowedRarities(LootPoolEntry entry)
    {
        var list = new List<Rarity>();
        if (entry.allowCommon) list.Add(Rarity.Common);
        if (entry.allowUncommon) list.Add(Rarity.Uncommon);
        if (entry.allowRare) list.Add(Rarity.Rare);
        if (entry.allowEpic) list.Add(Rarity.Epic);
        if (entry.allowGOD) list.Add(Rarity.GOD);
        return list;
    }

    private Rarity RollRarity(List<Rarity> allowed)
    {
        float total = 0f;
        foreach (var r in allowed)
            total += GetWeight(r);

        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var r in allowed)
        {
            cumulative += GetWeight(r);
            if (roll < cumulative)
                return r;
        }

        return allowed[allowed.Count - 1];
    }

    private float GetWeight(Rarity rarity) => rarity switch
    {
        Rarity.Common => weightCommon,
        Rarity.Uncommon => weightUncommon,
        Rarity.Rare => weightRare,
        Rarity.Epic => weightEpic,
        Rarity.GOD => weightGOD,
        _ => 0f
    };

    private void SaveRemainingLoot()
    {
        lootItems.RemoveWhere(inst =>
            inst.CurrentGrid != null &&
            inst.CurrentGrid != InventoryManager.Instance.EnvGrid);
    }
}