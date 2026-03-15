using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUI shopBagGridUI;
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

    private void Awake()
    {
        var mgr = InventoryManager.Instance;
        var layout = GetComponentInParent<InventoryLayout>();
        float cellSize = layout != null ? layout.CellSize : 64f;
        float cellSpacing = layout != null ? layout.CellSpacing : 2f;

        shopBagGridUI.Init(mgr.BagGrid, cellSize, cellSpacing);

        _canvas = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();
    }

    public void Populate(ShopEntry[] entries)
    {
        foreach (Transform child in itemListContainer)
            Destroy(child.gameObject);

        foreach (var entry in entries)
        {
            if (entry.data == null) continue;
            var item = Instantiate(shopItemPrefab, itemListContainer);
            item.Init(entry, this);
        }
    }

    public void BeginShopDrag(ShopEntry entry, PointerEventData e, ShopItemUI seller)
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