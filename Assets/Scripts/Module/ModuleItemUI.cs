using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ModuleItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public ModuleInstance Instance { get; private set; }

    public static bool IsDragging;
    public static ModuleInstance DraggingInstance;
    public static System.Collections.Generic.HashSet<Vector2Int> DraggingCells = new();
    public static GridData DraggingGrid;

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
    public static bool AnyDragging => IsDragging || MaterialItemUI.AnyMaterialDragging;

    [SerializeField] private bool allowSell = false;

    [HideInInspector] public ShopTooltipUI ShopTooltipUI;
    [HideInInspector] public SellConfirmationUI SellConfirmationUI;


    public void Init(ModuleInstance instance, GridUI envGridUI = null)
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
        _cg.alpha = 0f;
    }

    private void RebuildVisual(int rotation)
    {
        for (int i = _rt.childCount - 1; i >= 0; i--)
            DestroyImmediate(_rt.GetChild(i).gameObject);

        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;

        var bound = Instance.Data.GetBoundingSize(rotation);
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp, bound.y * (cs + sp) - sp);

        BuildBorders(Instance.Data.GetShapeCells(rotation), rotation, bound, cs, sp);

        if (Instance.Data.icon != null)
            BuildIconOverlay(rotation, bound, cs, sp);

        BuildVisualCells(Instance.Data.GetShapeCells(rotation), bound, rotation);
    }

    private void BuildIconOverlay(int rotation, Vector2Int bound, float cs, float sp)
    {
        var iconGo = new GameObject("IconOverlay", typeof(RectTransform), typeof(RawImage));
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.SetParent(_rt, false);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchorMin = new Vector2(0f, 0f);
        iconRt.anchorMax = new Vector2(1f, 1f);
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;

        var origBound = Instance.Data.GetBoundingSize(0);
        float origW = origBound.x * (cs + sp) - sp;
        float origH = origBound.y * (cs + sp) - sp;
        float curW = bound.x * (cs + sp) - sp;
        float curH = bound.y * (cs + sp) - sp;

        var raw = iconGo.GetComponent<RawImage>();
        raw.texture = Instance.Data.icon.texture;
        raw.color = Color.white;
        raw.raycastTarget = false;  // cells underneath handle raycasts

        // Rotate UV rect: rotation=1 CW90 means sample rotated portion of texture
        switch (rotation)
        {
            case 0: raw.uvRect = new Rect(0, 0, 1, 1); break;
            case 1:
                raw.uvRect = new Rect(0, 0, 1, 1);
                iconRt.localRotation = Quaternion.Euler(0, 0, 90); break;
            case 2:
                raw.uvRect = new Rect(0, 0, 1, 1);
                iconRt.localRotation = Quaternion.Euler(0, 0, 180); break;
            case 3:
                raw.uvRect = new Rect(0, 0, 1, 1);
                iconRt.localRotation = Quaternion.Euler(0, 0, -90); break;
        }

        // Fix scale distortion caused by non-square bounding boxes after rotation
        if (rotation == 1 || rotation == 3)
        {
            float scaleX = curH / curW;
            float scaleY = curW / curH;
            iconRt.localScale = new Vector3(scaleX, scaleY, 1f);
        }

        // Place after all border children (borders are built first in BuildVisualCells)
        // border count = shapeCells.Count, so insert after them
        int borderCount = Instance.Data.GetShapeCells(rotation).Count;
        iconGo.transform.SetSiblingIndex(borderCount);
    }



    private void BuildBorders(System.Collections.Generic.List<Vector2Int> shapeCells, int rotation, Vector2Int bound, float cs, float sp)
    {
        if (Instance.Data.icon != null)
        {
            var borderGo = new GameObject("BorderGlow", typeof(RectTransform), typeof(RawImage));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(_rt, false);
            borderRt.pivot = new Vector2(0.5f, 0.5f);
            borderRt.anchorMin = new Vector2(0f, 0f);
            borderRt.anchorMax = new Vector2(1f, 1f);
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;

            switch (rotation)
            {
                case 1: borderRt.localRotation = Quaternion.Euler(0, 0, 90); break;
                case 2: borderRt.localRotation = Quaternion.Euler(0, 0, 180); break;
                case 3: borderRt.localRotation = Quaternion.Euler(0, 0, -90); break;
            }

            if (rotation == 1 || rotation == 3)
            {
                float curW = bound.x * (cs + sp) - sp;
                float curH = bound.y * (cs + sp) - sp;
                borderRt.localScale = new Vector3(curH / curW, curW / curH, 1f);
            }

            var raw = borderGo.GetComponent<RawImage>();
            var tex = Instance.Data.icon.texture;
            // Use pixels-per-cell ratio for consistent thickness across different texture sizes
            float pixelsPerUnit = tex.width / (WeaponGridUI.CellSize * bound.x);
            int thickness = Mathf.Max(1, Mathf.RoundToInt(borderSize * pixelsPerUnit));

            var outlineTex = SpriteOutlineUtility.GetOrCreate(tex, SpriteOutlineUtility.RarityColor(Instance.Rarity), thickness);

            raw.texture = outlineTex;
            raw.color = Color.white;
            raw.uvRect = new Rect(0, 0, 1, 1);
            raw.raycastTarget = false;
        }
        else
        {
            Color borderColor = SpriteOutlineUtility.RarityColor(Instance.Rarity);
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

                var borderImg = borderGo.GetComponent<Image>();
                borderImg.color = borderColor;
                borderImg.raycastTarget = false;
            }
        }
    }


    private void BuildVisualCells(System.Collections.Generic.List<Vector2Int> shapeCells, Vector2Int bound, int rotation)
    {
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;

        Vector2Int topRightCell = shapeCells[0];
        foreach (var c in shapeCells)
            if (c.y < topRightCell.y || (c.y == topRightCell.y && c.x > topRightCell.x))
                topRightCell = c;

        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}", typeof(RectTransform));
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

            if (Instance.Data.icon != null)
            {
                // Icon is rendered by BuildIconOverlay as a single rotated overlay
                // Each cell still needs a raycast target
                var cellImg = go.AddComponent<Image>();
                cellImg.color = Color.clear;
                cellImg.raycastTarget = true;
            }
            else
            {
                var cellImg = go.AddComponent<Image>();
                cellImg.color = Instance.Data.moduleColor;
                cellImg.raycastTarget = true;
            }

            if (cell == topRightCell && Instance.Level > 0)
            {
                var textGo = new GameObject("LevelText", typeof(RectTransform), typeof(TextMeshProUGUI));
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.SetParent(_rt, false);
                textRt.anchorMin = new Vector2(0f, 1f);
                textRt.anchorMax = new Vector2(0f, 1f);
                textRt.pivot = new Vector2(1f, 1f);
                float cellRight = (cell.x + 1) * (cs + sp) - sp;
                float cellTop = -cell.y * (cs + sp);
                textRt.anchoredPosition = new Vector2(cellRight - 2f, cellTop - 2f);
                textRt.sizeDelta = new Vector2(cs, 20f);
                textGo.transform.SetAsLastSibling();

                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                tmp.text = $"+{Instance.Level}";
                tmp.fontSize = 24f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.TopRight;
                tmp.raycastTarget = false;
            }
        }
    }

    private RawImage[] _pulsingRawImages;
    private Image[] _pulsingImages;
    private Color[] _pulsingImageOriginals;

    public void PlaySpawnPulse()
    {
        StopAllCoroutines();
        _pulsingRawImages = GetComponentsInChildren<RawImage>(true);
        _pulsingImages = GetComponentsInChildren<Image>(true);
        _pulsingImageOriginals = new Color[_pulsingImages.Length];
        for (int i = 0; i < _pulsingImages.Length; i++)
            _pulsingImageOriginals[i] = _pulsingImages[i].color;
        StartCoroutine(SpawnPulseCoroutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (_pulsingRawImages != null)
        {
            foreach (var raw in _pulsingRawImages)
                if (raw != null) raw.color = Color.white;
            _pulsingRawImages = null;
        }
        if (_pulsingImages != null && _pulsingImageOriginals != null)
        {
            for (int i = 0; i < _pulsingImages.Length; i++)
                if (_pulsingImages[i] != null)
                    _pulsingImages[i].color = _pulsingImageOriginals[i];
            _pulsingImages = null;
            _pulsingImageOriginals = null;
        }
    }

    private IEnumerator SpawnPulseCoroutine()
    {
        yield return null;
        var rawImages = GetComponentsInChildren<RawImage>(true);
        var images = GetComponentsInChildren<Image>(true);
        _pulsingRawImages = rawImages;
        var rawOriginals = new Color[rawImages.Length];
        for (int i = 0; i < rawImages.Length; i++)
            rawOriginals[i] = rawImages[i].color;
        var imgOriginals = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
            imgOriginals[i] = images[i].color;
        float pulseDuration = 0.8f;
        int pulseCount = 2;
        float pulseStrength = 0.7f;
        Color pulseColor = Color.white;

        var rawTargets = new Color[rawImages.Length];
        for (int i = 0; i < rawImages.Length; i++)
            rawTargets[i] = Color.Lerp(rawOriginals[i], pulseColor, pulseStrength);
        var imgTargets = new Color[images.Length];
        for (int i = 0; i < images.Length; i++)
            imgTargets[i] = Color.Lerp(imgOriginals[i], pulseColor, pulseStrength);

        for (int p = 0; p < pulseCount; p++)
        {
            float t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                float lerp = t / pulseDuration;
                for (int i = 0; i < rawImages.Length; i++)
                    if (rawImages[i] != null) rawImages[i].color = Color.Lerp(rawOriginals[i], rawTargets[i], lerp);
                for (int i = 0; i < images.Length; i++)
                    if (images[i] != null && images[i].color != Color.clear)
                        images[i].color = Color.Lerp(imgOriginals[i], imgTargets[i], lerp);
                yield return null;
            }
            t = 0f;
            while (t < pulseDuration)
            {
                t += Time.deltaTime;
                float lerp = t / pulseDuration;
                for (int i = 0; i < rawImages.Length; i++)
                    if (rawImages[i] != null) rawImages[i].color = Color.Lerp(rawTargets[i], rawOriginals[i], lerp);
                for (int i = 0; i < images.Length; i++)
                    if (images[i] != null && images[i].color != Color.clear)
                        images[i].color = Color.Lerp(imgTargets[i], imgOriginals[i], lerp);
                yield return null;
            }
        }
        for (int i = 0; i < rawImages.Length; i++)
            if (rawImages[i] != null) rawImages[i].color = rawOriginals[i];
        for (int i = 0; i < images.Length; i++)
            if (images[i] != null && images[i].color != Color.clear)
                images[i].color = imgOriginals[i];
    }

    public void RefreshAfterUpgrade()
    {
        RebuildVisual(Instance.Rotation);
        _cg.alpha = 1f;
    }

    private void Update()
    {
        if (!_isDragging || Keyboard.current == null || !Keyboard.current[Key.R].wasPressedThisFrame) return;

        var currentShape = Instance.Data.GetShapeCells(_dragRotation);
        _clickedCell = ModuleData.RotateSinglePoint(_clickedCell, 1, currentShape);
        _dragRotation = (_dragRotation + 1) % 4;
        RebuildVisual(_dragRotation);

        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;
        _dragOffset = new Vector2((_clickedCell.x + 0.5f) * (cs + sp), -(_clickedCell.y + 0.5f) * (cs + sp));

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, Mouse.current.position.ReadValue(), UICam(), out var local))
            _rt.anchoredPosition = local - _dragOffset;

        UpdateHighlightInternal();
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
        else if (EnvGridUI != null && Instance.CurrentGrid == EnvGridUI.Data)
            _originGrid = EnvGridUI;
        else if (InputGridUI != null && Instance.CurrentGrid == InputGridUI.Data)
            _originGrid = InputGridUI;
        else
            _originGrid = BagGridUI;

        _originCell = Instance.GridPosition;
        _originRotation = Instance.Rotation;
        _dragRotation = Instance.Rotation;
        _isDragging = true;
        ModuleTooltipUI.Instance?.Hide();
        _clickedCell = GetClickedLocalCell(e);

        _rt.SetParent(_canvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;
        _dragOffset = new Vector2((_clickedCell.x + 0.5f) * (cs + sp), -(_clickedCell.y + 0.5f) * (cs + sp));

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var mouseLocal))
            _rt.anchoredPosition = mouseLocal - _dragOffset;

        _cg.alpha = dragAlpha;
        _cg.blocksRaycasts = false;

        ModuleItemUI.IsDragging = true;
        ModuleItemUI.DraggingInstance = Instance;
        ModuleItemUI.DraggingCells = new System.Collections.Generic.HashSet<Vector2Int>(Instance.GetAbsoluteCells());
        ModuleItemUI.DraggingGrid = Instance.CurrentGrid;
        if (!UIManager.IsRightPanelOpen) DiscardGridUI.Instance?.ShowForDrag();
    }

    public void OnDrag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRt, e.position, UICam(), out var local))
            _rt.anchoredPosition = local - _dragOffset;
        UpdateHighlight(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _isDragging = false;
        _cg.alpha = 1f;
        _cg.blocksRaycasts = true;
        ModuleItemUI.IsDragging = false;
        ModuleItemUI.DraggingInstance = null;
        ModuleItemUI.DraggingCells.Clear();
        ModuleItemUI.DraggingGrid = null;
        ClearHighlights();
        GridUI targetGrid = null;
        Vector2Int pivot = Vector2Int.zero;
        Vector2 sampleScreen = GetClickedCellCenterScreen();

        GridUI[] gridsToScan = MergeUI.IsMergeOpen
            ? new[] { InputGridUI, BagGridUI, WeaponGridUI, EnvGridUI, (DiscardGridUI.Instance != null && DiscardGridUI.Instance.IsVisible ? DiscardGridUI.Instance.GridUI : null) }
            : new[] { WeaponGridUI, BagGridUI, EnvGridUI, InputGridUI, (DiscardGridUI.Instance != null && DiscardGridUI.Instance.IsVisible ? DiscardGridUI.Instance.GridUI : null) };

        foreach (var g in gridsToScan)
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

            bool weaponBlocked = targetGrid == WeaponGridUI
                && targetGrid.Data == InventoryManager.Instance.WeaponGrid
                && !InventoryManager.Instance.CanPlaceInWeaponGrid(Instance, pivot, _dragRotation);

            if (weaponBlocked)
            {
                Instance.SetRotation(_originRotation);
                prevGrid?.TryPlace(Instance, prevPos);
            }
            else if (targetGrid.Data.TryPlace(Instance, pivot))
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

        SnapToCell(moved ? targetGrid : _originGrid, moved ? Instance.GridPosition : _originCell);
        DiscardGridUI.Instance?.OnDragEnded();
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (AnyDragging) return;

        if (ShopTooltipUI != null)
        {
            int price = Instance.Data.cost[(int)Instance.Rarity];
            ShopTooltipUI.ShowModule(Instance, price);
        }
        else
        {
            ModuleTooltipUI.Instance.Show(Instance, WeaponGridUI, BagGridUI, EnvGridUI, DiscardGridUI.Instance, InputGridUI);
        }
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (AnyDragging) return;
        if (ShopTooltipUI != null) ShopTooltipUI.Hide();
        else ModuleTooltipUI.Instance.Hide();
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

        GridUI[] gridsToHighlight = MergeUI.IsMergeOpen
            ? new[] { InputGridUI, BagGridUI, WeaponGridUI, EnvGridUI, (DiscardGridUI.Instance != null && DiscardGridUI.Instance.IsVisible ? DiscardGridUI.Instance.GridUI : null) }
            : new[] { WeaponGridUI, BagGridUI, EnvGridUI, InputGridUI, (DiscardGridUI.Instance != null && DiscardGridUI.Instance.IsVisible ? DiscardGridUI.Instance.GridUI : null) };

        foreach (var g in gridsToHighlight)
        {
            if (g == null) continue;
            if (g.ScreenToCell(sampleScreen, UICam(), out var hoveredCell))
            {
                var pivot = hoveredCell - _clickedCell;
                bool canPlace = (g == WeaponGridUI && g.Data == InventoryManager.Instance.WeaponGrid)
                    ? InventoryManager.Instance.CanPlaceInWeaponGrid(Instance, pivot, _dragRotation)
                    : g.Data.CanPlace(Instance, pivot, _dragRotation);
                g.HighlightCells(Instance.Data, pivot, canPlace, _dragRotation);
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
        if (DiscardGridUI.Instance != null && DiscardGridUI.Instance.IsVisible) DiscardGridUI.Instance.GridUI?.ClearHighlights();
    }

    private Vector2Int GetClickedLocalCell(PointerEventData e)
    {
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_rt, e.position, UICam(), out var localPoint);
        return new Vector2Int(Mathf.Max(0, Mathf.FloorToInt(localPoint.x / (cs + sp))),
                              Mathf.Max(0, Mathf.FloorToInt(-localPoint.y / (cs + sp))));
    }

    private Vector2 GetClickedCellCenterScreen()
    {
        float cs = WeaponGridUI.CellSize;
        float sp = WeaponGridUI.CellSpacing;
        Vector2 cellCenterLocal = new Vector2((_clickedCell.x + 0.5f) * (cs + sp), -(_clickedCell.y + 0.5f) * (cs + sp));
        return RectTransformUtility.WorldToScreenPoint(UICam(), _rt.TransformPoint(cellCenterLocal));
    }

    private Camera UICam() => _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}