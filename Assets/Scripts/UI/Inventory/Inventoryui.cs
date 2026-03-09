using System.Collections;
using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid UI")]
    [SerializeField] private GridUI weaponGridUI;
    [SerializeField] private GridUI bagGridUI;

    [Header("Prefabs")]
    [SerializeField] private ModuleItemUI moduleItemPrefab;

    private void Start()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) { Debug.LogError("[InventoryUI] InventoryManager missing!"); return; }

        weaponGridUI.Init(mgr.WeaponGrid);
        bagGridUI.Init(mgr.BagGrid);

        Debug.Log("[InventoryUI] Initialized.");
    }

    public ModuleItemUI SpawnModule(ModuleData data)
    {
        var inst = new ModuleInstance(data);
        if (!InventoryManager.Instance.TryAddToBag(inst))
        {
            Debug.LogWarning($"[InventoryUI] Bag full — {data.moduleName}");
            return null;
        }

        var go = Instantiate(moduleItemPrefab, transform);
        var ui = go.GetComponent<ModuleItemUI>();
        ui.Init(inst, weaponGridUI, bagGridUI);

        StartCoroutine(SnapNextFrame(ui, bagGridUI, inst.GridPosition));
        return ui;
    }

    private IEnumerator SnapNextFrame(ModuleItemUI ui, GridUI gridUI, Vector2Int cell)
    {
        ui.GetComponent<CanvasGroup>().alpha = 0f;
        yield return null;
        Canvas.ForceUpdateCanvases();
        ui.SnapToCell(gridUI, cell);
        ui.GetComponent<CanvasGroup>().alpha = 1f;
    }
}
