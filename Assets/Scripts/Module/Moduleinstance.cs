using System.Collections.Generic;
using UnityEngine;

public class ModuleInstance
{
    public ModuleData Data { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public GridData CurrentGrid { get; private set; }
    public ModuleItemUI UIElement { get; set; }

    public static PlayerStats SharedStats { get; set; }

    public ModuleInstance(ModuleData data) { Data = data; }

    internal void OnPlaced(GridData grid, Vector2Int position)
    {
        CurrentGrid = grid;
        GridPosition = position;

        if (SharedStats == null) return;

        if (!grid.IsWeaponGrid)
        {
            Data.moduleEffect?.Unequip(SharedStats);
            return;
        }

        // Equip applies base stats immediately
        Data.moduleEffect?.Equip(SharedStats);

        // Buff modules re-evaluate — OnLevelUp/Down on HpEffects do Remove+Apply internally
        RefreshAllBuffModules(grid);
    }

    internal void OnRemoved()
    {
        var grid = CurrentGrid;

        CurrentGrid = null;
        GridPosition = Vector2Int.zero;

        if (grid == null || SharedStats == null) return;

        Data.moduleEffect?.Unequip(SharedStats);

        if (grid.IsWeaponGrid)
            RefreshAllBuffModules(grid);
    }

    public List<Vector2Int> GetAbsoluteCells()
    {
        var rel = Data.GetShapeCells();
        var abs = new List<Vector2Int>(rel.Count);
        foreach (var c in rel) abs.Add(GridPosition + c);
        return abs;
    }

    private static void RefreshAllBuffModules(GridData grid)
    {
        if (!grid.IsWeaponGrid || SharedStats == null) return;

        foreach (var inst in grid.GetAllModules())
        {
            if (inst.Data.moduleEffect is not LevelUpBuffEffect buff) continue;
            buff.Refresh(grid, inst);
        }
    }
}