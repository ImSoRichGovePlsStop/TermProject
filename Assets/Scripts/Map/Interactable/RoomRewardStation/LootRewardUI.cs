using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LootRewardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform topRow;
    [SerializeField] private Transform bottomRow;
    [SerializeField] private ItemCardUI optionPrefab;

    private List<ModuleInstance> rolledOptions = new List<ModuleInstance>();
    private RandomLoot currentStation;

    private static (int top, int bot) GetLayout(int total) => total switch
    {
        1 => (1, 0),
        2 => (2, 0),
        3 => (2, 1),
        4 => (3, 1),
        5 => (3, 2),
        _ => (3, 3),
    };

    public void Open(RandomLoot station, List<TestModuleEntry> rolled)
    {
        currentStation = station;
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

    public void OnOptionSelected(ModuleInstance chosen)
    {
        var inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventoryUI?.SpawnModule(chosen.Data, chosen.Rarity, chosen.Level);

        currentStation?.OnLootPicked();

        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseRewardLoot();

        gameObject.SetActive(false);
    }
}