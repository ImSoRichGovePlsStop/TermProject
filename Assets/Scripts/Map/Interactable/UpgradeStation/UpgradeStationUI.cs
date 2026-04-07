using System.Collections.Generic;
using UnityEngine;

public class UpgradeStationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topRow;
    [SerializeField] private Transform bottomRow;
    [SerializeField] private ItemCardUI optionPrefab;

    private UpgradeStation _currentStation;

    private static (int top, int bot) GetLayout(int total) => total switch
    {
        1 => (1, 0),
        2 => (2, 0),
        3 => (2, 1),
        4 => (3, 1),
        5 => (3, 2),
        _ => (3, 3),
    };

    public void Open(UpgradeStation station)
    {
        _currentStation = station;
        PopulateOptions();
    }

    private void PopulateOptions()
    {
        var candidates = BuildWeightedCandidateList();

        if (candidates.Count == 0)
        {

            var uiManager = FindFirstObjectByType<UIManager>();
            uiManager?.CloseUpgrade();
            return;
        }

        var selected = new List<ModuleInstance>();
        var usedModules = new HashSet<ModuleInstance>();

        int attempts = 0;
        while (selected.Count < 3 && attempts < 1000)
        {
            attempts++;
            int idx = Random.Range(0, candidates.Count);
            var inst = candidates[idx];
            if (usedModules.Contains(inst)) continue;
            usedModules.Add(inst);
            selected.Add(inst);
        }

        var (topCount, _) = GetLayout(selected.Count);
        foreach (Transform child in topRow) Destroy(child.gameObject);
        foreach (Transform child in bottomRow) Destroy(child.gameObject);

        for (int i = 0; i < selected.Count; i++)
        {
            Transform parent = i < topCount ? topRow : bottomRow;
            var option = Instantiate(optionPrefab, parent);
            option.InitUpgrade(selected[i], this);
        }

        bottomRow.gameObject.SetActive(selected.Count > topCount);
    }

    private List<ModuleInstance> BuildWeightedCandidateList()
    {
        var mgr = InventoryManager.Instance;
        var candidates = new List<ModuleInstance>();

        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            if (inst is MaterialInstance) continue;
            candidates.Add(inst);
            candidates.Add(inst);
            candidates.Add(inst);
        }

        foreach (var inst in mgr.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance) continue;
            candidates.Add(inst);
        }

        return candidates;
    }

    public void OnOptionSelected(ModuleInstance instance)
    {
        instance.SetLevel(instance.Level + 1);


        var moduleUI = instance.UIElement as ModuleItemUI;
        if (moduleUI != null)
            moduleUI.RefreshAfterUpgrade();


        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseUpgrade();
    }
}