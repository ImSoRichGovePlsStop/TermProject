using UnityEngine;

[System.Serializable]
public struct TestModuleEntry
{
    public ModuleData data;
    public Rarity rarity;
    public int level;
}

public class InventoryTestSpawner : MonoBehaviour
{
    [SerializeField] private TestModuleEntry[] testModules;
    [SerializeField] private InventoryUI inventoryUI;

    private void Start()
    {
        if (inventoryUI == null) { Debug.LogError("[TestSpawner] InventoryUI missing!"); return; }
        foreach (var entry in testModules)
            if (entry.data != null) inventoryUI.SpawnModule(entry.data, entry.rarity, entry.level);
    }
}