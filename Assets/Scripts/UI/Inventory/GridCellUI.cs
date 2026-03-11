using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GridCellUI : MonoBehaviour
{
    public Vector2Int GridCoord  { get; private set; }
    public GridUI     ParentGrid { get; private set; }

    [SerializeField] private Color colorEmpty    = new Color(0.15f, 0.15f, 0.20f, 1f);
    [SerializeField] private Color colorOccupied = new Color(0.25f, 0.25f, 0.35f, 1f);
    [SerializeField] private Color colorValid    = new Color(0.3f,  0.8f,  0.3f,  0.6f);
    [SerializeField] private Color colorInvalid  = new Color(0.8f,  0.3f,  0.3f,  0.6f);

    private Image _img;

    public enum State { Empty, Occupied, Valid, Invalid }

    public void Init(Vector2Int coord, GridUI parent)
    {
        GridCoord  = coord;
        ParentGrid = parent;
        _img       = GetComponent<Image>();
        SetState(State.Empty);
    }

    public void SetState(State s)
    {
        if (_img == null) _img = GetComponent<Image>();
        _img.color = s switch
        {
            State.Empty    => colorEmpty,
            State.Occupied => colorOccupied,
            State.Valid    => colorValid,
            State.Invalid  => colorInvalid,
            _              => colorEmpty
        };
    }

    public void Refresh(GridData grid)
        => SetState(grid.GetModuleAt(GridCoord) != null ? State.Occupied : State.Empty);

    public void SetColor(Color color)
    {
        if (_img == null) _img = GetComponent<Image>();
        _img.color = color;
    }
}