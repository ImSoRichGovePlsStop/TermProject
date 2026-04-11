using System;
using UnityEngine;

[Serializable]
public class BSPRoomShapeRow
{
    [Tooltip("Each bool = one cell. True = void (blocked), False = floor.")]
    // 0 = floor, 1 = void, 2 = pillar
    public byte[] cells;

    public BSPRoomShapeRow(int width)
    {
        cells = new byte[width];
    }
}

[CreateAssetMenu(fileName = "BSPRoomPreset", menuName = "Map/BSP Room Preset")]
public class BSPRoomPreset : ScriptableObject
{
    [Header("Size")]
    [Tooltip("Width of this room preset in cells.")]
    public int sizeX = 10;
    [Tooltip("Depth of this room preset in cells.")]
    public int sizeZ = 10;

    [Header("Void Grid")]
    [Tooltip("Grid of sizeX x sizeZ. Toggle a cell to make it void — no floor, no door on that edge.")]
    public BSPRoomShapeRow[] voidGrid;

    [Header("Allowed Room Types")]
    [Tooltip("Which room types may use this preset. Leave empty to allow all.")]
    public RoomType[] allowedTypes;

    [Header("Variant")]
    [Tooltip("Integer key used to vary content (enemy count, loot tier, etc.) without needing a new preset.")]
    public int variant = 0;

    public bool IsVoid(int x, int z)
    {
        if (voidGrid == null || z >= voidGrid.Length) return false;
        var row = voidGrid[z];
        if (row == null || row.cells == null || x >= row.cells.Length) return false;
        return row.cells[x] == 1;
    }

    public bool IsPillar(int x, int z)
    {
        if (voidGrid == null || z >= voidGrid.Length) return false;
        var row = voidGrid[z];
        if (row == null || row.cells == null || x >= row.cells.Length) return false;
        return row.cells[x] == 2;
    }

    public bool AllowsType(RoomType type)
    {
        if (allowedTypes == null || allowedTypes.Length == 0) return true;
        foreach (var t in allowedTypes)
            if (t == type) return true;
        return false;
    }

    public void ResetGrid()
    {
        voidGrid = new BSPRoomShapeRow[sizeZ];
        for (int z = 0; z < sizeZ; z++)
            voidGrid[z] = new BSPRoomShapeRow(sizeX);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        sizeX = Mathf.Max(2, sizeX);
        sizeZ = Mathf.Max(2, sizeZ);

        if (voidGrid == null || voidGrid.Length != sizeZ)
        {
            var newGrid = new BSPRoomShapeRow[sizeZ];
            for (int z = 0; z < sizeZ; z++)
            {
                newGrid[z] = (voidGrid != null && z < voidGrid.Length && voidGrid[z] != null)
                    ? voidGrid[z] : new BSPRoomShapeRow(sizeX);

                if (newGrid[z].cells == null || newGrid[z].cells.Length != sizeX)
                {
                    var oldCells = newGrid[z].cells;
                    newGrid[z].cells = new byte[sizeX];
                    if (oldCells != null)
                        for (int x = 0; x < Mathf.Min(oldCells.Length, sizeX); x++)
                            newGrid[z].cells[x] = oldCells[x];
                }
            }
            voidGrid = newGrid;
        }
        else
        {
            for (int z = 0; z < sizeZ; z++)
            {
                if (voidGrid[z] == null) { voidGrid[z] = new BSPRoomShapeRow(sizeX); continue; }
                if (voidGrid[z].cells == null || voidGrid[z].cells.Length != sizeX)
                {
                    var old = voidGrid[z].cells;
                    voidGrid[z].cells = new byte[sizeX];
                    if (old != null)
                        for (int x = 0; x < Mathf.Min(old.Length, sizeX); x++)
                            voidGrid[z].cells[x] = old[x];
                }
            }
        }
    }
#endif
}