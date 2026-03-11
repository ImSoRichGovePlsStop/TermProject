using System.Collections;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI  moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    private void Awake()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) { Debug.LogError("[InventoryUI] InventoryManager missing!"); return; }

        weaponGridUI.Init(mgr.WeaponGrid);
        bagGridUI.Init(mgr.BagGrid);

        Debug.Log("[InventoryUI] Initialized.");
    }

    public ModuleItemUI SpawnModule(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        var inst = new ModuleInstance(data, rarity, level);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI);

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public MaterialItemUI SpawnMaterial(MaterialData data)
    {
        foreach (var existing in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (existing is MaterialInstance existingMat
                && existingMat.MaterialData == data
                && existingMat.StackCount < existingMat.MaxStack)
            {
                existingMat.AddStack();
                return existingMat.UIElement as MaterialItemUI;
            }
        }

        var inst = new MaterialInstance(data);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var go = Instantiate(materialItemPrefab, transform);
        var ui = go.GetComponent<MaterialItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI);

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    private IEnumerator SnapNextFrame(MaterialItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }
}