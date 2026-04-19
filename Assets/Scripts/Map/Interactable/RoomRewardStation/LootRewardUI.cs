using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootRewardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topRow;
    [SerializeField] private Transform bottomRow;
    [SerializeField] private ItemCardUI optionPrefab;
    [SerializeField] private Button rerollButton;

    private List<ModuleInstance> rolledOptions = new List<ModuleInstance>();
    private LootConfig _config;
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

    public void Open(LootConfig config, List<TestModuleEntry> rolled)
    {
        _config = config;

        _hasRerolled = false;
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnReroll);
            rerollButton.interactable = RunManager.Instance == null || RunManager.Instance.AllowReroll;
        }

        RepopulateCards(rolled);
    }

    private void RepopulateCards(List<TestModuleEntry> rolled)
    {
        rolledOptions.Clear();

        foreach (Transform child in topRow) Destroy(child.gameObject);
        foreach (Transform child in bottomRow) Destroy(child.gameObject);

        var validEntries = rolled.FindAll(e => e.data != null);
        var (topCount, botCount) = GetLayout(validEntries.Count);

        for (int i = 0; i < validEntries.Count; i++)
        {
            var entry = validEntries[i];
            var inst = new ModuleInstance(entry.data, entry.rarity, entry.level);
            rolledOptions.Add(inst);

            Transform parent = i < topCount ? topRow : bottomRow;
            var option = Instantiate(optionPrefab, parent);
            option.InitReward(inst, this);
        }

        bottomRow.gameObject.SetActive(botCount > 0);
    }

    private void OnReroll()
    {
        if (_hasRerolled) return;
        if (RunManager.Instance != null && !RunManager.Instance.AllowReroll) return;

        _hasRerolled = true;
        if (rerollButton != null) rerollButton.interactable = false;

        var newRolled = Randomizer.Roll(_config.optionCount, _config.optionCount, _config.meanCost, _config.sd, _config.allowDuplicates);
        RepopulateCards(newRolled);
    }

    public void OnOptionSelected(ModuleInstance chosen)
    {
        var inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventoryUI?.SpawnModule(chosen.Data, chosen.Rarity, chosen.Level);

        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseRewardLoot();

        gameObject.SetActive(false);
    }
}