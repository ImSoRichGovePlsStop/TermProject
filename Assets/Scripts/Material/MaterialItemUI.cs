using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class MaterialItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public MaterialInstance Instance { get; private set; }

    [HideInInspector] public GridUI WeaponGridUI;
    [HideInInspector] public GridUI BagGridUI;
    [HideInInspector] public GridUI EnvGridUI;
    [HideInInspector] public InventoryUI InventoryUI;

    [SerializeField] private float dragAlpha = 0.6f;
    [SerializeField] private float borderSize = 4f;

    private RectTransform _rt;
    private RectTransform _canvasRt;
    private CanvasGroup _cg;
    private Canvas _canvas;
    private GridUI _originGrid;
    private Vector2Int _originCell;
    private Vector2 _dragOffset;
    private Vector2Int _clickedCell;
    private TextMeshProUGUI _stackText;


    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common    => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon  => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare      => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic      => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _                => Color.white
    };

    public void Init(MaterialInstance instance, GridUI weaponGridUI, GridUI bagGridUI, GridUI envGridUI = null)
    {
        Instance      = instance;
        instance.UIElement = this;
        WeaponGridUI  = weaponGridUI;
        BagGridUI     = bagGridUI;
        EnvGridUI     = envGridUI;

        _rt       = GetComponent<RectTransform>();
        _cg       = GetComponent<CanvasGroup>();
        _canvas   = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();

        _rt.pivot = new Vector2(0f, 1f);

        var bound = instance.Data.GetBoundingSize();
        float cs = bagGridUI.CellSize;
        float sp = bagGridUI.CellSpacing;
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
        var gridRt = gridUI.GetComponent<RectTransform>();
        _rt.SetParent(gridRt, worldPositionStays: false);
        Vector3 worldPos = gridUI.GetCellWorldTopLeft(cell);
        Vector3 localPos = gridRt.InverseTransformPoint(worldPos);
        _rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _originGrid  = Instance.CurrentGrid == InventoryManager.Instance.EnvGrid ? EnvGridUI : BagGridUI;
        _originCell  = Instance.GridPosition;
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

        foreach (var g in new[] { BagGridUI, EnvGridUI })
        {
            if (g == null) continue;
            if (!g.ScreenToCell(e.position, UICam(), out var hoveredCell)) continue;

            // Check stacking (only within same grid type)
            var existing = g.Data.GetModuleAt(hoveredCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
            {
                int toAdd = Mathf.Min(Instance.StackCount, target.MaxStack - target.StackCount);
                for (int i = 0; i < toAdd; i++) target.AddStack();

                if (toAdd >= Instance.StackCount)
                {
                    _originGrid.Data.Remove(Instance);
                    Destroy(gameObject);
                }
                else
                {
                    for (int i = 0; i < toAdd; i++) Instance.RemoveStack();
                    SnapToCell(_originGrid, _originCell);
                }
                return;
            }

            var pivot = hoveredCell - _clickedCell;
            bool moved = InventoryManager.Instance.TryMoveModule(Instance, g.Data, pivot);
            SnapToCell(g, moved ? Instance.GridPosition : _originCell);
            return;
        }

        SnapToCell(_originGrid, _originCell);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;
        if (Instance.StackCount <= 1) return;
        if (InventoryUI == null) return;

        var targetGrid = Instance.CurrentGrid == InventoryManager.Instance.EnvGrid ? EnvGridUI : BagGridUI;

        Instance.RemoveStack();
        var ui = InventoryUI.SpawnSplitMaterial(Instance.MaterialData, targetGrid);
        if (ui == null) // grid เต็ม — rollback
            Instance.AddStack();
    }

    public void OnPointerEnter(PointerEventData e) => ModuleTooltipUI.Instance.Show(Instance);
    public void OnPointerExit(PointerEventData e)  => ModuleTooltipUI.Instance.Hide();

    private void UpdateHighlight(PointerEventData e)
    {
        ClearHighlights();

        // WeaponGrid → invalid เสมอ (material ลาก weapon grid ไม่ได้)
        if (WeaponGridUI.ScreenToCell(e.position, UICam(), out var weaponCell))
        {
            WeaponGridUI.HighlightCells(Instance.Data, weaponCell - _clickedCell, valid: false);
            return;
        }

        foreach (var g in new[] { BagGridUI, EnvGridUI })
        {
            if (g == null) continue;
            if (!g.ScreenToCell(e.position, UICam(), out var hoveredCell)) continue;

            var existing = g.Data.GetModuleAt(hoveredCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
                g.HighlightCells(target.Data, target.GridPosition, valid: true);
            else
            {
                var pivot = hoveredCell - _clickedCell;
                g.HighlightCells(Instance.Data, pivot, g.Data.CanPlace(Instance, pivot));
            }
            return;
        }
    }

    private void ClearHighlights()
    {
        WeaponGridUI?.ClearHighlights();
        BagGridUI?.ClearHighlights();
        EnvGridUI?.ClearHighlights();
    }

    private Vector2Int GetClickedLocalCell(PointerEventData e)
    {
        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt, e.position, UICam(), out var localPoint);

        int col = Mathf.FloorToInt(localPoint.x / (cs + sp));
        int row = Mathf.FloorToInt(-localPoint.y / (cs + sp));
        return new Vector2Int(Mathf.Max(0, col), Mathf.Max(0, row));
    }

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}
