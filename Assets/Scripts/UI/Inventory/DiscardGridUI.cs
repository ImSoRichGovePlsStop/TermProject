using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiscardGridUI : MonoBehaviour
{
    public static DiscardGridUI Instance { get; private set; }

    [Header("Grid")]
    [SerializeField] private GridUI gridUI;
    [SerializeField] private int    discardCols = 4;
    [SerializeField] private int    discardRows = 2;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI   moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    [Header("UI References")]
    [SerializeField] private GameObject      rootPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subtitleText;

    [Header("References")]
    [SerializeField] private InventoryUI inventoryUI;

    private GridData _discardGrid;
    private bool     _isDragMode;

    public bool     IsVisible   => rootPanel != null && rootPanel.activeSelf;
    public GridUI   GridUI      => gridUI;
    public GridData DiscardGrid => _discardGrid;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _discardGrid = new GridData(discardCols, discardRows, isWeaponGrid: false);
        gridUI.Init(_discardGrid, 63f, 2f);

        rootPanel?.SetActive(false);
    }

    public void ShowForDrag()
    {
        _isDragMode = true;
        SetTitle("Discard", "Drop here to discard");
        rootPanel?.SetActive(true);
    }

    public void ShowForOverflow(List<ModuleInstance> overflowItems)
    {
        _isDragMode = false;
        SetTitle("Bag Full", "Manage before closing inventory");
        rootPanel?.SetActive(true);

        foreach (var inst in overflowItems)
            SpawnToDiscard(inst);
    }

    public void OnDragEnded()
    {
        if (!_isDragMode) return;
        _isDragMode = false;

        if (!HasItems())
            ForceHide();
        else
            SetTitle("Discard", "Manage before closing inventory");
    }

    public void ForceHide()
    {
        ClearAll();
        rootPanel?.SetActive(false);
    }

    public void ClearAll()
    {
        var items = new List<ModuleInstance>(_discardGrid.GetAllModules());
        foreach (var inst in items)
        {
            _discardGrid.Remove(inst);
            if (inst.UIElement != null) Destroy(inst.UIElement.gameObject);
        }
    }

    private void SpawnToDiscard(ModuleInstance inst)
    {
        bool placed = false;
        for (int row = 0; row < _discardGrid.Height && !placed; row++)
            for (int col = 0; col < _discardGrid.Width && !placed; col++)
                if (_discardGrid.TryPlace(inst, new Vector2Int(col, row)))
                    placed = true;

        if (!placed) { Debug.LogWarning("[DiscardGrid] Grid full."); return; }

        if (inst is MaterialInstance matInst)
        {
            var ui = Instantiate(materialItemPrefab, gridUI.transform);
            ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ui.Init(matInst, gridUI);
            ui.InventoryUI = inventoryUI;
            StartCoroutine(SnapNextFrame(ui, gridUI, inst.GridPosition));
        }
        else
        {
            var ui = Instantiate(moduleItemPrefab, gridUI.transform);
            ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ui.Init(inst, gridUI);
            StartCoroutine(SnapNextFrame(ui, gridUI, inst.GridPosition));
        }
    }

    public bool HasItems() => _discardGrid.GetAllModules().Count > 0;

    private void SetTitle(string title, string subtitle)
    {
        if (titleText    != null) titleText.text    = title;
        if (subtitleText != null) subtitleText.text = subtitle;
    }

    private System.Collections.IEnumerator SnapNextFrame(Component ui, GridUI grid, Vector2Int cell)
    {
        var cg = ui.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (ui is MaterialItemUI mat) mat.SnapToCell(grid, cell);
        else if (ui is ModuleItemUI mod) mod.SnapToCell(grid, cell);
        if (cg != null) cg.alpha = 1f;
    }
}
