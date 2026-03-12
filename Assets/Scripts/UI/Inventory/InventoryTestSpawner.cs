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
        {
            if (entry.data == null) continue;
            if (entry.data is MaterialData mat)
                inventoryUI.SpawnMaterial(mat);
            else
                inventoryUI.SpawnModule(entry.data, entry.rarity, entry.level);
        }
    }
}