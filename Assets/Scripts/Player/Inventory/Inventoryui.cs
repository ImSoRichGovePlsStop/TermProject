using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;

    private readonly List<ModuleItemUI> _items = new();

    private void Start()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) { Debug.LogError("[InventoryUI] InventoryManager missing!"); return; }

        weaponGridUI.Init(mgr.WeaponGrid);
        bagGridUI.Init(mgr.BagGrid);

        Debug.Log("[InventoryUI] Initialized.");
    }

    public ModuleItemUI SpawnModule(ModuleData data)
    {
        var inst = new ModuleInstance(data);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.Init(inst, weaponGridUI, bagGridUI);

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        _items.Add(ui);
        return ui;
    }

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
    }
}