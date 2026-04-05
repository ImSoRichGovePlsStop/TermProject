using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Weapon Grid — Max Size")]
    [SerializeField] private int weaponMaxCols = 5;
    [SerializeField] private int weaponMaxRows = 7;

    [Header("Bag Grid — Max Size")]
    [SerializeField] private int bagMaxCols = 10;
    [SerializeField] private int bagMaxRows = 6;

    [Header("Bag Grid — Starting Unlocked Size")]
    [SerializeField] private int bagStartCols = 8;
    [SerializeField] private int bagStartRows = 5;

    public GridData WeaponGrid { get; private set; }
    public GridData BagGrid { get; private set; }

    public int WeaponUnlockedCols { get; private set; } = 1;
    public int WeaponUnlockedRows { get; private set; } = 1;
    public int BagUnlockedCols { get; private set; }
    public int BagUnlockedRows { get; private set; }

    public event System.Action<ModuleInstance> OnModuleEquipped;
    public event System.Action<ModuleInstance> OnModuleUnequipped;
    public event System.Action OnWeaponGridChanged;
    public event System.Action OnBagGridChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        WeaponGrid = new GridData(weaponMaxCols, weaponMaxRows, isWeaponGrid: true);
        BagGrid = new GridData(bagMaxCols, bagMaxRows, isWeaponGrid: false);

        WeaponUnlockedCols = 1;
        WeaponUnlockedRows = 1;
        BagUnlockedCols = bagStartCols;
        BagUnlockedRows = bagStartRows;

        WeaponGrid.OnModulePlaced += inst => OnModuleEquipped?.Invoke(inst);
        WeaponGrid.OnModuleRemoved += inst => OnModuleUnequipped?.Invoke(inst);
    }

    public void UpgradeWeaponGrid(int newUnlockedCols, int newUnlockedRows)
    {
        WeaponUnlockedCols = Mathf.Clamp(newUnlockedCols, 1, weaponMaxCols);
        WeaponUnlockedRows = Mathf.Clamp(newUnlockedRows, 1, weaponMaxRows);

        var toEvict = new List<ModuleInstance>();
        foreach (var inst in WeaponGrid.GetAllModules())
            foreach (var cell in inst.GetAbsoluteCells())
                if (cell.x >= WeaponUnlockedCols || cell.y >= WeaponUnlockedRows)
                { toEvict.Add(inst); break; }

        foreach (var inst in toEvict)
        {
            WeaponGrid.Remove(inst);
            TryAddToBag(inst);
        }

        OnWeaponGridChanged?.Invoke();
    }

    public void UpgradeBagGrid(int newUnlockedCols, int newUnlockedRows)
    {
        BagUnlockedCols = Mathf.Clamp(newUnlockedCols, 1, bagMaxCols);
        BagUnlockedRows = Mathf.Clamp(newUnlockedRows, 1, bagMaxRows);
        OnBagGridChanged?.Invoke();
    }

    public bool TryMoveModule(ModuleInstance inst, GridData targetGrid, Vector2Int pivot)
    {
        var prevGrid = inst.CurrentGrid;
        var prevPos = inst.GridPosition;

        prevGrid?.Remove(inst);
        if (targetGrid.TryPlace(inst, pivot)) return true;

        prevGrid?.TryPlace(inst, prevPos);
        return false;
    }

    public bool TryAddToBag(ModuleInstance inst)
    {
        for (int row = 0; row < BagUnlockedRows; row++)
            for (int col = 0; col < BagUnlockedCols; col++)
                if (BagGrid.TryPlace(inst, new Vector2Int(col, row))) return true;

        Debug.LogWarning($"[Inventory] Bag full — {inst.Data.moduleName}");
        return false;
    }

    public bool IsBagFull()
    {
        for (int row = 0; row < BagUnlockedRows; row++)
            for (int col = 0; col < BagUnlockedCols; col++)
                if (BagGrid.IsCellEmpty(new Vector2Int(col, row))) return false;
        return true;
    }

    public bool IsWithinWeaponUnlocked(Vector2Int cell)
        => cell.x < WeaponUnlockedCols && cell.y < WeaponUnlockedRows;

    public bool CanPlaceInWeaponGrid(ModuleInstance inst, Vector2Int pivot, int rotation)
    {
        foreach (var cell in WeaponGrid.GetAbsoluteCells(inst.Data, pivot, rotation))
            if (!IsWithinWeaponUnlocked(cell)) return false;
        return WeaponGrid.CanPlace(inst, pivot, rotation);
    }
    public void DeleteModule(ModuleInstance inst)
    {
        WeaponGrid.Remove(inst);
        BagGrid.Remove(inst);

        if (inst.UIElement is MonoBehaviour ui)
            Destroy(ui.gameObject);

        OnWeaponGridChanged?.Invoke();
        OnBagGridChanged?.Invoke();
    }
}