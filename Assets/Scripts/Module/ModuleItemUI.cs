using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ModuleItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    public ModuleInstance Instance { get; private set; }

    [HideInInspector] public GridUI WeaponGridUI;
    [HideInInspector] public GridUI BagGridUI;
    [HideInInspector] public GridUI EnvGridUI;

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

    [HideInInspector] public RectTransform InventoryPanelRt;

    // Rarity Colors
    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _ => Color.white
    };

    public void Init(ModuleInstance instance, GridUI weaponGridUI, GridUI bagGridUI, GridUI envGridUI = null)
    {
        Instance = instance;
        instance.UIElement = this;
        WeaponGridUI = weaponGridUI;
        BagGridUI    = bagGridUI;
        EnvGridUI    = envGridUI;

        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();

        _rt.pivot = new Vector2(0f, 1f);

        var bound = instance.Data.GetBoundingSize();
        float cs = weaponGridUI.CellSize;
        float sp = weaponGridUI.CellSpacing;
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp,
                                    bound.y * (cs + sp) - sp);

        var img = GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;

        Color borderColor = RarityColor(instance.Rarity);
        var shapeCells = instance.Data.GetShapeCells();

        // Border layer
        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}",
                                          typeof(RectTransform), typeof(Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(_rt, false);
            borderRt.pivot = new Vector2(0f, 1f);
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(0f, 1f);
            bool bHasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool bHasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool bHasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool bHasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float bExtraRight = bHasRight ? borderSize + sp : 0f;
            float bExtraLeft = bHasLeft ? borderSize + sp : 0f;
            float bExtraBottom = bHasBottom ? borderSize + sp : 0f;
            float bExtraTop = bHasTop ? borderSize + sp : 0f;

            borderRt.sizeDelta = new Vector2(
                cs + bExtraLeft + bExtraRight,
                cs + bExtraTop + bExtraBottom);

            borderRt.anchoredPosition = new Vector2(
                 cell.x * (cs + sp) - bExtraLeft,
                -cell.y * (cs + sp) + bExtraTop);

            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;
        }

        Vector2Int topRightCell = shapeCells[0];
        foreach (var c in shapeCells)
        {
            if (c.y < topRightCell.y || (c.y == topRightCell.y && c.x > topRightCell.x))
                topRightCell = c;
        }

        // Cell layer
        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}",
                                        typeof(RectTransform), typeof(Image));
            var cellRt = go.GetComponent<RectTransform>();
            cellRt.SetParent(_rt, false);
            cellRt.pivot = new Vector2(0f, 1f);
            cellRt.anchorMin = new Vector2(0f, 1f);
            cellRt.anchorMax = new Vector2(0f, 1f);
            bool hasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool hasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool hasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool hasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float extraRight = hasRight ? borderSize + sp : 0f;
            float extraLeft = hasLeft ? borderSize + sp : 0f;
            float extraBottom = hasBottom ? borderSize + sp : 0f;
            float extraTop = hasTop ? borderSize + sp : 0f;

            cellRt.sizeDelta = new Vector2(
                cs - borderSize * 2f + extraLeft + extraRight,
                cs - borderSize * 2f + extraTop + extraBottom);

            cellRt.anchoredPosition = new Vector2(
                cell.x * (cs + sp) + borderSize - extraLeft,
               -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (instance.Data.icon != null) cellImg.sprite = instance.Data.icon;
            cellImg.color = instance.Data.moduleColor;
            cellImg.raycastTarget = true;

            // Level text on first cell
            if (cell == topRightCell)
            {
                if (instance.Level > 0)
                {
                    var textGo = new GameObject("LevelText",
                                               typeof(RectTransform), typeof(TextMeshProUGUI));
                    var textRt = textGo.GetComponent<RectTransform>();
                    textRt.SetParent(cellRt, false);
                    textRt.anchorMin = new Vector2(0f, 1f);
                    textRt.anchorMax = new Vector2(1f, 1f);
                    textRt.pivot = new Vector2(1f, 1f);
                    textRt.anchoredPosition = new Vector2(-2f, -2f);
                    textRt.sizeDelta = new Vector2(0f, 20f);

                    var tmp = textGo.GetComponent<TextMeshProUGUI>();
                    tmp.text = $"+{instance.Level}";
                    tmp.fontSize = 24f;
                    tmp.color = Color.white;
                    tmp.alignment = TextAlignmentOptions.TopRight;
                    tmp.raycastTarget = false;
                }
            }
        }

        _cg.alpha = 0f;
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
        var mgr = InventoryManager.Instance;
        if (Instance.CurrentGrid == mgr.WeaponGrid)
            _originGrid = WeaponGridUI;
        else if (EnvGridUI != null && Instance.CurrentGrid == mgr.EnvGrid)
            _originGrid = EnvGridUI;
        else
            _originGrid = BagGridUI;
        _originCell = Instance.GridPosition;

        _clickedCell = GetClickedLocalCell(e);

        _rt.SetParent(_canvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var mouseLocal);
        _dragOffset = mouseLocal - _rt.anchoredPosition;

        _cg.alpha = dragAlpha;
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
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        ClearHighlights();

        GridUI targetGrid = null;
        Vector2Int pivot = Vector2Int.zero;

        foreach (var g in new[] { WeaponGridUI, BagGridUI, EnvGridUI })
        {
            if (g == null) continue;
            if (g.ScreenToCell(e.position, UICam(), out var hoveredCell))
            {
                targetGrid = g;
                pivot = hoveredCell - _clickedCell;
                break;
            }
        }

        bool moved = targetGrid != null &&
                     InventoryManager.Instance.TryMoveModule(Instance, targetGrid.Data, pivot);

        var snapGrid = moved ? targetGrid : _originGrid;
        var snapCell = moved ? Instance.GridPosition : _originCell;

        SnapToCell(snapGrid, snapCell);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        ModuleTooltipUI.Instance.Show(Instance, WeaponGridUI, BagGridUI);
    }

    public void OnPointerExit(PointerEventData e)
    {
        ModuleTooltipUI.Instance.Hide();
    }

    private void UpdateHighlight(PointerEventData e)
    {
        ClearHighlights();
        foreach (var g in new[] { WeaponGridUI, BagGridUI, EnvGridUI })
        {
            if (g == null) continue;
            if (g.ScreenToCell(e.position, UICam(), out var hoveredCell))
            {
                var pivot = hoveredCell - _clickedCell;
                g.HighlightCells(Instance.Data, pivot, g.Data.CanPlace(Instance, pivot));
                return;
            }
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
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rt, e.position, UICam(), out var localPoint);

        int col = Mathf.FloorToInt(localPoint.x / (cs + sp));
        int row = Mathf.FloorToInt(-localPoint.y / (cs + sp));

        return new Vector2Int(Mathf.Max(0, col), Mathf.Max(0, row));
    }

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}