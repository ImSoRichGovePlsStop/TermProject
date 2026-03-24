using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MergeUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI inputGridUI;
    [SerializeField] private GridUI outputGridUI;
    [SerializeField] private GridUI bagGridUI;

    [Header("UI")]
    [SerializeField] private Button mergeButton;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    private GridData _inputGrid;
    private GridData _outputGrid;
    private bool _initialized = false;
    private MergeStation _currentStation = null;
    private Canvas _canvas;
    private InventoryUI _inventoryUI;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        var mgr = InventoryManager.Instance;
        var layout = GetComponentInParent<InventoryLayout>();
        float cellSize = layout != null ? layout.CellSize : 64f;
        float cellSpacing = layout != null ? layout.CellSpacing : 2f;

        _inputGrid = new GridData(5, 5, isWeaponGrid: false);
        _outputGrid = new GridData(5, 5, isWeaponGrid: false);

        inputGridUI.Init(_inputGrid, cellSize, cellSpacing);
        outputGridUI.Init(_outputGrid, cellSize, cellSpacing);

        _outputGrid.OnModuleRemoved += OnOutputRemoved;
        mergeButton.onClick.AddListener(OnMergeClicked);
        _initialized = true;
    }

    private void OnDisable()
    {
        if (!_initialized) return;

        ReturnInputToBag();

        if (_currentStation != null && _currentStation.CachedOutput != null)
        {
            var ui = _currentStation.CachedOutput.UIElement as ModuleItemUI;
            if (ui != null)
            {
                _outputGrid.Remove(_currentStation.CachedOutput);
                Destroy(ui.gameObject);
            }
        }

        _currentStation = null;

        SetBagItemRefsForInventory();
        ModuleTooltipUI.Instance?.Hide();
        bagGridUI.ClearHighlights();
        bagGridUI.ClearBuffHighlights();
    }

    public void Open(MergeStation station)
    {
        _currentStation = station;
        SetBagItemRefsForMerge();

        if (_currentStation.HasOutput)
            SpawnOutputUI(_currentStation.CachedOutput);

        RefreshInputInteractable();
    }

    public void ForceMoveToBag()
    {
        if (!_initialized) return;
        SetBagItemRefsForInventory();
    }

    private void SetBagItemRefsForMerge()
    {
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = bagGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = inputGridUI;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = bagGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = inputGridUI;
                ui.ShopTooltipUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }
    }

    private void SetBagItemRefsForInventory()
    {
        if (_inventoryUI == null) return;
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = _inventoryUI.WeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = _inventoryUI.EnvGridUI;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = _inventoryUI.WeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = _inventoryUI.EnvGridUI;
                ui.InputGridUI = null;
                ui.ShopTooltipUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }
    }

    private void OnMergeClicked()
    {
        if (_currentStation != null && _currentStation.HasOutput)
        {
            Debug.LogWarning("[MergeUI] Collect the output item first!");
            return;
        }

        var inputModules = new List<ModuleInstance>(_inputGrid.GetAllModules());
        if (inputModules.Count == 0)
        {
            Debug.LogWarning("[MergeUI] No input items!");
            return;
        }

        int totalCost = 0;
        foreach (var inst in inputModules)
            totalCost += inst.Data.cost[(int)inst.Rarity];

        var rolled = Randomizer.Roll(1, 1, totalCost*0.75f, totalCost * 0.2f);
        if (rolled.Count == 0)
        {
            Debug.LogWarning("[MergeUI] Randomizer returned no results!");
            return;
        }

        foreach (var inst in inputModules)
        {
            _inputGrid.Remove(inst);
            var moduleUI = inst.UIElement as ModuleItemUI;
            if (moduleUI != null) Destroy(moduleUI.gameObject);
            var matUI = inst.UIElement as MaterialItemUI;
            if (matUI != null) Destroy(matUI.gameObject);
        }

        var entry = rolled[0];
        var outInst = new ModuleInstance(entry.data, entry.rarity, entry.level);
        _currentStation.CachedOutput = outInst;

        PlaceOutputInGrid(outInst);
        RefreshInputInteractable();
    }

    private void PlaceOutputInGrid(ModuleInstance inst)
    {
        bool placed = false;
        for (int row = 0; row < _outputGrid.Height && !placed; row++)
            for (int col = 0; col < _outputGrid.Width && !placed; col++)
                if (_outputGrid.TryPlace(inst, new Vector2Int(col, row)))
                    placed = true;

        if (!placed) { Debug.LogWarning("[MergeUI] Output grid full!"); return; }
        SpawnOutputUI(inst);
    }

    private void SpawnOutputUI(ModuleInstance inst)
    {
        if (inst.CurrentGrid != _outputGrid)
        {
            bool placed = false;
            for (int row = 0; row < _outputGrid.Height && !placed; row++)
                for (int col = 0; col < _outputGrid.Width && !placed; col++)
                    if (_outputGrid.TryPlace(inst, new Vector2Int(col, row)))
                        placed = true;

            if (!placed) { Debug.LogWarning("[MergeUI] Could not restore output to grid!"); return; }
        }

        var outUI = Instantiate(moduleItemPrefab, outputGridUI.transform);
        outUI.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        outUI.Init(inst, bagGridUI, bagGridUI);
        StartCoroutine(SnapNextFrame(outUI, outputGridUI, inst.GridPosition));
    }

    private void OnOutputRemoved(ModuleInstance inst)
    {
        if (_currentStation == null || inst != _currentStation.CachedOutput) return;
        _currentStation.CachedOutput = null;
        RefreshInputInteractable();
    }

    private void RefreshInputInteractable()
    {
        bool hasOutput = _currentStation != null && _currentStation.HasOutput;

        var inputCg = inputGridUI.GetComponent<CanvasGroup>();
        if (inputCg == null) inputCg = inputGridUI.gameObject.AddComponent<CanvasGroup>();
        inputCg.interactable = !hasOutput;
        inputCg.blocksRaycasts = !hasOutput;
        inputCg.alpha = hasOutput ? 0.5f : 1f;

        mergeButton.interactable = !hasOutput;
    }

    private void ReturnInputToBag()
    {
        var inputModules = new List<ModuleInstance>(_inputGrid.GetAllModules());
        foreach (var inst in inputModules)
        {
            var moduleUI = inst.UIElement as ModuleItemUI;
            if (moduleUI != null) Destroy(moduleUI.gameObject);
            var matUI = inst.UIElement as MaterialItemUI;
            if (matUI != null) Destroy(matUI.gameObject);

            _inputGrid.Remove(inst);

            if (!InventoryManager.Instance.TryAddToBag(inst))
            {
                Debug.LogWarning($"[MergeUI] Bag full — could not return {inst.Data.moduleName}");
                continue;
            }

            var go = Instantiate(moduleItemPrefab, _canvas.transform);
            go.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            go.Init(inst, bagGridUI, bagGridUI);
            StartCoroutine(SnapNextFrame(go, bagGridUI, inst.GridPosition));
        }
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