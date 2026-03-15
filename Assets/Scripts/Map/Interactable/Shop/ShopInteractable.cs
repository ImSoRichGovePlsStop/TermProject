using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ShopEntry[] shopEntries;
    [SerializeField] private ShopUI shopUI;
    [SerializeField] private UIManager uiManager;

    public void Interact(PlayerController playerController)
    {
        if (shopUI == null) { Debug.LogError("[ShopInteractable] ShopUI is missing!"); return; }
        if (uiManager == null) { Debug.LogError("[ShopInteractable] UIManager is missing!"); return; }

        shopUI.Populate(shopEntries);
        uiManager.OpenShop(shopUI);
    }
}