using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeStationUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topRow;
    [SerializeField] private Transform bottomRow;
    [SerializeField] private ItemCardUI optionPrefab;
    [SerializeField] private Button rerollButton;

    private UpgradeStation _currentStation;
    private bool _hasRerolled;

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

        _hasRerolled = false;
        if (rerollButton != null)
        {
            bool canReroll = RunManager.Instance == null || RunManager.Instance.AllowReroll;
            rerollButton.gameObject.SetActive(canReroll);
            rerollButton.interactable = true;
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnReroll);
        }
    }

    private void OnReroll()
    {
        if (_hasRerolled) return;
        if (RunManager.Instance != null && !RunManager.Instance.AllowReroll) return;

        _hasRerolled = true;
        if (rerollButton != null) rerollButton.interactable = false;

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

    private void OnDisable()
    {
        _currentStation?.OnUpgradeClosed();
    }

    public void OnOptionSelected(ModuleInstance instance)
    {
        instance.SetLevel(instance.Level + 1);

        InventoryManager.Instance?.RefreshModule(instance);

        var moduleUI = instance.UIElement as ModuleItemUI;
        if (moduleUI != null)
        {
            moduleUI.RefreshAfterUpgrade();
            moduleUI.PlaySpawnPulse();
        }

        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseUpgrade();
    }
}
