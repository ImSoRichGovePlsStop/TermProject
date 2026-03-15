using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUI shopBagGridUI;
    [SerializeField] private GridUI inventoryBagGridUI;
    [SerializeField] private GridUI inventoryWeaponGridUI;
    [SerializeField] private GridUI inventoryEnvGridUI;
    [SerializeField] private Transform itemListContainer;
    [SerializeField] private ShopItemUI shopItemPrefab;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;

    private ModuleItemUI _ghostUI;
    private ModuleInstance _ghostInst;
    private ShopItemUI _activeSeller;
    private Canvas _canvas;
    private RectTransform _canvasRt;
    private Vector2 _dragOffset;
    private Vector2Int _clickedCell;
    private bool _initialized = false;
    private ShopInteractable _currentInteractable;

    private void Awake()
    {
        _canvas = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();
    }

    private void Start()
    {
        var mgr = InventoryManager.Instance;
        var layout = GetComponentInParent<InventoryLayout>();
        float cellSize = layout != null ? layout.CellSize : 64f;
        float cellSpacing = layout != null ? layout.CellSpacing : 2f;

        shopBagGridUI.Init(mgr.BagGrid, cellSize, cellSpacing);
        _initialized = true;
    }

    private void OnEnable()
    {
        if (!_initialized) return;
        MoveItemsBetweenGrids(inventoryBagGridUI, shopBagGridUI);
    }

    private void OnDisable()
    {
        if (!_initialized) return;
        MoveItemsBetweenGrids(shopBagGridUI, inventoryBagGridUI);

        // Destroy ghost if shop closed mid-drag
        if (_ghostUI != null)
        {
            Destroy(_ghostUI.gameObject);
            _ghostUI = null;
            _ghostInst = null;
            _activeSeller = null;
            shopBagGridUI.ClearHighlights();
        }
    }

    public void Populate(TestModuleEntry[] entries, HashSet<int> soldIndices, ShopInteractable interactable)
    {
        _currentInteractable = interactable;

        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.data == null) continue;

            var item = Instantiate(shopItemPrefab, itemListContainer);
            item.Init(entry, this, i);

            if (soldIndices.Contains(i))
                item.MarkPurchased();
        }
    }

    public void RegisterSold(int index)
    {
        _currentInteractable?.RegisterSold(index);
    }

    public void BeginShopDrag(TestModuleEntry entry, PointerEventData e, ShopItemUI seller)
    {
        _activeSeller = seller;
        _ghostInst = new ModuleInstance(entry.data, entry.rarity, entry.level);
        _clickedCell = Vector2Int.zero;

        _ghostUI = Instantiate(moduleItemPrefab, _canvas.transform);
        _ghostUI.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _ghostUI.Init(_ghostInst, shopBagGridUI, shopBagGridUI);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var mouseLocal);
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

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var local))
            _ghostUI.GetComponent<RectTransform>().anchoredPosition = local - _dragOffset;

        if (shopBagGridUI.ScreenToCell(e.position, UICam(), out var hoveredCell))
        {
            var pivot = hoveredCell - _clickedCell;
            shopBagGridUI.HighlightCells(_ghostInst.Data, pivot,
                shopBagGridUI.Data.CanPlace(_ghostInst, pivot));
        }
        else
        {
            shopBagGridUI.ClearHighlights();
        }
    }

    public void EndShopDrag(PointerEventData e)
    {
        shopBagGridUI.ClearHighlights();

        if (_ghostUI == null) return;

        bool bought = false;

        if (shopBagGridUI.ScreenToCell(e.position, UICam(), out var hoveredCell))
        {
            var pivot = hoveredCell - _clickedCell;
            if (shopBagGridUI.Data.TryPlace(_ghostInst, pivot))
            {
                _activeSeller?.MarkPurchased();
                _activeSeller = null;

                var cg = _ghostUI.GetComponent<CanvasGroup>();
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                StartCoroutine(SnapNextFrame(_ghostUI, shopBagGridUI, _ghostInst.GridPosition));
                bought = true;
            }
        }

        if (!bought)
        {
            _activeSeller = null;
            Destroy(_ghostUI.gameObject);
        }

        _ghostUI = null;
        _ghostInst = null;
    }



    private void MoveItemsBetweenGrids(GridUI from, GridUI to)
    {
        bool goingToShop = to == shopBagGridUI;
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.BagGridUI = to;
                ui.WeaponGridUI = goingToShop ? to : inventoryWeaponGridUI;
                ui.EnvGridUI = goingToShop ? null : inventoryEnvGridUI;
                ui.transform.SetParent(to.transform, false);
                ui.SnapToCell(to, inst.GridPosition);
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.BagGridUI = to;
                ui.WeaponGridUI = goingToShop ? to : inventoryWeaponGridUI;
                ui.EnvGridUI = goingToShop ? null : inventoryEnvGridUI;
                ui.transform.SetParent(to.transform, false);
                ui.SnapToCell(to, inst.GridPosition);
            }
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

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}