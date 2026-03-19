using System.Collections.Generic;
using UnityEngine;

public class ModuleInstance
{
    public ModuleData Data { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public GridData CurrentGrid { get; private set; }
    public MonoBehaviour UIElement { get; set; }
    public List<ModuleInstance> buffTargets { get; private set; } = new List<ModuleInstance>();

    public Rarity Rarity { get; private set; }
    public int Level { get; private set; }
    public ModuleRuntimeState RuntimeState { get; } = new ModuleRuntimeState();

    public ModuleInstance(ModuleData data, Rarity rarity = Rarity.Common, int level = 0)
    {
        Data = data;
        Rarity = rarity;
        Level = level;
    }

    public void SetLevel(int level) => Level = Mathf.Max(0, level);
    public void SetRarity(Rarity r) => Rarity = r;

    internal void OnPlaced(GridData grid, Vector2Int position)
    {
        CurrentGrid = grid;
        GridPosition = position;
    }

    internal void OnRemoved()
    {
        CurrentGrid = null;
        GridPosition = Vector2Int.zero;
    }

    public List<Vector2Int> GetAbsoluteCells()
    {
        var relative = Data.GetShapeCells();
        var absolute = new List<Vector2Int>(relative.Count);
        foreach (var c in relative) absolute.Add(GridPosition + c);
        return absolute;
    }

    public List<Vector2Int> GetAbsoluteBuffCells()
    {
        if (!Data.isBuffAdjacent) return new List<Vector2Int>();
        var relative = Data.GetBuffCells();
        var absolute = new List<Vector2Int>(relative.Count);
        foreach (var c in relative) absolute.Add(GridPosition + c);
        return absolute;
    }
    public float GetCostAtLevel()
    {
        if (Data.cost == null || Data.cost.Length == 0) return 0;
        float baseCost = Data.cost[(int)Rarity];
        for (int i = 0; i < Level; i++)
        {
            baseCost *= 1.15f;
        }
        return baseCost;
    }

    public float GetSellValueAtLevel()
    {
        return Mathf.Floor(GetCostAtLevel() * 0.75f);
    }
    public float GetUpgradeCost()
    {
        return GetCostAtLevel() * 0.3f;
    }
}