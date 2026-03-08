using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryToggle : MonoBehaviour
{
    [Header("Key Binding")]
    [SerializeField] private Key toggleKey = Key.Tab;

    [Header("References")]
    [SerializeField] private GameObject inventoryPanel;

    public bool IsOpen { get; private set; }

    public event System.Action<bool> OnInventoryToggled;

    private void Start()
    {
        SetInventoryOpen(false);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            SetInventoryOpen(!IsOpen);
    }

    public void SetInventoryOpen(bool open)
    {
        IsOpen = open;
        if (inventoryPanel != null)
            inventoryPanel.SetActive(open);
        OnInventoryToggled?.Invoke(open);
    }
}