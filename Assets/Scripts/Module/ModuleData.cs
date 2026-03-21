using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleShapeRow
{
    public bool[] cells = new bool[5];
}

[CreateAssetMenu(fileName = "Module_New", menuName = "Inventory/Module")]
public class ModuleData : ScriptableObject
{
    public string moduleName = "New Module";
    public Sprite icon;
    public ModuleEffect moduleEffect;
    public Color moduleColor = Color.white;
    public int[] cost = { 0, 0, 0, 0, 0 };

    [Header("Module Buff")]
    public bool isBuffAdjacent;
    [SerializeField] private ModuleShapeRow[] buffGrid = DefaultShape();

    [Header("Shape (5×5 — tick cells to form the shape)")]
    [SerializeField] private ModuleShapeRow[] shapeGrid = DefaultShape();

    public List<Vector2Int> GetBuffCells()
    {
        int shapeMinX = int.MaxValue, shapeMinY = int.MaxValue;
        for (int row = 0; row < 5; row++)
        {
            if (shapeGrid == null || row >= shapeGrid.Length) continue;
            var r = shapeGrid[row];
            if (r == null) continue;
            for (int col = 0; col < 5; col++)
                if (col < r.cells.Length && r.cells[col])
                {
                    if (col < shapeMinX) shapeMinX = col;
                    if (row < shapeMinY) shapeMinY = row;
                }
        }
        if (shapeMinX == int.MaxValue) { shapeMinX = 0; shapeMinY = 0; }

        var result = new List<Vector2Int>();
        for (int row = 0; row < 5; row++)
        {
            if (buffGrid == null || row >= buffGrid.Length) continue;
            var r = buffGrid[row];
            if (r == null) continue;
            for (int col = 0; col < 5; col++)
                if (col < r.cells.Length && r.cells[col])
                    result.Add(new Vector2Int(col - shapeMinX, row - shapeMinY));
        }
        return result;
    }

    public List<Vector2Int> GetShapeCells()
    {
        var raw = new List<Vector2Int>();
        for (int row = 0; row < 5; row++)
        {
            if (shapeGrid == null || row >= shapeGrid.Length) continue;
            var r = shapeGrid[row];
            if (r == null) continue;
            for (int col = 0; col < 5; col++)
                if (col < r.cells.Length && r.cells[col])
                    raw.Add(new Vector2Int(col, row));
        }
        if (raw.Count == 0) { raw.Add(Vector2Int.zero); return raw; }

        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in raw) { if (c.x < minX) minX = c.x; if (c.y < minY) minY = c.y; }

        var result = new List<Vector2Int>(raw.Count);
        foreach (var c in raw) result.Add(new Vector2Int(c.x - minX, c.y - minY));
        return result;
    }

    public Vector2Int GetBoundingSize()
    {
        int maxX = 0, maxY = 0;
        foreach (var c in GetShapeCells()) { if (c.x > maxX) maxX = c.x; if (c.y > maxY) maxY = c.y; }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    public static List<Vector2Int> RotateCells(List<Vector2Int> cells, int rotation)
    {
        var rotated = new List<Vector2Int>(cells.Count);
        foreach (var c in cells)
        {
            Vector2Int r = rotation switch {
                1 => new Vector2Int( c.y, -c.x),
                2 => new Vector2Int(-c.x, -c.y), 
                3 => new Vector2Int(-c.y,  c.x), 
                _ => c
            };
            rotated.Add(r);
        }
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in rotated) { if (c.x < minX) minX = c.x; if (c.y < minY) minY = c.y; }
        var result = new List<Vector2Int>(rotated.Count);
        foreach (var c in rotated) result.Add(new Vector2Int(c.x - minX, c.y - minY));
        return result;
    }

    public List<Vector2Int> GetShapeCells(int rotation) => RotateCells(GetShapeCells(), rotation);

    public List<Vector2Int> GetBuffCells(int rotation)
    {
        if (rotation == 0) return GetBuffCells();

        var buffCells = GetBuffCells();
        var rotatedBuff = new List<Vector2Int>(buffCells.Count);
        foreach (var c in buffCells)
        {
            rotatedBuff.Add(rotation switch {
                1 => new Vector2Int( c.y, -c.x),
                2 => new Vector2Int(-c.x, -c.y),
                3 => new Vector2Int(-c.y,  c.x),
                _ => c
            });
        }

        var shapeCells = GetShapeCells();
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in shapeCells)
        {
            Vector2Int r = rotation switch {
                1 => new Vector2Int( c.y, -c.x),
                2 => new Vector2Int(-c.x, -c.y),
                3 => new Vector2Int(-c.y,  c.x),
                _ => c
            };
            if (r.x < minX) minX = r.x;
            if (r.y < minY) minY = r.y;
        }
        if (minX == int.MaxValue) { minX = 0; minY = 0; }

        var result = new List<Vector2Int>(rotatedBuff.Count);
        foreach (var c in rotatedBuff)
            result.Add(new Vector2Int(c.x - minX, c.y - minY));
        return result;
    }

    public Vector2Int GetBoundingSize(int rotation)
    {
        int maxX = 0, maxY = 0;
        foreach (var c in GetShapeCells(rotation)) { if (c.x > maxX) maxX = c.x; if (c.y > maxY) maxY = c.y; }
        return new Vector2Int(maxX + 1, maxY + 1);
    }

    private static ModuleShapeRow[] DefaultShape()
    {
        var g = new ModuleShapeRow[5];
        for (int i = 0; i < 5; i++) g[i] = new ModuleShapeRow();
        return g;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (shapeGrid != null && shapeGrid.Length == 5) return;
        var n = new ModuleShapeRow[5];
        for (int i = 0; i < 5; i++)
        {
            n[i] = (shapeGrid != null && i < shapeGrid.Length && shapeGrid[i] != null)
                ? shapeGrid[i] : new ModuleShapeRow();
            if (n[i].cells == null || n[i].cells.Length != 5)
            {
                var old = n[i].cells; n[i].cells = new bool[5];
                if (old != null) for (int j = 0; j < Mathf.Min(old.Length, 5); j++) n[i].cells[j] = old[j];
            }
        }
        shapeGrid = n;
    }
#endif
}