using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GridUI : MonoBehaviour
{
    [SerializeField] public GridCellUI cellPrefab;

    public float CellSize    { get; private set; }
    public float CellSpacing { get; private set; }
    public GridData Data { get; private set; }

    private GridCellUI[,]   _cells;
    private GridLayoutGroup _layout;
    private List<GameObject> _buffHighlights = new List<GameObject>();

    public void Init(GridData data, float cellSize, float cellSpacing)
    {
        Data        = data;
        CellSize    = cellSize;
        CellSpacing = cellSpacing;

        _layout = GetComponent<GridLayoutGroup>() ?? gameObject.AddComponent<GridLayoutGroup>();
        _layout.cellSize        = new Vector2(cellSize, cellSize);
        _layout.spacing         = new Vector2(cellSpacing, cellSpacing);
        _layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        _layout.constraintCount = data.Width;
        _layout.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        _layout.startAxis       = GridLayoutGroup.Axis.Horizontal;
        _layout.childAlignment  = TextAnchor.UpperLeft;

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(data.Width  * (CellSize + CellSpacing) - CellSpacing,
                                   data.Height * (CellSize + CellSpacing) - CellSpacing);
        BuildCells();

        data.OnModulePlaced  += _ => RefreshAll();
        data.OnModuleRemoved += _ => RefreshAll();
    }

    private void BuildCells()
    {
        foreach (Transform t in transform) Destroy(t.gameObject);
        _cells = new GridCellUI[Data.Width, Data.Height];

        for (int row = 0; row < Data.Height; row++)
            for (int col = 0; col < Data.Width; col++)
            {
                var cell = Instantiate(cellPrefab, transform);
                cell.name = $"Cell_{col}_{row}";
                cell.Init(new Vector2Int(col, row), this);
                _cells[col, row] = cell;
            }
    }

    public Vector3 GetCellWorldTopLeft(Vector2Int coord)
    {
        if (_cells == null || !Data.IsInBounds(coord)) return transform.position;
        var corners = new Vector3[4];
        _cells[coord.x, coord.y].GetComponent<RectTransform>().GetWorldCorners(corners);
        return corners[1];
    }

    public bool ScreenToCell(Vector2 screenPos, Camera uiCam, out Vector2Int coord)
    {
        coord = Vector2Int.zero;
        var rt = GetComponent<RectTransform>();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPos, uiCam, out var local))
            return false;

        int col = Mathf.FloorToInt((local.x - rt.rect.xMin) / (CellSize + CellSpacing));
        int row = Mathf.FloorToInt((rt.rect.yMax - local.y) / (CellSize + CellSpacing));

        if (col < 0 || col >= Data.Width || row < 0 || row >= Data.Height) return false;
        coord = new Vector2Int(col, row);
        return true;
    }

    public void HighlightCells(ModuleData data, Vector2Int pivot, bool valid)
    {
        RefreshAll();
        foreach (var c in Data.GetAbsoluteCells(data, pivot))
        {
            if (!Data.IsInBounds(c)) continue;
            _cells[c.x, c.y].SetState(valid ? GridCellUI.State.Valid : GridCellUI.State.Invalid);
        }
    }

    public void HighlightBuffCells(ModuleInstance inst, Color color)
    {
        ClearBuffHighlights();
        Color buffColor = new Color(color.r, color.g, color.b, 0.3f);
        var canvas = GetComponentInParent<Canvas>();
        var canvasRt = canvas.GetComponent<RectTransform>();

        foreach (var cell in inst.GetAbsoluteBuffCells())
        {
            if (!Data.IsInBounds(cell)) continue;

            var overlayGo = new GameObject("BuffHighlight", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(canvasRt, false);
            overlayGo.transform.SetAsLastSibling();

            var overlayRt = overlayGo.GetComponent<RectTransform>();
            overlayRt.pivot = new Vector2(0f, 1f);
            overlayRt.anchorMin = new Vector2(0f, 0f);
            overlayRt.anchorMax = new Vector2(0f, 0f);
            overlayRt.sizeDelta = new Vector2(CellSize, CellSize);

            Vector3 worldPos = GetCellWorldTopLeft(cell);
            Vector3 localPos = canvasRt.InverseTransformPoint(worldPos);
            overlayRt.anchoredPosition = new Vector2(
                localPos.x + canvasRt.rect.width * 0.5f,
                localPos.y + canvasRt.rect.height * 0.5f
            );

            var overlayImg = overlayGo.GetComponent<Image>();
            overlayImg.color = buffColor;
            overlayImg.raycastTarget = false;

            _buffHighlights.Add(overlayGo);
        }
    }

    public void ClearBuffHighlights()
    {
        foreach (var go in _buffHighlights)
            if (go != null) Destroy(go);
        _buffHighlights.Clear();
    }

    public void ClearHighlights() => RefreshAll();

    public void RefreshAll()
    {
        if (_cells == null) return;
        for (int col = 0; col < Data.Width; col++)
            for (int row = 0; row < Data.Height; row++)
                _cells[col, row].Refresh(Data);
    }
}