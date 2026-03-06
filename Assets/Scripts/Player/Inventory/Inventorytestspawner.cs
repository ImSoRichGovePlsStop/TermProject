using UnityEngine;

public class InventoryTestSpawner : MonoBehaviour
{
    [SerializeField] private ModuleData[] testModules;
    [SerializeField] private InventoryUI  inventoryUI;

    private void Start()
    {
        if (inventoryUI == null) { Debug.LogError("[TestSpawner] InventoryUI missing!"); return; }
        foreach (var m in testModules)
            if (m != null) inventoryUI.SpawnModule(m);
    }
}