using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button inventoryTabButton;
    [SerializeField] private Button playerStatTabButton;
    [SerializeField] private Button skillTabButton;
    [SerializeField] private Button questTabButton;
    [SerializeField] private Button menuTabButton;

    [Header("Tab Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject playerStatPanel;
    [SerializeField] private GameObject skillPanel;
    [SerializeField] private GameObject questPanel;
    [SerializeField] private GameObject menuPanel;

    [Header("Tab Active Indicators (optional)")]
    [SerializeField] private GameObject inventoryTabIndicator;
    [SerializeField] private GameObject playerStatTabIndicator;
    [SerializeField] private GameObject skillTabIndicator;
    [SerializeField] private GameObject questTabIndicator;
    [SerializeField] private GameObject menuTabIndicator;

    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;
    [SerializeField] private MaterialItemUI materialItemPrefab;

    [Header("Grid Settings")]
    [SerializeField] private float cellSize = 63f;
    [SerializeField] private float cellSpacing = 2f;

    public static GridUI StaticWeaponGridUI { get; private set; }
    public static GridUI StaticBagGridUI { get; private set; }

    public GridUI WeaponGridUI => weaponGridUI;
    public GridUI BagGridUI => bagGridUI;

    public enum Tab { Inventory, PlayerStat, Skill, Quest, Menu }
    public Tab CurrentTab { get; private set; } = Tab.Inventory;
    private readonly List<(MaterialItemUI ui, GridUI grid, Vector2Int cell)> pendingSnaps = new();

    private void Awake()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) { Debug.LogError("[InventoryUI] InventoryManager not found!"); return; }

        weaponGridUI.Init(mgr.WeaponGrid, cellSize, cellSpacing);
        bagGridUI.Init(mgr.BagGrid, cellSize, cellSpacing);

        weaponGridUI.SetWeaponGridState(mgr.WeaponUnlockedCols, mgr.WeaponUnlockedRows);

        StaticWeaponGridUI = weaponGridUI;
        StaticBagGridUI = bagGridUI;

        mgr.OnWeaponGridChanged += OnWeaponGridChanged;
        mgr.OnBagGridChanged += OnBagGridChanged;

        inventoryTabButton?.onClick.AddListener(() => SwitchTab(Tab.Inventory));
        playerStatTabButton?.onClick.AddListener(() => SwitchTab(Tab.PlayerStat));
        skillTabButton?.onClick.AddListener(() => SwitchTab(Tab.Skill));
        questTabButton?.onClick.AddListener(() => SwitchTab(Tab.Quest));
        menuTabButton?.onClick.AddListener(() => SwitchTab(Tab.Menu));

        SwitchTab(Tab.Inventory);
    }

    private void OnDestroy()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;
        mgr.OnWeaponGridChanged -= OnWeaponGridChanged;
        mgr.OnBagGridChanged -= OnBagGridChanged;
    }

    private void OnEnable()
    {
        if (pendingSnaps.Count == 0) return;
        StartCoroutine(FlushPendingSnaps());
    }

    private IEnumerator FlushPendingSnaps()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        foreach (var (ui, grid, cell) in pendingSnaps)
        {
            if (ui == null) continue;
            ui.SnapToCell(grid, cell);
            var cg = ui.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;
        }
        pendingSnaps.Clear();
    }

    public void SwitchTab(Tab tab)
    {
        CurrentTab = tab;
        inventoryPanel?.SetActive(tab == Tab.Inventory);
        playerStatPanel?.SetActive(tab == Tab.PlayerStat);
        skillPanel?.SetActive(tab == Tab.Skill);
        questPanel?.SetActive(tab == Tab.Quest);
        menuPanel?.SetActive(tab == Tab.Menu);

        if (inventoryTabIndicator != null) inventoryTabIndicator.SetActive(tab == Tab.Inventory);
        if (playerStatTabIndicator != null) playerStatTabIndicator.SetActive(tab == Tab.PlayerStat);
        if (skillTabIndicator != null) skillTabIndicator.SetActive(tab == Tab.Skill);
        if (questTabIndicator != null) questTabIndicator.SetActive(tab == Tab.Quest);
        if (menuTabIndicator != null) menuTabIndicator.SetActive(tab == Tab.Menu);
    }

    private void OnWeaponGridChanged()
    {
        var mgr = InventoryManager.Instance;
        weaponGridUI.SetWeaponGridState(mgr.WeaponUnlockedCols, mgr.WeaponUnlockedRows);
    }

    private void OnBagGridChanged() => bagGridUI.RefreshAll();

    public void RestoreBagItemRefs()
    {
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = weaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = weaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
                ui.ShopTooltipUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }
    }

    public ModuleItemUI SpawnExistingModule(ModuleInstance inst)
    {
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            if (DiscardGridUI.Instance != null)
                DiscardGridUI.Instance.ShowForOverflow(new List<ModuleInstance> { inst });
            else
                Debug.LogWarning($"[InventoryUI] Bag full — {inst.Data.moduleName}");
            return null;
        }

        var ui = Instantiate(moduleItemPrefab, bagGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst);
        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public ModuleItemUI SpawnModule(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        var inst = new ModuleInstance(data, rarity, level);

        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            if (DiscardGridUI.Instance != null)
                DiscardGridUI.Instance.ShowForOverflow(new List<ModuleInstance> { inst });
            else
                Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var ui = Instantiate(moduleItemPrefab, bagGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst);
        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public MaterialItemUI SpawnMaterial(MaterialData data)
    {
        foreach (var existing in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (existing is MaterialInstance mat && mat.MaterialData == data && mat.StackCount < mat.MaxStack)
            {
                mat.AddStack();
                return mat.UIElement as MaterialItemUI;
            }
        }

        var inst = new MaterialInstance(data);

        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            if (DiscardGridUI.Instance != null)
                DiscardGridUI.Instance.ShowForOverflow(new List<ModuleInstance> { inst });
            else
                Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var ui = Instantiate(materialItemPrefab, bagGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst);
        ui.InventoryUI = this;
        if (gameObject.activeInHierarchy)
            StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        else
            pendingSnaps.Add((ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    public MaterialItemUI SpawnSplitMaterial(MaterialData data, GridUI targetGridUI)
    {
        var inst = new MaterialInstance(data);

        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Cannot split — bag full");
            return null;
        }

        var ui = Instantiate(materialItemPrefab, targetGridUI.transform);
        ui.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ui.Init(inst);
        ui.InventoryUI = this;
        StartCoroutine(SnapNextFrame(ui, targetGridUI, inst.GridPosition));
        return ui;
    }

    private IEnumerator SnapNextFrame(MaterialItemUI ui, GridUI grid, Vector2Int cell)
    {
        var cg = ui.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        yield return null;
        if (ui == null) yield break;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(grid, cell);
        if (cg != null) cg.alpha = 1f;
    }

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI grid, Vector2Int cell)
    {
        var cg = ui.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(grid, cell);
        if (cg != null) cg.alpha = 1f;
    }
}