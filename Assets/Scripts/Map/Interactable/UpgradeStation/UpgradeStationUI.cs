using System.Collections.Generic;
using UnityEngine;

public class UpgradeStationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform optionContainer;
    [SerializeField] private UpgradeOptionUI optionPrefab;

    private UpgradeStation _currentStation;

    public void Open(UpgradeStation station)
    {
        _currentStation = station;
        PopulateOptions();
    }

    private void PopulateOptions()
    {
        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

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

        foreach (var inst in selected)
        {
            var option = Instantiate(optionPrefab, optionContainer);
            option.Init(inst, this);
        }
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

        if (_currentStation != null)
            Object.Destroy(_currentStation.gameObject);

        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseUpgrade();
    }
}