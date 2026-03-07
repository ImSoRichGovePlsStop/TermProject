using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class ModuleItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ModuleInstance Instance { get; private set; }

    [HideInInspector] public GridUI WeaponGridUI;
    [HideInInspector] public GridUI BagGridUI;

    [SerializeField] private float dragAlpha = 0.6f;

    private RectTransform _rt;
    private RectTransform _canvasRt;
    private CanvasGroup   _cg;
    private Canvas        _canvas;
    private GridUI        _originGrid;
    private Vector2Int    _originCell;

    public void Init(ModuleInstance instance, GridUI weaponGridUI, GridUI bagGridUI)
    {
        Instance           = instance;
        instance.UIElement = this;
        WeaponGridUI       = weaponGridUI;
        BagGridUI          = bagGridUI;

        _rt       = GetComponent<RectTransform>();
        _cg       = GetComponent<CanvasGroup>();
        _canvas   = GetComponentInParent<Canvas>();
        _canvasRt = _canvas.GetComponent<RectTransform>();

        _rt.pivot = new Vector2(0f, 1f);

        var bound = instance.Data.GetBoundingSize();
        float cs  = weaponGridUI.cellSize;
        float sp  = weaponGridUI.cellSpacing;
        _rt.sizeDelta = new Vector2(bound.x * (cs + sp) - sp,
                                    bound.y * (cs + sp) - sp);

        // Main image ใช้สำหรับ raycast/drag เท่านั้น — ซ่อน visual ไว้
        var img = GetComponent<Image>();
        img.color = Color.clear;

        // สร้าง child image ตาม shape จริง (ไม่ใช่ bounding box)
        foreach (var cell in instance.Data.GetShapeCells())
        {
            var go     = new GameObject($"cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
            var cellRt = go.GetComponent<RectTransform>();
            cellRt.SetParent(_rt, false);
            cellRt.pivot        = new Vector2(0f, 1f);
            cellRt.anchorMin    = new Vector2(0f, 1f);
            cellRt.anchorMax    = new Vector2(0f, 1f);
            cellRt.sizeDelta    = new Vector2(cs, cs);
            cellRt.anchoredPosition = new Vector2(cell.x * (cs + sp), -cell.y * (cs + sp));

            var cellImg = go.GetComponent<Image>();
            if (instance.Data.icon != null) cellImg.sprite = instance.Data.icon;
            cellImg.raycastTarget = false; // ให้ parent จัดการ drag แทน
        }
    }

    public void SnapToCell(GridUI gridUI, Vector2Int cell)
    {
        var parent = gridUI.transform.parent as RectTransform;
        _rt.SetParent(parent, worldPositionStays: false);

        Vector3 worldPos = gridUI.GetCellWorldTopLeft(cell);
        Vector3 localPos = parent.InverseTransformPoint(worldPos);
        _rt.anchoredPosition = new Vector2(localPos.x, localPos.y);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        _originGrid = Instance.CurrentGrid == InventoryManager.Instance.WeaponGrid
            ? WeaponGridUI : BagGridUI;
        _originCell = Instance.GridPosition;

        _rt.SetParent(_canvas.transform, worldPositionStays: true);
        _rt.SetAsLastSibling();

        _cg.alpha          = dragAlpha;
        _cg.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRt, e.position, UICam(), out var local))
            _rt.anchoredPosition = local;

        UpdateHighlight(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        _cg.alpha          = 1f;
        _cg.blocksRaycasts = true;
        ClearHighlights();

        GridUI     targetGrid = null;
        Vector2Int pivot      = Vector2Int.zero;

        foreach (var g in new[] { WeaponGridUI, BagGridUI })
            if (g.ScreenToCell(e.position, UICam(), out var c)) { targetGrid = g; pivot = c; break; }

        bool moved = targetGrid != null &&
                     InventoryManager.Instance.TryMoveModule(Instance, targetGrid.Data, pivot);

        var snapGrid = moved ? targetGrid : _originGrid;
        var snapCell = moved ? Instance.GridPosition : _originCell;

        SnapToCell(snapGrid, snapCell);
    }

    private void UpdateHighlight(PointerEventData e)
    {
        ClearHighlights();
        foreach (var g in new[] { WeaponGridUI, BagGridUI })
            if (g.ScreenToCell(e.position, UICam(), out var c))
            { g.HighlightCells(Instance.Data, c, g.Data.CanPlace(Instance, c)); return; }
    }

    private void ClearHighlights()
    {
        WeaponGridUI?.ClearHighlights();
        BagGridUI?.ClearHighlights();
    }

    private Camera UICam() =>
        _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
}