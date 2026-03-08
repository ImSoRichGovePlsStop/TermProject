using System.Collections.Generic;
using UnityEngine;

public class ModuleInstance
{
    public ModuleData  Data         { get; private set; }
    public Vector2Int  GridPosition { get; private set; }
    public GridData    CurrentGrid  { get; private set; }
    public ModuleItemUI UIElement   { get; set; }

    public ModuleInstance(ModuleData data)
    {
        Data = data;
    }

    internal void OnPlaced(GridData grid, Vector2Int position)
    {
        CurrentGrid  = grid;
        GridPosition = position;
    }

    internal void OnRemoved()
    {
        CurrentGrid  = null;
        GridPosition = Vector2Int.zero;
    }

    public List<Vector2Int> GetAbsoluteCells()
    {
        var relative = Data.GetShapeCells();
        var absolute = new List<Vector2Int>(relative.Count);
        foreach (var c in relative) absolute.Add(GridPosition + c);
        return absolute;
    }
}