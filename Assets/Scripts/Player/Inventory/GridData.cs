using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure C# grid model — ไม่มี MonoBehaviour
/// จัดการ place / remove / validate / adjacency
/// Coordinates: col = x (→), row = y (↓), origin = top-left
/// </summary>
public class GridData
{
    public int  Width        { get; private set; }
    public int  Height       { get; private set; }
    public bool IsWeaponGrid { get; private set; }

    private ModuleInstance[,]            _cells;
    private readonly HashSet<ModuleInstance> _placed = new HashSet<ModuleInstance>();

    public event System.Action<ModuleInstance> OnModulePlaced;
    public event System.Action<ModuleInstance> OnModuleRemoved;

    public GridData(int width, int height, bool isWeaponGrid)
    {
        IsWeaponGrid = isWeaponGrid;
        Resize(width, height);
    }

    // ── Resize ────────────────────────────────

    public void Resize(int newW, int newH, List<ModuleInstance> evicted = null)
    {
        var newCells = new ModuleInstance[newW, newH];
        var toEvict  = new HashSet<ModuleInstance>();

        if (_cells != null)
            for (int c = 0; c < Width; c++)
                for (int r = 0; r < Height; r++)
                {
                    var inst = _cells[c, r];
                    if (inst == null) continue;
                    if (c < newW && r < newH) newCells[c, r] = inst;
                    else toEvict.Add(inst);
                }

        foreach (var inst in toEvict)
        {
            _placed.Remove(inst);
            inst.OnRemoved();
            evicted?.Add(inst);
            OnModuleRemoved?.Invoke(inst);
        }

        Width  = newW;
        Height = newH;
        _cells = newCells;
    }

    // ── Queries ───────────────────────────────

    public bool           IsInBounds   (Vector2Int c) => c.x >= 0 && c.x < Width && c.y >= 0 && c.y < Height;
    public bool           IsCellEmpty  (Vector2Int c) => IsInBounds(c) && _cells[c.x, c.y] == null;
    public ModuleInstance GetModuleAt  (Vector2Int c) => IsInBounds(c) ? _cells[c.x, c.y] : null;
    public IReadOnlyCollection<ModuleInstance> GetAllModules() => _placed;

    // ── Validation ────────────────────────────

    public bool CanPlace(ModuleInstance inst, Vector2Int pivot)
    {
        foreach (var c in GetAbsoluteCells(inst.Data, pivot))
        {
            if (!IsInBounds(c)) return false;
            var occ = _cells[c.x, c.y];
            if (occ != null && occ != inst) return false;
        }
        return true;
    }

    // ── Place / Remove ────────────────────────

    public bool TryPlace(ModuleInstance inst, Vector2Int pivot)
    {
        if (!CanPlace(inst, pivot)) return false;
        foreach (var c in GetAbsoluteCells(inst.Data, pivot))
            _cells[c.x, c.y] = inst;
        _placed.Add(inst);
        inst.OnPlaced(this, pivot);
        OnModulePlaced?.Invoke(inst);
        return true;
    }

    public bool Remove(ModuleInstance inst)
    {
        if (!_placed.Contains(inst)) return false;
        foreach (var c in inst.GetAbsoluteCells())
            if (IsInBounds(c) && _cells[c.x, c.y] == inst)
                _cells[c.x, c.y] = null;
        _placed.Remove(inst);
        inst.OnRemoved();
        OnModuleRemoved?.Invoke(inst);
        return true;
    }

    // ── Adjacency ─────────────────────────────

    public List<ModuleInstance> GetAdjacentModules(ModuleInstance inst)
    {
        var result = new HashSet<ModuleInstance>();
        var dirs   = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (var c in inst.GetAbsoluteCells())
            foreach (var d in dirs)
            {
                var nb = GetModuleAt(c + d);
                if (nb != null && nb != inst) result.Add(nb);
            }
        return new List<ModuleInstance>(result);
    }

    // ── Helper ────────────────────────────────

    public List<Vector2Int> GetAbsoluteCells(ModuleData data, Vector2Int pivot)
    {
        var rel = data.GetShapeCells();
        var abs = new List<Vector2Int>(rel.Count);
        foreach (var c in rel) abs.Add(pivot + c);
        return abs;
    }
}