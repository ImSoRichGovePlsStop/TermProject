using System.Collections.Generic;
using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private TestModuleEntry[] shopEntries;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private UIManager uiManager;

    private readonly HashSet<int> _soldIndices = new HashSet<int>();

    public void Interact(PlayerController playerController)
    {
        if (shopUI == null) { Debug.LogError("[ShopInteractable] ShopUI is missing!"); return; }
        if (uiManager == null) { Debug.LogError("[ShopInteractable] UIManager is missing!"); return; }

        uiManager.OpenShop(shopUI);
        shopUI.Populate(shopEntries, _soldIndices, this);
    }

    public void RegisterSold(int index)
    {
        _soldIndices.Add(index);
    }
}