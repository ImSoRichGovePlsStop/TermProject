using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ModuleShapeRow
{
    public bool[] cells = new bool[5];
}

public enum BuffCellType { None, Body, Aura }

[System.Serializable]
public class BuffShapeRow
{
    public BuffCellType[] cells = new BuffCellType[5];
}

[CreateAssetMenu(fileName = "Module_New", menuName = "Inventory/Module")]
public class ModuleData : ScriptableObject
{
    public string moduleName = "New Module";
    public Sprite icon;
    public ModuleEffect moduleEffect;

    [Header("Appearance")]
    public Color moduleColor = Color.cyan;

    [Header("Buff")]
    [Tooltip("Untick to make this module immune to aura level-up buffs.")]
    public bool canBeBuffed = true;

    [SerializeField] private ModuleShapeRow[] shapeGrid = DefaultShape();
    [SerializeField] private BuffShapeRow[] auraGrid = DefaultAuraGrid();

    //Shape

    public List<Vector2Int> GetShapeCells()
    {
        var raw = new List<Vector2Int>();

        if (moduleEffect is LevelUpBuffEffect)
        {
            // Body cells from the aura grid are the physical footprint
            for (int row = 0; row < 5; row++)
            {
                if (auraGrid == null || row >= auraGrid.Length) continue;
                var r = auraGrid[row]; if (r == null) continue;
                for (int col = 0; col < 5; col++)
                    if (col < r.cells.Length && r.cells[col] == BuffCellType.Body)
                        raw.Add(new Vector2Int(col, row));
            }
        }
        else
        {
            for (int row = 0; row < 5; row++)
            {
                if (shapeGrid == null || row >= shapeGrid.Length) continue;
                var r = shapeGrid[row]; if (r == null) continue;
                for (int col = 0; col < 5; col++)
                    if (col < r.cells.Length && r.cells[col])
                        raw.Add(new Vector2Int(col, row));
            }
        }

        if (raw.Count == 0) { raw.Add(Vector2Int.zero); return raw; }

        // Normalize to (0,0) origin
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

    // ── Aura ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns aura cell positions normalized to the same (0,0) origin as
    /// GetShapeCells(), so GridPosition + auraCell = correct world position.
    /// </summary>
    public List<Vector2Int> GetAuraCells()
    {
        // Find the top-left Body cell — this is the same origin GetShapeCells uses
        int minX = int.MaxValue, minY = int.MaxValue;
        if (auraGrid != null)
            for (int row = 0; row < 5; row++)
            {
                if (row >= auraGrid.Length) continue;
                var r = auraGrid[row]; if (r == null) continue;
                for (int col = 0; col < 5; col++)
                    if (col < r.cells.Length && r.cells[col] == BuffCellType.Body)
                    { if (col < minX) minX = col; if (row < minY) minY = row; }
            }

        // If no Body cell exists fall back to (0,0)
        if (minX == int.MaxValue) { minX = 0; minY = 0; }

        var list = new List<Vector2Int>();
        if (auraGrid == null) return list;
        for (int row = 0; row < 5; row++)
        {
            if (row >= auraGrid.Length) continue;
            var r = auraGrid[row]; if (r == null) continue;
            for (int col = 0; col < 5; col++)
                if (col < r.cells.Length && r.cells[col] == BuffCellType.Aura)
                    list.Add(new Vector2Int(col - minX, row - minY));
        }
        return list;
    }

    public BuffCellType GetAuraCell(int col, int row)
    {
        if (auraGrid == null || row < 0 || row >= auraGrid.Length) return BuffCellType.None;
        var r = auraGrid[row];
        if (r == null || col < 0 || col >= r.cells.Length) return BuffCellType.None;
        return r.cells[col];
    }

    // ── Defaults ──────────────────────────────────────────────────────────────

    private static ModuleShapeRow[] DefaultShape()
    {
        var g = new ModuleShapeRow[5];
        for (int i = 0; i < 5; i++) g[i] = new ModuleShapeRow();
        g[0].cells[0] = true;
        return g;
    }

    private static BuffShapeRow[] DefaultAuraGrid()
    {
        var g = new BuffShapeRow[5];
        for (int i = 0; i < 5; i++) g[i] = new BuffShapeRow();
        g[1].cells[1] = BuffCellType.Aura; g[1].cells[2] = BuffCellType.Aura; g[1].cells[3] = BuffCellType.Aura;
        g[2].cells[1] = BuffCellType.Aura; g[2].cells[2] = BuffCellType.Body; g[2].cells[3] = BuffCellType.Aura;
        g[3].cells[1] = BuffCellType.Aura; g[3].cells[2] = BuffCellType.Aura; g[3].cells[3] = BuffCellType.Aura;
        return g;
    }

#if UNITY_EDITOR
    private void OnValidate() { EnsureShapeGrid(); EnsureAuraGrid(); }

    private void EnsureShapeGrid()
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

    private void EnsureAuraGrid()
    {
        if (auraGrid != null && auraGrid.Length == 5) return;
        var n = new BuffShapeRow[5];
        for (int i = 0; i < 5; i++)
        {
            n[i] = (auraGrid != null && i < auraGrid.Length && auraGrid[i] != null)
                ? auraGrid[i] : new BuffShapeRow();
            if (n[i].cells == null || n[i].cells.Length != 5)
            {
                var old = n[i].cells; n[i].cells = new BuffCellType[5];
                if (old != null) for (int j = 0; j < Mathf.Min(old.Length, 5); j++) n[i].cells[j] = old[j];
            }
        }
        auraGrid = n;
    }
#endif
}