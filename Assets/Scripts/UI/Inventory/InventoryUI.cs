using System.Collections;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;
    [SerializeField] private GridUI envGridUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI  moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    private void Awake()
    {
        var mgr    = InventoryManager.Instance;
        if (mgr == null) { return; }

        var layout = GetComponentInParent<InventoryLayout>();
        float cellSize    = layout != null ? layout.CellSize    : 64f;
        float cellSpacing = layout != null ? layout.CellSpacing : 2f;

        weaponGridUI.Init(mgr.WeaponGrid, cellSize, cellSpacing);
        bagGridUI.Init(mgr.BagGrid,       cellSize, cellSpacing);
        envGridUI?.Init(mgr.EnvGrid,      cellSize, cellSpacing);

        Debug.Log("[InventoryUI] Initialized.");
    }

    public ModuleItemUI SpawnModule(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        var inst = new ModuleInstance(data, rarity, level);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full leaew — {data.moduleName}");
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);

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
            Debug.LogWarning($"[InventoryUI] Bag full leaw — {data.moduleName}");
            return null;
        }

        var go = Instantiate(materialItemPrefab, transform);
        var ui = go.GetComponent<MaterialItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public MaterialItemUI SpawnExistingMaterialToEnv(MaterialInstance inst)
    {
        var mgr = InventoryManager.Instance;
        bool placed = false;
        for (int row = 0; row < mgr.EnvGrid.Height && !placed; row++)
            for (int col = 0; col < mgr.EnvGrid.Width && !placed; col++)
                if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                    placed = true;

        if (!placed) return null;

        var go = Instantiate(materialItemPrefab, transform);
        var ui = go.GetComponent<MaterialItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        StartCoroutine(SnapNextFrame(ui, envGridUI, inst.GridPosition));
        return ui;
    }

    public ModuleItemUI SpawnExistingModuleToEnv(ModuleInstance inst)
    {
        var mgr = InventoryManager.Instance;
        bool placed = false;
        for (int row = 0; row < mgr.EnvGrid.Height && !placed; row++)
            for (int col = 0; col < mgr.EnvGrid.Width && !placed; col++)
                if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                    placed = true;

        if (!placed)
        {
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        StartCoroutine(SnapNextFrame(ui, envGridUI, inst.GridPosition));
        return ui;
    }

    public ModuleItemUI SpawnModuleToEnv(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        var inst = new ModuleInstance(data, rarity, level);

        var mgr = InventoryManager.Instance;
        bool placed = false;
        for (int row = 0; row < mgr.EnvGrid.Height && !placed; row++)
            for (int col = 0; col < mgr.EnvGrid.Width && !placed; col++)
                if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                    placed = true;

        if (!placed)
        {
            Debug.LogWarning($"[InventoryUI] EnvGrid full leaw — {data.moduleName}");
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.InventoryPanelRt = GetComponent<RectTransform>();
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        StartCoroutine(SnapNextFrame(ui, envGridUI, inst.GridPosition));
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