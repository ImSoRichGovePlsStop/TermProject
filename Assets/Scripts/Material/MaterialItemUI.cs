using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class MaterialItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public MaterialInstance Instance { get; private set; }

    [HideInInspector] public GridUI WeaponGridUI;
    [HideInInspector] public GridUI BagGridUI;

    [SerializeField] private float dragAlpha = 0.6f;
    [SerializeField] private float borderSize = 4f;

    private RectTransform _rt;
    private RectTransform _canvasRt;
    private CanvasGroup _cg;
    private Canvas _canvas;
    private Vector2Int _originCell;
    private Vector2 _dragOffset;
    private Vector2Int _clickedCell;
    private TextMeshProUGUI _stackText;

    [HideInInspector] public RectTransform InventoryPanelRt;

    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common    => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon  => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare      => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic      => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _                => Color.white
    };

    public void Init(MaterialInstance instance, GridUI weaponGridUI, GridUI bagGridUI)
    {
        Instance      = instance;
        instance.UIElement = this;
        WeaponGridUI  = weaponGridUI;
        BagGridUI     = bagGridUI;

        _rt       = GetComponent<RectTransform>();
        _cg       = GetComponent<CanvasGroup>();
        _canvas   = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();

        _rt.pivot = new Vector2(0f, 1f);

        var bound = instance.Data.GetBoundingSize();
        float cs = bagGridUI.cellSize;
        float sp = bagGridUI.cellSpacing;
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp,
                                    bound.y * (cs + sp) - sp);

        var img = GetComponent<Image>();
        img.color         = Color.clear;
        img.raycastTarget = false;

        Color borderColor = RarityColor(instance.Rarity);
        var shapeCells    = instance.Data.GetShapeCells();

        // Border layer
        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}",
                                          typeof(RectTransform), typeof(Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(_rt, false);
            borderRt.pivot     = new Vector2(0f, 1f);
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(0f, 1f);

            bool bHasRight  = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool bHasLeft   = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool bHasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool bHasTop    = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float bExtraRight  = bHasRight  ? borderSize + sp : 0f;
            float bExtraLeft   = bHasLeft   ? borderSize + sp : 0f;
            float bExtraBottom = bHasBottom ? borderSize + sp : 0f;
            float bExtraTop    = bHasTop    ? borderSize + sp : 0f;

            borderRt.sizeDelta = new Vector2(cs + bExtraLeft + bExtraRight,
                                             cs + bExtraTop  + bExtraBottom);
            borderRt.anchoredPosition = new Vector2( cell.x * (cs + sp) - bExtraLeft,
                                                    -cell.y * (cs + sp) + bExtraTop);

            borderGo.GetComponent<Image>().color         = borderColor;
            borderGo.GetComponent<Image>().raycastTarget = false;
        }

        Vector2Int topRightCell = shapeCells[0];
        foreach (var c in shapeCells)
            if (c.y < topRightCell.y || (c.y == topRightCell.y && c.x > topRightCell.x))
                topRightCell = c;

        // Cell layer
        foreach (var cell in shapeCells)
        {
            var go     = new GameObject($"cell_{cell.x}_{cell.y}",
                                        typeof(RectTransform), typeof(Image));
            var cellRt = go.GetComponent<RectTransform>();
            cellRt.SetParent(_rt, false);
            cellRt.pivot     = new Vector2(0f, 1f);
            cellRt.anchorMin = new Vector2(0f, 1f);
            cellRt.anchorMax = new Vector2(0f, 1f);

            bool hasRight  = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool hasLeft   = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool hasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool hasTop    = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float extraRight  = hasRight  ? borderSize + sp : 0f;
            float extraLeft   = hasLeft   ? borderSize + sp : 0f;
            float extraBottom = hasBottom ? borderSize + sp : 0f;
            float extraTop    = hasTop    ? borderSize + sp : 0f;

            cellRt.sizeDelta = new Vector2(cs - borderSize * 2f + extraLeft + extraRight,
                                           cs - borderSize * 2f + extraTop  + extraBottom);
            cellRt.anchoredPosition = new Vector2( cell.x * (cs + sp) + borderSize - extraLeft,
                                                  -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (instance.Data.icon != null) cellImg.sprite = instance.Data.icon;
            cellImg.color          = instance.MaterialData.moduleColor;
            cellImg.raycastTarget  = true;

            // Stack count text on bottom-right cell
            if (cell == topRightCell)
            {
                var stackGo = new GameObject("StackText",
                                             typeof(RectTransform), typeof(TextMeshProUGUI));
                var stackRt = stackGo.GetComponent<RectTransform>();
                stackRt.SetParent(cellRt, false);
                stackRt.anchorMin        = new Vector2(0f, 1f);
                stackRt.anchorMax        = new Vector2(1f, 1f);
                stackRt.pivot            = new Vector2(1f, 1f);
                stackRt.anchoredPosition = new Vector2(-2f, -2f);
                stackRt.sizeDelta        = new Vector2(0f, 20f);

                _stackText = stackGo.GetComponent<TextMeshProUGUI>();
                _stackText.fontSize      = 24f;
                _stackText.color         = Color.white;
                _stackText.alignment     = TextAlignmentOptions.TopRight;
                _stackText.raycastTarget = false;
            }
        }

        RefreshStackText();
        instance.OnStackChanged += RefreshStackText;

        _cg.alpha = 0f;
    }

    private void RefreshStackText()
    {
        if (_stackText == null) return;
        _stackText.text = Instance.MaxStack > 1 ? $"x{Instance.StackCount}" : "";
    }

    public void SnapToCell(GridUI gridUI, Vector2Int cell)
    {
        _rt.SetParent(InventoryPanelRt, worldPositionStays: false);
        Vector3 worldPos = gridUI.GetCellWorldTopLeft(cell);
        Vector3 localPos = InventoryPanelRt.InverseTransformPoint(worldPos);
        _rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        // Material is always in BagGrid
        _originCell = Instance.GridPosition;
        _clickedCell = GetClickedLocalCell(e);

        _rt.SetParent(_canvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var mouseLocal);
        _dragOffset = mouseLocal - _rt.anchoredPosition;

        _cg.alpha          = dragAlpha;
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var local))
            _rt.anchoredPosition = local - _dragOffset;

        UpdateHighlight(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.alpha          = 1f;
        _cg.blocksRaycasts = true;
        ClearHighlights();

        if (BagGridUI.ScreenToCell(e.position, UICam(), out var hoveredCell))
        {
            // Check stacking first — same material type + not full
            var existing = BagGridUI.Data.GetModuleAt(hoveredCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
            {
                InventoryManager.Instance.BagGrid.Remove(Instance);
                target.AddStack();
                Destroy(gameObject);
                return;
            }

            // Normal placement
            var pivot = hoveredCell - _clickedCell;
            bool moved = InventoryManager.Instance.TryMoveModule(Instance, BagGridUI.Data, pivot);
            SnapToCell(BagGridUI, moved ? Instance.GridPosition : _originCell);
        }
        else
        {
            SnapToCell(BagGridUI, _originCell);
        }
    }

    public void OnPointerEnter(PointerEventData e) => ModuleTooltipUI.Instance.Show(Instance);
    public void OnPointerExit(PointerEventData e)  => ModuleTooltipUI.Instance.Hide();

    private void UpdateHighlight(PointerEventData e)
    {
        ClearHighlights();

        // Hovering WeaponGrid → show invalid (red)
        if (WeaponGridUI.ScreenToCell(e.position, UICam(), out var weaponCell))
        {
            WeaponGridUI.HighlightCells(Instance.Data, weaponCell - _clickedCell, valid: false);
            return;
        }

        // Hovering BagGrid → check stacking or normal placement
        if (BagGridUI.ScreenToCell(e.position, UICam(), out var bagCell))
        {
            var existing = BagGridUI.Data.GetModuleAt(bagCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
                BagGridUI.HighlightCells(target.Data, target.GridPosition, valid: true);
            else
            {
                var pivot = bagCell - _clickedCell;
                BagGridUI.HighlightCells(Instance.Data, pivot, BagGridUI.Data.CanPlace(Instance, pivot));
            }
        }
    }

    private void ClearHighlights()
    {
        WeaponGridUI?.ClearHighlights();
        BagGridUI?.ClearHighlights();
    }

    private Vector2Int GetClickedLocalCell(PointerEventData e)
    {
        float cs = BagGridUI.cellSize;
        float sp = BagGridUI.cellSpacing;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt, e.position, UICam(), out var localPoint);

        int col = Mathf.FloorToInt(localPoint.x / (cs + sp));
        int row = Mathf.FloorToInt(-localPoint.y / (cs + sp));
        return new Vector2Int(Mathf.Max(0, col), Mathf.Max(0, row));
    }

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}
