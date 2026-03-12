using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Weapon Grid")]
    [SerializeField] private int weaponCols = 5;
    [SerializeField] private int weaponRows = 5;

    [Header("Bag Grid")]
    [SerializeField] private int bagCols = 8;
    [SerializeField] private int bagRows = 8;

    [Header("Env Grid")]
    [SerializeField] private int envCols = 5;
    [SerializeField] private int envRows = 8;

    public GridData WeaponGrid { get; private set; }
    public GridData BagGrid    { get; private set; }
    public GridData EnvGrid    { get; private set; }

    public event System.Action<ModuleInstance> OnModuleEquipped;
    public event System.Action<ModuleInstance> OnModuleUnequipped;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        WeaponGrid = new GridData(weaponCols, weaponRows, isWeaponGrid: true);
        BagGrid    = new GridData(bagCols,    bagRows,    isWeaponGrid: false);
        EnvGrid    = new GridData(envCols,    envRows,    isWeaponGrid: false);

        WeaponGrid.OnModulePlaced  += inst => OnModuleEquipped?.Invoke(inst);
        WeaponGrid.OnModuleRemoved += inst => OnModuleUnequipped?.Invoke(inst);

        GetComponent<InventoryLayout>()?.ApplyLayout(weaponCols, weaponRows, bagCols, bagRows, envCols, envRows);
    }

    public bool TryMoveModule(ModuleInstance inst, GridData targetGrid, Vector2Int pivot)
    {
        var prevGrid = inst.CurrentGrid;
        var prevPos  = inst.GridPosition;

        prevGrid?.Remove(inst);

        if (targetGrid.TryPlace(inst, pivot)) return true;

        prevGrid?.TryPlace(inst, prevPos);
        return false;
    }

    public bool TryAddToBag(ModuleInstance inst)
    {
        for (int row = 0; row < BagGrid.Height; row++)
            for (int col = 0; col < BagGrid.Width; col++)
                if (BagGrid.TryPlace(inst, new Vector2Int(col, row))) return true;

        Debug.LogWarning($"[Inventory] Bag full — {inst.Data.moduleName}");
        return false;
    }

    public void ExpandWeaponGrid(int newCols, int newRows)
    {
        var evicted = new List<ModuleInstance>();
        WeaponGrid.Resize(newCols, newRows, evicted);
        foreach (var inst in evicted) TryAddToBag(inst);
    }
}