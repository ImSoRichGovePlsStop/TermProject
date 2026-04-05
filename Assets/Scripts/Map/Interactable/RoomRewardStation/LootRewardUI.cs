using System.Collections.Generic;
using UnityEngine;

public class LootRewardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform optionContainer;
    [SerializeField] private LootOptionUI optionPrefab;

    private List<ModuleInstance> _rolledOptions = new List<ModuleInstance>();
    private RandomLoot _currentStation;

    public void Open(RandomLoot station, List<TestModuleEntry> rolled)
    {
        _currentStation = station;
        _rolledOptions.Clear();

        foreach (Transform child in optionContainer)
            Destroy(child.gameObject);

        foreach (var entry in rolled)
        {
            if (entry.data == null) continue;
            var inst = new ModuleInstance(entry.data, entry.rarity, entry.level);
            _rolledOptions.Add(inst);
            var option = Instantiate(optionPrefab, optionContainer);
            option.Init(inst, this);
        }
    }

    public void OnOptionSelected(ModuleInstance chosen)
    {
        var inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        inventoryUI?.SpawnModule(chosen.Data, chosen.Rarity, chosen.Level);

        if (_currentStation != null)
            Object.Destroy(_currentStation.gameObject);

        var uiManager = FindFirstObjectByType<UIManager>();
        uiManager?.CloseRewardLoot();

        gameObject.SetActive(false);
    }
}