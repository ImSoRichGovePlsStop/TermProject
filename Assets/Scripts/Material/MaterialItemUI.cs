using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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
    [HideInInspector] public GridUI InputGridUI;
    [HideInInspector] public InventoryUI InventoryUI;

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
    private TextMeshProUGUI _stackText;
    private int _dragRotation;
    private bool _isDragging;
    private Vector2 _lastPointerScreenPos;

    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _ => Color.white
    };

    public void Init(MaterialInstance instance, GridUI envGridUI = null)
    {
        Instance = instance;
        instance.UIElement = this;
        WeaponGridUI = InventoryUI.StaticWeaponGridUI;
        BagGridUI = InventoryUI.StaticBagGridUI;
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
        RefreshStackText();
        instance.OnStackChanged += RefreshStackText;
        _cg.alpha = 0f;
    }

    private void RebuildVisual(int rotation)
    {
        _stackText = null;
        for (int i = _rt.childCount - 1; i >= 0; i--)
            Destroy(_rt.GetChild(i).gameObject);

        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;

        var bound = Instance.Data.GetBoundingSize(rotation);
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp, bound.y * (cs + sp) - sp);

        BuildVisualCells(Instance.Data.GetShapeCells(rotation));
    }

    private void BuildVisualCells(System.Collections.Generic.List<Vector2Int> shapeCells)
    {
        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;
        Color borderColor = RarityColor(Instance.Rarity);

        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
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

            borderRt.sizeDelta = new Vector2(cs + bExtraLeft + bExtraRight, cs + bExtraTop + bExtraBottom);
            borderRt.anchoredPosition = new Vector2(cell.x * (cs + sp) - bExtraLeft, -cell.y * (cs + sp) + bExtraTop);

            borderGo.GetComponent<Image>().color = borderColor;
            borderGo.GetComponent<Image>().raycastTarget = false;
        }

        Vector2Int topRightCell = shapeCells[0];
        foreach (var c in shapeCells)
            if (c.y < topRightCell.y || (c.y == topRightCell.y && c.x > topRightCell.x))
                topRightCell = c;

        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
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

            cellRt.sizeDelta = new Vector2(cs - borderSize * 2f + extraLeft + extraRight, cs - borderSize * 2f + extraTop + extraBottom);
            cellRt.anchoredPosition = new Vector2(cell.x * (cs + sp) + borderSize - extraLeft, -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (Instance.Data.icon != null) cellImg.sprite = Instance.Data.icon;
            cellImg.color = Instance.MaterialData.moduleColor;
            cellImg.raycastTarget = true;

            if (cell == topRightCell)
            {
                var stackGo = new GameObject("StackText", typeof(RectTransform), typeof(TextMeshProUGUI));
                var stackRt = stackGo.GetComponent<RectTransform>();
                stackRt.SetParent(cellRt, false);
                stackRt.anchorMin = new Vector2(0f, 1f);
                stackRt.anchorMax = new Vector2(1f, 1f);
                stackRt.pivot = new Vector2(1f, 1f);
                stackRt.anchoredPosition = new Vector2(-2f, -2f);
                stackRt.sizeDelta = new Vector2(0f, 20f);

                _stackText = stackGo.GetComponent<TextMeshProUGUI>();
                _stackText.fontSize = 24f;
                _stackText.color = Color.white;
                _stackText.alignment = TextAlignmentOptions.TopRight;
                _stackText.raycastTarget = false;
            }
        }
    }

    private void Update()
    {
        if (!_isDragging || Keyboard.current == null || !Keyboard.current[Key.R].wasPressedThisFrame) return;

        var currentShape = Instance.Data.GetShapeCells(_dragRotation);
        _clickedCell = ModuleData.RotateSinglePoint(_clickedCell, 1, currentShape);
        _dragRotation = (_dragRotation + 1) % 4;
        RebuildVisual(_dragRotation);
        RefreshStackText();

        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;
        _dragOffset = new Vector2((_clickedCell.x + 0.5f) * (cs + sp), -(_clickedCell.y + 0.5f) * (cs + sp));

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, Mouse.current.position.ReadValue(), UICam(), out var local))
            _rt.anchoredPosition = local - _dragOffset;

        UpdateHighlightAtScreenPos(Mouse.current.position.ReadValue());
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
        if (InputGridUI != null && Instance.CurrentGrid == InputGridUI.Data)
            _originGrid = InputGridUI;
        else if (EnvGridUI != null && Instance.CurrentGrid == EnvGridUI.Data)
            _originGrid = EnvGridUI;
        else
            _originGrid = BagGridUI;

        _originCell = Instance.GridPosition;
        _originRotation = Instance.Rotation;
        _dragRotation = Instance.Rotation;
        _isDragging = true;
        _lastPointerScreenPos = e.position;
        _clickedCell = GetClickedLocalCell(e);

        _rt.SetParent(_canvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;
        _dragOffset = new Vector2((_clickedCell.x + 0.5f) * (cs + sp), -(_clickedCell.y + 0.5f) * (cs + sp));

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var mouseLocal))
            _rt.anchoredPosition = mouseLocal - _dragOffset;

        _cg.alpha = dragAlpha;
        _cg.blocksRaycasts = false;

        ModuleItemUI.IsDragging = true;
        DiscardGridUI.Instance?.ShowForDrag();
    }

    public void OnDrag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var local))
            _rt.anchoredPosition = local - _dragOffset;

        _lastPointerScreenPos = e.position;
        UpdateHighlight(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _isDragging = false;
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        ModuleItemUI.IsDragging = false;
        ClearHighlights();

        var gridsToCheck = new[] { BagGridUI, EnvGridUI, InputGridUI, DiscardGridUI.Instance?.GridUI };

        foreach (var g in gridsToCheck)
        {
            if (g == null) continue;
            if (!g.ScreenToCell(e.position, UICam(), out var hoveredCell)) continue;

            var existing = g.Data.GetModuleAt(hoveredCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
            {
                int toAdd = Mathf.Min(Instance.StackCount, target.MaxStack - target.StackCount);
                for (int i = 0; i < toAdd; i++) target.AddStack();

                if (toAdd >= Instance.StackCount)
                {
                    _originGrid.Data.Remove(Instance);
                    DiscardGridUI.Instance?.OnDragEnded();
                    Destroy(gameObject);
                }
                else
                {
                    for (int i = 0; i < toAdd; i++) Instance.RemoveStack();
                    _dragRotation = Instance.Rotation;
                    RebuildVisual(_dragRotation);
                    RefreshStackText();
                    SnapToCell(_originGrid, _originCell);
                }
                DiscardGridUI.Instance?.OnDragEnded();
                return;
            }

            var pivot = hoveredCell - _clickedCell;
            var prevGrid = Instance.CurrentGrid;
            var prevPos = Instance.GridPosition;

            prevGrid?.Remove(Instance);
            Instance.SetRotation(_dragRotation);

            if (g.Data.TryPlace(Instance, pivot))
            {
                SnapToCell(g, Instance.GridPosition);
            }
            else
            {
                var blocker = g.Data.BlockingModuleMai(Instance, pivot, _dragRotation);
                if (blocker != null)
                {
                    var oldPos = blocker.GridPosition;
                    g.Data.Remove(blocker);

                    bool placedDrag = g.Data.TryPlace(Instance, pivot);
                    bool placedSwap = placedDrag && prevGrid != null && prevGrid.TryPlace(blocker, prevPos);

                    if (placedDrag && placedSwap)
                    {
                        SnapToCell(g, Instance.GridPosition);
                        if (blocker.UIElement is ModuleItemUI blockerModUI)
                            blockerModUI.SnapToCell(_originGrid, prevPos);
                        else if (blocker.UIElement is MaterialItemUI blockerMatUI)
                            blockerMatUI.SnapToCell(_originGrid, prevPos);
                        DiscardGridUI.Instance?.OnDragEnded();
                        return;
                    }
                    else
                    {
                        if (placedDrag) g.Data.Remove(Instance);
                        g.Data.TryPlace(blocker, oldPos);
                    }
                }

                Instance.SetRotation(_originRotation);
                prevGrid?.TryPlace(Instance, prevPos);
                _dragRotation = Instance.Rotation;
                RebuildVisual(_dragRotation);
                RefreshStackText();
                SnapToCell(_originGrid, _originCell);
            }
            DiscardGridUI.Instance?.OnDragEnded();
            return;
        }

        _dragRotation = Instance.Rotation;
        RebuildVisual(_dragRotation);
        RefreshStackText();
        SnapToCell(_originGrid, _originCell);
        DiscardGridUI.Instance?.OnDragEnded();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Right) return;
        if (Instance.StackCount <= 1) return;
        if (InventoryUI == null) return;

        Instance.RemoveStack();
        var ui = InventoryUI.SpawnSplitMaterial(Instance.MaterialData, BagGridUI);
        if (ui == null) Instance.AddStack();
    }

    public void OnPointerEnter(PointerEventData e) => ModuleTooltipUI.Instance.Show(Instance);
    public void OnPointerExit(PointerEventData e) => ModuleTooltipUI.Instance.Hide();

    private void UpdateHighlight(PointerEventData e)
    {
        _lastPointerScreenPos = e.position;
        UpdateHighlightAtScreenPos(e.position);
    }

    private void UpdateHighlightAtScreenPos(Vector2 screenPos)
    {
        ClearHighlights();

        if (WeaponGridUI.ScreenToCell(screenPos, UICam(), out var weaponCell))
        {
            WeaponGridUI.HighlightCells(Instance.Data, weaponCell - _clickedCell, valid: false, _dragRotation);
            return;
        }

        foreach (var g in new[] { BagGridUI, EnvGridUI, InputGridUI, DiscardGridUI.Instance?.GridUI })
        {
            if (g == null) continue;
            if (!g.ScreenToCell(screenPos, UICam(), out var hoveredCell)) continue;

            var existing = g.Data.GetModuleAt(hoveredCell);
            if (existing is MaterialInstance target && Instance.CanStackOnto(target))
                g.HighlightCells(target.Data, target.GridPosition, valid: true, target.Rotation);
            else
            {
                var pivot = hoveredCell - _clickedCell;
                g.HighlightCells(Instance.Data, pivot, g.Data.CanPlace(Instance, pivot, _dragRotation), _dragRotation);
            }
            return;
        }
    }

    private void ClearHighlights()
    {
        WeaponGridUI?.ClearHighlights();
        BagGridUI?.ClearHighlights();
        EnvGridUI?.ClearHighlights();
        InputGridUI?.ClearHighlights();
        DiscardGridUI.Instance?.GridUI.ClearHighlights();
    }

    private Vector2Int GetClickedLocalCell(PointerEventData e)
    {
        float cs = BagGridUI.CellSize;
        float sp = BagGridUI.CellSpacing;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, UICam(), out var localPoint);
        return new Vector2Int(Mathf.Max(0, Mathf.FloorToInt(localPoint.x / (cs + sp))),
                              Mathf.Max(0, Mathf.FloorToInt(-localPoint.y / (cs + sp))));
    }

    private Camera UICam() => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}