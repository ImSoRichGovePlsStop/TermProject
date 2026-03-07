using UnityEngine;
using UnityEngine.UI;

public class GridUI : MonoBehaviour
{
    [SerializeField] public GridCellUI cellPrefab;
    [SerializeField] public float cellSize    = 64f;
    [SerializeField] public float cellSpacing = 2f;

    public GridData Data { get; private set; }

    private GridCellUI[,]   _cells;
    private GridLayoutGroup _layout;

    public void Init(GridData data)
    {
        Data = data;

        _layout = GetComponent<GridLayoutGroup>() ?? gameObject.AddComponent<GridLayoutGroup>();
        _layout.cellSize        = new Vector2(cellSize, cellSize);
        _layout.spacing         = new Vector2(cellSpacing, cellSpacing);
        _layout.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        _layout.constraintCount = data.Width;
        _layout.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        _layout.startAxis       = GridLayoutGroup.Axis.Horizontal;
        _layout.childAlignment  = TextAnchor.UpperLeft;

        var rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(data.Width  * (cellSize + cellSpacing) - cellSpacing,
                                   data.Height * (cellSize + cellSpacing) - cellSpacing);
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

        int col = Mathf.FloorToInt((local.x - rt.rect.xMin) / (cellSize + cellSpacing));
        int row = Mathf.FloorToInt((rt.rect.yMax - local.y) / (cellSize + cellSpacing));

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

    public void ClearHighlights() => RefreshAll();

    public void RefreshAll()
    {
        if (_cells == null) return;
        for (int col = 0; col < Data.Width; col++)
            for (int row = 0; row < Data.Height; row++)
                _cells[col, row].Refresh(Data);
    }
}