using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private GameObject inventoryPanel;
    private PassiveScreenUI passiveScreenUI;
    private ShopUI _activeShopUI;
    [SerializeField] private GameObject hud;
    public bool IsInventoryOpen { get; private set; }
    public bool IsShopOpen => _activeShopUI != null && _activeShopUI.gameObject.activeSelf;

    private float holdTime = 0f;
    private float holdDuration = 1f;

    private void Start()
    {
        inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>(FindObjectsInactive.Include);

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (_activeShopUI != null) _activeShopUI.gameObject.SetActive(true);

        inventoryPanel?.SetActive(false);
        _activeShopUI?.gameObject.SetActive(false);

        var sellUI = FindObjectsByType<SellConfirmationUI>(FindObjectsSortMode.None);
        foreach (var ui in sellUI)
        {
            ui.gameObject.SetActive(true);
            ui.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (Keyboard.current[Key.Tab].wasPressedThisFrame)
        {
            if (!passiveScreenUI.IsOpen)
                ToggleInventory();
            else
                passiveScreenUI.Close();
        }

        if (Keyboard.current[Key.Escape].wasPressedThisFrame)
        {
            if (passiveScreenUI.IsOpen)
                passiveScreenUI.Close();
            else if (_activeShopUI != null && _activeShopUI.gameObject.activeSelf)
                CloseShop();
            else if (IsInventoryOpen)
                ToggleInventory();
        }

        if (Keyboard.current[Key.F].wasPressedThisFrame && IsInventoryOpen)
            inventoryUI?.TakeAllFromEnv();

        if (passiveScreenUI != null && passiveScreenUI.IsOpen)
        {
            if (Keyboard.current[Key.R].isPressed)
            {
                holdTime += Time.deltaTime;
                if (holdTime >= holdDuration)
                {
                    passiveScreenUI.OnResetHeld();
                    holdTime = 0f;
                }
            }
            else
            {
                holdTime = 0f;
            }
        }
    }

    [SerializeField] private ShopUI shopUI;
    [SerializeField] private InventoryUI inventoryUI;

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        if (_activeShopUI != null && _activeShopUI.gameObject.activeSelf)
            CloseShop();

        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);

        if (IsInventoryOpen)
            StartCoroutine(ForceMoveToBagNextFrame()); // <-- move items to inventory bag grid on open

        if (!IsInventoryOpen)
        {
            ModuleTooltipUI.Instance?.Hide();
            inventoryUI?.ClearEnvGrid();
            inventoryUI?.SetEnvGridVisible(false);
        }
        UpdateHUDVisibility();
    }


    public void OpenShop(ShopUI shopUI)
    {
        if (IsInventoryOpen) ToggleInventory();
        _activeShopUI = shopUI;
        shopUI.gameObject.SetActive(true);
        shopUI.ForceMoveToShop();
        UpdateHUDVisibility();
    }

    public void CloseShop()
    {
        if (_activeShopUI == null) return;
        _activeShopUI.gameObject.SetActive(false);
        _activeShopUI = null;
        UpdateHUDVisibility();
    }

    private void UpdateHUDVisibility()
    {
        bool shouldHide =
            IsInventoryOpen ||
            (passiveScreenUI != null && passiveScreenUI.IsOpen) ||
            (_activeShopUI != null && _activeShopUI.gameObject.activeSelf);

        if (hud != null)
            hud.SetActive(!shouldHide);
    }

    private IEnumerator ForceMoveToBagNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        shopUI?.ForceMoveToBag();
    }
}