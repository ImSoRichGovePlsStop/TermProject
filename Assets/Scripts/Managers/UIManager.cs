using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private GameObject inventoryPanel;
    public bool IsInventoryOpen { get; private set; }

    private void Start()
    {
        inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current[Key.Tab].wasPressedThisFrame)
            ToggleInventory();
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;
        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);
    }
}