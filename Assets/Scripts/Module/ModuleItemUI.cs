using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ModuleItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public ModuleInstance Instance { get; private set; }

    [HideInInspector] public GridUI WeaponGridUI;
    [HideInInspector] public GridUI BagGridUI;
    [HideInInspector] public GridUI EnvGridUI;
    [HideInInspector] public GridUI InputGridUI;

    [SerializeField] private float dragAlpha = 0.6f;
    [SerializeField] private float borderSize = 4f;

    private RectTransform _rt;
    private RectTransform _canvasRt;
    private CanvasGroup _cg;
    private Canvas _canvas;
    private GridUI _originGrid;
    private Vector2Int _originCell;
    private int _originRotation;
    private Vector2 _dragOffset;
    private Vector2Int _clickedCell;
    private int _dragRotation;
    private bool _isDragging;

    [SerializeField] private bool allowSell = false;

    [HideInInspector] public ShopTooltipUI ShopTooltipUI;
    [HideInInspector] public SellConfirmationUI SellConfirmationUI;

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
        BagGridUI = bagGridUI;
        EnvGridUI = envGridUI;

        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _canvas = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();

        _rt.pivot = new Vector2(0f, 1f);

        var img = GetComponent<Image>();
        img.color = Color.clear;
        img.raycastTarget = false;

        RebuildVisual(Instance.Rotation);

        _cg.alpha = 0f;
    }

    private void RebuildVisual(int rotation)
    {
        for (int i = _rt.childCount - 1; i >= 0; i--)
            Destroy(_rt.GetChild(i).gameObject);

        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;

        var bound = Instance.Data.GetBoundingSize(rotation);
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp,
                                    bound.y * (cs + sp) - sp);

        BuildVisualCells(Instance.Data.GetShapeCells(rotation));
    }

    private void BuildVisualCells(System.Collections.Generic.List<Vector2Int> shapeCells)
    {
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;
        Color borderColor = RarityColor(Instance.Rarity);

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
            if (c.y < topRightCell.y || (c.y == topRightCell.y && c.x > topRightCell.x))
                topRightCell = c;

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
            if (Instance.Data.icon != null) cellImg.sprite = Instance.Data.icon;
            cellImg.color = Instance.Data.moduleColor;
            cellImg.raycastTarget = true;

            if (cell == topRightCell && Instance.Level > 0)
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
                tmp.text = $"+{Instance.Level}";
                tmp.fontSize = 24f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.TopRight;
                tmp.raycastTarget = false;
            }
        }
    }

    public void RefreshAfterUpgrade()
    {
        RebuildVisual(Instance.Rotation);
        _cg.alpha = 1f;
    }

    private void Update()
    {
        if (_isDragging && Keyboard.current != null && Keyboard.current[Key.R].wasPressedThisFrame)
        {
            _dragRotation = (_dragRotation + 1) % 4;
            _clickedCell = Vector2Int.zero;
            RebuildVisual(_dragRotation);
            UpdateHighlightInternal();
        }
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
        var mgr = InventoryManager.Instance;
        if (Instance.CurrentGrid == mgr.WeaponGrid)
            _originGrid = WeaponGridUI;
        else if (EnvGridUI != null && Instance.CurrentGrid == mgr.EnvGrid)
            _originGrid = EnvGridUI;
        else if (InputGridUI != null && Instance.CurrentGrid == InputGridUI.Data)
            _originGrid = InputGridUI;
        else
            _originGrid = BagGridUI;

        _originCell = Instance.GridPosition;
        _originRotation = Instance.Rotation;
        _dragRotation = Instance.Rotation;
        _isDragging = true;
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
        _isDragging = false;
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        ClearHighlights();

        GridUI targetGrid = null;
        Vector2Int pivot = Vector2Int.zero;
        Vector2 sampleScreen = GetClickedCellCenterScreen();

        foreach (var g in new[] { WeaponGridUI, BagGridUI, EnvGridUI, InputGridUI })
        {
            if (g == null) continue;
            if (g.ScreenToCell(sampleScreen, UICam(), out var hoveredCell))
            {
                targetGrid = g;
                pivot = hoveredCell - _clickedCell;
                break;
            }
        }

        bool moved = false;
        if (targetGrid != null)
        {
            var prevGrid = Instance.CurrentGrid;
            var prevPos = Instance.GridPosition;

            prevGrid?.Remove(Instance);
            Instance.SetRotation(_dragRotation);

            if (targetGrid.Data.TryPlace(Instance, pivot))
            {
                moved = true;
            }
            else
            {
                var blocker = targetGrid.Data.BlockingModuleMai(Instance, pivot, _dragRotation);
                bool blockerIsMaterial = prevGrid != null && prevGrid.IsWeaponGrid && blocker is MaterialInstance;

                if (blocker != null && !blockerIsMaterial)
                {
                    var oldPos = blocker.GridPosition;
                    targetGrid.Data.Remove(blocker);

                    bool placedDrag = targetGrid.Data.TryPlace(Instance, pivot);
                    bool placedSwap = placedDrag && prevGrid != null && prevGrid.TryPlace(blocker, prevPos);

                    if (placedDrag && placedSwap)
                    {
                        moved = true;
                        if (blocker.UIElement is ModuleItemUI blockerUI)
                            blockerUI.SnapToCell(_originGrid, prevPos);
                        else if (blocker.UIElement is MaterialItemUI blockerMatUI)
                            blockerMatUI.SnapToCell(_originGrid, prevPos);
                    }
                    else
                    {
                        if (placedDrag) targetGrid.Data.Remove(Instance);
                        targetGrid.Data.TryPlace(blocker, oldPos);
                        Instance.SetRotation(_originRotation);
                        prevGrid?.TryPlace(Instance, prevPos);
                    }
                }
                else
                {
                    Instance.SetRotation(_originRotation);
                    prevGrid?.TryPlace(Instance, prevPos);
                }
            }
        }

        if (!moved)
        {
            _dragRotation = Instance.Rotation;
            RebuildVisual(_dragRotation);
        }

        var snapGrid = moved ? targetGrid : _originGrid;
        var snapCell = moved ? Instance.GridPosition : _originCell;

        SnapToCell(snapGrid, snapCell);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (ShopTooltipUI != null)
        {
            int price = Instance.Data.cost[(int)Instance.Rarity];
            ShopTooltipUI.ShowModule(Instance, price);
        }
        else
        {
            ModuleTooltipUI.Instance.Show(Instance, WeaponGridUI, BagGridUI, EnvGridUI);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (ShopTooltipUI != null)
            ShopTooltipUI.Hide();
        else
            ModuleTooltipUI.Instance.Hide();
    }

    public void SetAllowSell(bool allow) => allowSell = allow;

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;
        if (!allowSell) return;
        if (Instance.CurrentGrid != InventoryManager.Instance.BagGrid) return;
        SellConfirmationUI?.Show(Instance, e.position);
    }

    private void UpdateHighlight(PointerEventData e) => UpdateHighlightInternal();

    private void UpdateHighlightInternal()
    {
        ClearHighlights();
        Vector2 sampleScreen = GetClickedCellCenterScreen();

        foreach (var g in new[] { WeaponGridUI, BagGridUI, EnvGridUI, InputGridUI })
        {
            if (g == null) continue;
            if (g.ScreenToCell(sampleScreen, UICam(), out var hoveredCell))
            {
                var pivot = hoveredCell - _clickedCell;
                g.HighlightCells(Instance.Data, pivot, g.Data.CanPlace(Instance, pivot, _dragRotation), _dragRotation);
                return;
            }
        }
    }

    private void ClearHighlights()
    {
        WeaponGridUI?.ClearHighlights();
        BagGridUI?.ClearHighlights();
        EnvGridUI?.ClearHighlights();
        InputGridUI?.ClearHighlights();
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

    private Vector2 GetClickedCellCenterScreen()
    {
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;

        Vector2 cellCenterLocal = new Vector2(
             (_clickedCell.x + 0.5f) * (cs + sp),
            -(_clickedCell.y + 0.5f) * (cs + sp));

        Vector3 worldCenter = _rt.TransformPoint(cellCenterLocal);
        return RectTransformUtility.WorldToScreenPoint(UICam(), worldCenter);
    }

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}