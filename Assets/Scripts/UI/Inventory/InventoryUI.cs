using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;
    [SerializeField] private GridUI envGridUI;
    [SerializeField] private ShopUI shopUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI  moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    public GridUI WeaponGridUI => weaponGridUI;

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
        envGridUI?.gameObject.SetActive(false);

        Debug.Log("[InventoryUI] Initialized.");
    }

    public void ClearEnvGrid()
    {
        var mgr = InventoryManager.Instance;
        foreach (var inst in new List<ModuleInstance>(mgr.EnvGrid.GetAllModules()))
        {
            if (inst.UIElement != null)
                Destroy(inst.UIElement.gameObject);
            mgr.EnvGrid.Remove(inst);
        }
    }

    public void SetEnvGridVisible(bool visible)
    {
        envGridUI.gameObject.SetActive(visible);
    }

    public void TakeAllFromEnv()
    {
        var mgr = InventoryManager.Instance;
        var toTake = new List<ModuleInstance>(mgr.EnvGrid.GetAllModules());
        if (toTake.Count == 0) return;

        foreach (var inst in toTake)
        {
            var uiElem = inst.UIElement;
            mgr.EnvGrid.Remove(inst);

            if (!mgr.TryAddToBag(inst))
            {
                bool restored = false;
                for (int row = 0; row < mgr.EnvGrid.Height && !restored; row++)
                    for (int col = 0; col < mgr.EnvGrid.Width && !restored; col++)
                        if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                            restored = true;
                continue;
            }

            if (uiElem == null) continue;
            uiElem.transform.SetParent(bagGridUI.transform, false);
            if (uiElem is MaterialItemUI matUI)
                StartCoroutine(SnapNextFrame(matUI, bagGridUI, inst.GridPosition));
            else if (uiElem is ModuleItemUI modUI)
                StartCoroutine(SnapNextFrame(modUI, bagGridUI, inst.GridPosition));
        }
    }

    private void Update()
    {
        if (shopUI == null) return;
        shopUI.ForceMoveToBag();
    }

    public ModuleItemUI SpawnModule(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        var inst = new ModuleInstance(data, rarity, level);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full leaew — {data.moduleName}");
            return null;
        }

        var ui = Instantiate(moduleItemPrefab, bagGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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

        var ui = Instantiate(materialItemPrefab, bagGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        ui.InventoryUI = this;

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public MaterialItemUI SpawnSplitMaterial(MaterialData data, GridUI targetGridUI)
    {
        var inst = new MaterialInstance(data); // count = 1 เสมอ
        var mgr  = InventoryManager.Instance;

        bool placed;
        if (targetGridUI == envGridUI)
        {
            placed = false;
            for (int row = 0; row < mgr.EnvGrid.Height && !placed; row++)
                for (int col = 0; col < mgr.EnvGrid.Width && !placed; col++)
                    if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                        placed = true;
        }
        else
        {
            placed = mgr.TryAddToBag(inst);
        }

        if (!placed)
        {
            Debug.LogWarning($"[InventoryUI] Grid full — cannot split {data.moduleName}");
            return null;
        }

        var ui = Instantiate(materialItemPrefab, targetGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        ui.InventoryUI = this;
        StartCoroutine(SnapNextFrame(ui, targetGridUI, inst.GridPosition));
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

        var ui = Instantiate(materialItemPrefab, envGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst, weaponGridUI, bagGridUI, envGridUI);
        ui.InventoryUI = this;
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

        var ui = Instantiate(moduleItemPrefab, envGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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

        var ui = Instantiate(moduleItemPrefab, envGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
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