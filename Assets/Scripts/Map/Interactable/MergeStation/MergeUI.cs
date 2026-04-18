using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    [SerializeField] private TextMeshProUGUI totalCostText;
    [SerializeField] private TextMeshProUGUI outputRangeText;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    private GridData _inputGrid;
    private GridData _outputGrid;
    private bool _initialized = false;
    private MergeStation _currentStation = null;
    private Canvas _canvas;
    private InventoryUI _inventoryUI;
    private Coroutine _rollDisplayCoroutine;

    public static bool IsMergeOpen { get; private set; }

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        float cellSize = 63f;
        float cellSpacing = 2f;

        _inputGrid = new GridData(5, 5, isWeaponGrid: false);
        _outputGrid = new GridData(5, 5, isWeaponGrid: false);

        inputGridUI.Init(_inputGrid, cellSize, cellSpacing);
        outputGridUI.Init(_outputGrid, cellSize, cellSpacing);

        _outputGrid.OnModuleRemoved += OnOutputRemoved;
        _inputGrid.OnModulePlaced  += _ => RefreshCostDisplay();
        _inputGrid.OnModuleRemoved += _ => RefreshCostDisplay();
        mergeButton.onClick.AddListener(OnMergeClicked);
        _initialized = true;
    }

    private void OnDisable()
    {
        IsMergeOpen = false;
        if (!_initialized) return;

        ReturnInputToBag();
        ReturnOutputToBag();

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
        RefreshCostDisplay();
    }

    public void Open(MergeStation station)
    {
        IsMergeOpen = true;
        _currentStation = station;
        SetBagItemRefsForMerge();
        if (_currentStation.HasOutput) SpawnOutputUI(_currentStation.CachedOutput);
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
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = inputGridUI;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = inputGridUI;
                ui.ShopTooltipUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }

        foreach (var inst in InventoryManager.Instance.WeaponGrid.GetAllModules())
        {
            var ui = inst.UIElement as ModuleItemUI;
            if (ui == null) continue;
            ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
            ui.BagGridUI = bagGridUI;
            ui.EnvGridUI = null;
            ui.InputGridUI = inputGridUI;
            ui.ShopTooltipUI = null;
            ui.SellConfirmationUI = null;
            ui.SetAllowSell(false);
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
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
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
        if (inputModules.Count == 0) { Debug.LogWarning("[MergeUI] No input items!"); return; }

        int totalCost = CalculateTotalInputCost(inputModules);

        var run = RunManager.Instance;
        float valueMult  = run != null ? run.EffectiveMergeValueMultiplier  : 1f;
        float spreadMult = run != null ? run.EffectiveMergeSpreadMultiplier : 1f;
        int   minRarity  = 0;
        if (run != null)
        {
            if (run.EffectiveMergeGuaranteeSameRarity)
                minRarity = CalcAvgRarityIndex(inputModules);
            minRarity = Mathf.Clamp(minRarity + run.EffectiveMergeRarityBonus, 0, 4);
        }

        float mean = totalCost * 0.75f * valueMult;
        float sd   = totalCost * 0.1f  * spreadMult;
        float low  = Mathf.Max(0, mean - 2f * sd);
        float high = mean + 2f * sd;

        var entry = Randomizer.RollInRange(low, high, minRarityIndex: minRarity);
        float step = totalCost * 0.1f;
        while (entry.data == null && (high - low) < totalCost * 4f)
        {
            low  = Mathf.Max(0, low  - step);
            high = high + step;
            entry = Randomizer.RollInRange(low, high, minRarityIndex: minRarity);
        }
        if (entry.data == null) { Debug.LogWarning("[MergeUI] No module found in pool!"); return; }

        foreach (var inst in inputModules)
        {
            _inputGrid.Remove(inst);
            if (inst.UIElement is ModuleItemUI modUI) Destroy(modUI.gameObject);
            if (inst.UIElement is MaterialItemUI matUI) Destroy(matUI.gameObject);
        }

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
        outUI.Init(inst);
        outUI.InputGridUI = inputGridUI;
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

    private void ReturnGridToBag(GridData grid, bool restoreOnFull)
    {
        var modules = new List<ModuleInstance>(grid.GetAllModules());
        if (modules.Count == 0) return;

        foreach (var inst in modules)
        {
            var uiElem = inst.UIElement;
            grid.Remove(inst);

            if (!InventoryManager.Instance.TryAddToBag(inst))
            {
                if (restoreOnFull)
                {
                    bool restored = false;
                    for (int row = 0; row < grid.Height && !restored; row++)
                        for (int col = 0; col < grid.Width && !restored; col++)
                            if (grid.TryPlace(inst, new Vector2Int(col, row)))
                                restored = true;
                    if (!restored)
                        Debug.LogWarning($"[MergeUI] Bag full and couldn't restore {inst.Data.moduleName}");
                }
                else
                {
                    if (uiElem != null) Destroy(uiElem.gameObject);
                    Debug.LogWarning($"[MergeUI] Bag full — output item {inst.Data.moduleName} discarded.");
                }
                continue;
            }

            if (uiElem == null) continue;

            if (uiElem is MaterialItemUI matUI)
            {
                matUI.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                matUI.BagGridUI = bagGridUI;
                matUI.EnvGridUI = null;
                matUI.InputGridUI = null;
                matUI.SnapToCell(bagGridUI, inst.GridPosition);
            }
            else if (uiElem is ModuleItemUI modUI)
            {
                modUI.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                modUI.BagGridUI = bagGridUI;
                modUI.EnvGridUI = null;
                modUI.InputGridUI = null;
                modUI.SnapToCell(bagGridUI, inst.GridPosition);
            }
        }
    }

    private void ReturnInputToBag() => ReturnGridToBag(_inputGrid, restoreOnFull: true);
    private void ReturnOutputToBag() => ReturnGridToBag(_outputGrid, restoreOnFull: false);

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }

    private IEnumerator SnapNextFrame(MaterialItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }

    private static int CalcAvgRarityIndex(List<ModuleInstance> modules)
    {
        int total = 0, count = 0;
        foreach (var inst in modules)
        {
            if (inst is MaterialInstance) continue;
            total += (int)inst.Rarity;
            count++;
        }
        return count > 0 ? Mathf.RoundToInt((float)total / count) : 0;
    }

    private static int CalculateTotalInputCost(List<ModuleInstance> modules)
    {
        int total = 0;
        foreach (var inst in modules)
        {
            if (inst is MaterialInstance matInst)
                total += matInst.Cost * matInst.StackCount;
            else
            {
                var cost = inst.Data.cost;
                int idx = Mathf.Clamp((int)inst.Rarity, 0, cost.Length - 1);
                total += cost[idx];
            }
        }
        return total;
    }

    private void RefreshCostDisplay()
    {
        if (!_initialized) return;
        var modules = new List<ModuleInstance>(_inputGrid.GetAllModules());
        int totalCost = CalculateTotalInputCost(modules);
        bool hasInput = modules.Count > 0;

        if (totalCostText != null)
            totalCostText.text = hasInput ? $"Value: {totalCost}" : "";

        if (_rollDisplayCoroutine != null)
        {
            StopCoroutine(_rollDisplayCoroutine);
            _rollDisplayCoroutine = null;
        }

        if (!hasInput)
        {
            if (outputRangeText != null) outputRangeText.text = "";
            return;
        }

        var run = RunManager.Instance;
        float valueMult  = run != null ? run.EffectiveMergeValueMultiplier  : 1f;
        float spreadMult = run != null ? run.EffectiveMergeSpreadMultiplier : 1f;
        _rollDisplayCoroutine = StartCoroutine(RollDisplayCoroutine(totalCost, valueMult, spreadMult));
    }

    private IEnumerator RollDisplayCoroutine(int totalCost, float valueMult, float spreadMult)
    {
        float mean = totalCost * 0.75f * valueMult;
        float sd   = totalCost * 0.1f  * spreadMult;
        float low  = Mathf.Max(0, mean - 4f * sd);
        float high = mean + 4f * sd;

        while (true)
        {
            if (outputRangeText != null)
                outputRangeText.text = $"Value: ~{Mathf.RoundToInt(Random.Range(low, high))}";

            yield return new WaitForSeconds(Random.Range(0.08f, 0.18f));
        }
    }
}
