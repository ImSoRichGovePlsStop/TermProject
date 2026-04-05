using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUI bagGridUI;
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private ShopItemUI shopItemPrefab;
    [SerializeField] private SellConfirmationUI sellConfirmationUI;
    [SerializeField] private ShopTooltipUI shopTooltipUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;

    private ModuleItemUI _ghostUI;
    private ModuleInstance _ghostInst;
    private ShopItemUI _activeSeller;
    private Canvas _canvas;
    private RectTransform _canvasRt;
    private Vector2 _dragOffset;
    private Vector2Int _clickedCell;
    private ShopInteractable _currentInteractable;
    private int _pendingPrice;
    private TestModuleEntry[] _currentEntries;
    private InventoryUI _inventoryUI;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();
        _inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
    }

    public void OnOpened() => SetBagItemRefsForShop();

    public void OnClosed()
    {
        SetBagItemRefsForInventory();

        if (_ghostUI != null)
        {
            Destroy(_ghostUI.gameObject);
            _ghostUI = null;
            _ghostInst = null;
            _activeSeller = null;
        }

        bagGridUI.ClearHighlights();
        bagGridUI.ClearBuffHighlights();
    }

    public void ForceMoveToBag() { }
    public void ForceMoveToShop() { }

    private void SetBagItemRefsForShop()
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
                ui.ShopTooltipUI = shopTooltipUI;
                ui.SellConfirmationUI = sellConfirmationUI;
                ui.SetAllowSell(true);
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
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = _inventoryUI.WeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
                ui.ShopTooltipUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }
    }

    public void Populate(TestModuleEntry[] entries, HashSet<int> soldIndices, ShopInteractable interactable)
    {
        _currentInteractable = interactable;
        _currentEntries = entries;

        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        var indexed = new List<(TestModuleEntry entry, int originalIndex)>();
        for (int i = 0; i < entries.Length; i++)
            indexed.Add((entries[i], i));

        indexed.Sort((a, b) => GetSortPriority(a.originalIndex, soldIndices).CompareTo(GetSortPriority(b.originalIndex, soldIndices)));

        foreach (var (entry, originalIndex) in indexed)
        {
            if (entry.data == null) continue;
            var item = Instantiate(shopItemPrefab, itemListContainer);
            item.Init(entry, this, originalIndex);
            if (soldIndices.Contains(originalIndex)) item.MarkPurchased();
        }
    }

    private int GetSortPriority(int index, HashSet<int> soldIndices)
    {
        if (soldIndices.Contains(index)) return 2;
        if (CurrencyManager.Instance == null) return 0;
        int price = _currentEntries[index].data.cost[(int)_currentEntries[index].rarity];
        return CurrencyManager.Instance.Coins >= price ? 0 : 1;
    }

    public void RegisterSold(int index) => _currentInteractable?.RegisterSold(index);

    public void BeginShopDrag(TestModuleEntry entry, PointerEventData e, ShopItemUI seller)
    {
        _activeSeller = seller;
        _ghostInst = new ModuleInstance(entry.data, entry.rarity, entry.level);
        _clickedCell = Vector2Int.zero;
        _pendingPrice = entry.data.cost[(int)entry.rarity];

        _ghostUI = Instantiate(moduleItemPrefab, _canvas.transform);
        _ghostUI.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _ghostUI.Init(_ghostInst);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var mouseLocal);
        _ghostUI.GetComponent<RectTransform>().anchoredPosition = mouseLocal;
        _dragOffset = Vector2.zero;

        var cg = _ghostUI.GetComponent<CanvasGroup>();
        cg.alpha = 0.6f;
        cg.blocksRaycasts = false;
        _ghostUI.GetComponent<RectTransform>().SetAsLastSibling();
    }

    public void UpdateShopDrag(PointerEventData e)
    {
        if (_ghostUI == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var local))
            _ghostUI.GetComponent<RectTransform>().anchoredPosition = local - _dragOffset;

        bagGridUI.ClearHighlights();
        InventoryUI.StaticWeaponGridUI?.ClearHighlights();

        if (InventoryUI.StaticWeaponGridUI != null && InventoryUI.StaticWeaponGridUI.ScreenToCell(e.position, UICam(), out var weaponCell))
        {
            var pivot = weaponCell - _clickedCell;
            bool canPlace = InventoryManager.Instance.CanPlaceInWeaponGrid(_ghostInst, pivot, 0);
            InventoryUI.StaticWeaponGridUI.HighlightCells(_ghostInst.Data, pivot, canPlace);
        }
        else if (bagGridUI.ScreenToCell(e.position, UICam(), out var hoveredCell))
        {
            var pivot = hoveredCell - _clickedCell;
            bagGridUI.HighlightCells(_ghostInst.Data, pivot, bagGridUI.Data.CanPlace(_ghostInst, pivot));
        }
    }

    public void EndShopDrag(PointerEventData e)
    {
        bagGridUI.ClearHighlights();
        InventoryUI.StaticWeaponGridUI?.ClearHighlights();
        if (_ghostUI == null) return;

        bool bought = false;

        if (InventoryUI.StaticWeaponGridUI != null && InventoryUI.StaticWeaponGridUI.ScreenToCell(e.position, UICam(), out var weaponCell))
        {
            var pivot = weaponCell - _clickedCell;
            if (InventoryManager.Instance.CanPlaceInWeaponGrid(_ghostInst, pivot, 0)
                && InventoryManager.Instance.WeaponGrid.TryPlace(_ghostInst, pivot))
            {
                if (CurrencyManager.Instance != null && !CurrencyManager.Instance.TrySpend(_pendingPrice))
                {
                    InventoryManager.Instance.WeaponGrid.Remove(_ghostInst);
                    Destroy(_ghostUI.gameObject);
                    _ghostUI = null; _ghostInst = null; _activeSeller = null;
                    return;
                }

                _activeSeller?.MarkPurchased();
                _activeSeller = null;

                var cg = _ghostUI.GetComponent<CanvasGroup>();
                cg.alpha = 1f; cg.blocksRaycasts = true;
                StartCoroutine(SnapNextFrame(_ghostUI, InventoryUI.StaticWeaponGridUI, _ghostInst.GridPosition));
                bought = true;
            }
        }
        else if (bagGridUI.ScreenToCell(e.position, UICam(), out var hoveredCell))
        {
            var pivot = hoveredCell - _clickedCell;
            if (bagGridUI.Data.TryPlace(_ghostInst, pivot))
            {
                if (CurrencyManager.Instance != null && !CurrencyManager.Instance.TrySpend(_pendingPrice))
                {
                    bagGridUI.Data.Remove(_ghostInst);
                    Destroy(_ghostUI.gameObject);
                    _ghostUI = null; _ghostInst = null; _activeSeller = null;
                    return;
                }

                _activeSeller?.MarkPurchased();
                _activeSeller = null;

                _ghostUI.SetAllowSell(true);
                _ghostUI.SellConfirmationUI = sellConfirmationUI;
                _ghostUI.ShopTooltipUI = shopTooltipUI;

                var cg = _ghostUI.GetComponent<CanvasGroup>();
                cg.alpha = 1f; cg.blocksRaycasts = true;
                StartCoroutine(SnapNextFrame(_ghostUI, bagGridUI, _ghostInst.GridPosition));
                bought = true;
            }
        }

        if (!bought) { _activeSeller = null; Destroy(_ghostUI.gameObject); }
        _ghostUI = null; _ghostInst = null;
    }

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }

    private Camera UICam() => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}