using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCellUI : MonoBehaviour
{
    public Vector2Int GridCoord { get; private set; }
    public GridUI ParentGrid { get; private set; }

    
    private Color colorEmpty = new Color(0.15f, 0.15f, 0.20f, 1f);
    private Color colorOccupied = new Color(0.25f, 0.25f, 0.35f, 1f);
    private Color colorValid = new Color(0.3f, 0.8f, 0.3f, 0.6f);
    private Color colorInvalid = new Color(0.8f, 0.3f, 0.3f, 0.6f);

    private Color colorLocked = new Color(0.65f, 0.72f, 0.80f, 0.25f);
    private Color colorUnused = new Color(0.65f, 0.72f, 0.80f, 0.25f);

    private Image _img;

    public enum State { Empty, Occupied, Valid, Invalid, Locked, Unused }

    public void Init(Vector2Int coord, GridUI parent)
    {
        GridCoord = coord;
        ParentGrid = parent;
        _img = GetComponent<Image>();
        SetState(State.Empty);
    }

    public void SetState(State s)
    {
        if (_img == null) _img = GetComponent<Image>();
        _img.color = s switch
        {
            State.Empty => colorEmpty,
            State.Occupied => colorOccupied,
            State.Valid => colorValid,
            State.Invalid => colorInvalid,
            State.Locked => colorLocked,
            State.Unused => colorUnused,
            _ => colorEmpty
        };
    }

    public void Refresh(GridData grid)
        => SetState(grid.GetModuleAt(GridCoord) != null ? State.Occupied : State.Empty);

    public void RefreshWeapon(GridData grid, int unlockedCols, int unlockedRows)
    {
        bool unlocked = GridCoord.x < unlockedCols && GridCoord.y < unlockedRows;
        if (!unlocked) { SetState(State.Locked); return; }
        SetState(grid.GetModuleAt(GridCoord) != null ? State.Occupied : State.Empty);
    }

    public void SetColor(Color color)
    {
        if (_img == null) _img = GetComponent<Image>();
        _img.color = color;
    }
}