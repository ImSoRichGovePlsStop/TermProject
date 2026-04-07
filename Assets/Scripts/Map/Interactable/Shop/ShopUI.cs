using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridUI bagGridUI;
    [SerializeField] private Transform topRow;
    [SerializeField] private Transform bottomRow;
    [SerializeField] private ItemCardUI shopItemPrefab;
    [SerializeField] private SellConfirmationUI sellConfirmationUI;


    private ShopInteractable _currentInteractable;
    private TestModuleEntry[] _currentEntries;
    private InventoryUI _inventoryUI;

    private void Awake()
    {
        _inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
    }

    public void OnOpened() => SetBagItemRefsForShop();

    public void OnClosed()
    {
        SetBagItemRefsForInventory();

        bagGridUI.ClearHighlights();
        bagGridUI.ClearBuffHighlights();
    }


    private void SetBagItemRefsForShop()
    {
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = InventoryUI.StaticWeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
                ui.SellConfirmationUI = sellConfirmationUI;
                ui.SetAllowSell(true);
            }
        }
    }

    private void SetBagItemRefsForInventory()
    {
        if (_inventoryUI == null) return;
        foreach (var inst in InventoryManager.Instance.BagGrid.GetAllModules())
        {
            if (inst is MaterialInstance)
            {
                var ui = inst.UIElement as MaterialItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = _inventoryUI.WeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
            }
            else
            {
                var ui = inst.UIElement as ModuleItemUI;
                if (ui == null) continue;
                ui.WeaponGridUI = _inventoryUI.WeaponGridUI;
                ui.BagGridUI = bagGridUI;
                ui.EnvGridUI = null;
                ui.InputGridUI = null;
                ui.SellConfirmationUI = null;
                ui.SetAllowSell(false);
            }
        }
    }

    private static (int top, int bot) GetLayout(int total) => total switch
    {
        1 => (1, 0),
        2 => (2, 0),
        3 => (3, 0),
        4 => (3, 1),
        5 => (3, 2),
        _ => (3, 3),
    };

    public void Populate(TestModuleEntry[] entries, HashSet<int> soldIndices, ShopInteractable interactable)
    {
        _currentInteractable = interactable;
        _currentEntries = entries;

        foreach (Transform child in topRow) Destroy(child.gameObject);
        foreach (Transform child in bottomRow) Destroy(child.gameObject);

        var validEntries = new List<(TestModuleEntry entry, int originalIndex)>();
        for (int i = 0; i < entries.Length; i++)
            if (entries[i].data != null)
                validEntries.Add((entries[i], i));

        validEntries.Sort((a, b) => GetSortPriority(a.originalIndex, soldIndices).CompareTo(GetSortPriority(b.originalIndex, soldIndices)));

        var (topCount, botCount) = GetLayout(validEntries.Count);

        for (int i = 0; i < validEntries.Count; i++)
        {
            var (entry, originalIndex) = validEntries[i];
            Transform parent = i < topCount ? topRow : bottomRow;
            var item = Instantiate(shopItemPrefab, parent);
            item.InitShop(entry, this, originalIndex);
            if (soldIndices.Contains(originalIndex)) item.MarkPurchased();
        }

        bottomRow.gameObject.SetActive(botCount > 0);
    }

    private int GetSortPriority(int index, HashSet<int> soldIndices)
    {
        if (soldIndices.Contains(index)) return 2;
        if (CurrencyManager.Instance == null) return 0;
        int price = _currentEntries[index].data.cost[(int)_currentEntries[index].rarity];
        return CurrencyManager.Instance.Coins >= price ? 0 : 1;
    }

    public void RegisterSold(int index) => _currentInteractable?.RegisterSold(index);

}