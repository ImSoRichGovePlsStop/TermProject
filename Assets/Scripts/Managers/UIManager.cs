using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private GameObject inventoryPanel;
    private PassiveScreenUI passiveScreenUI;
    private ShopUI _activeShopUI;
    private GamblerScreenUI gamblerScreenUI;
    private GameObject gameOverScreen;
    private UpgradeStationUI _upgradeStationUI;
    private bool _upgradeOpen = false;

    [SerializeField] private GameObject hud;

    public bool isInBattle { get; set; }
    public bool IsInventoryOpen { get; private set; }
    public bool IsShopOpen => _activeShopUI != null && _activeShopUI.gameObject.activeSelf;
    public bool IsUpgradeOpen => _upgradeOpen;

    public PassiveScreenUI GetPassiveScreen() => passiveScreenUI;
    public GamblerScreenUI GetGamblerScreen() => gamblerScreenUI;

    private float holdTime = 0f;
    private float holdDuration = 1f;

    private void Start()
    {
        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            playerStats.OnPlayerDeath += OnPlayerDeath;

        inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>(FindObjectsInactive.Include);
        gamblerScreenUI = FindFirstObjectByType<GamblerScreenUI>(FindObjectsInactive.Include);
        _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (_activeShopUI != null) _activeShopUI.gameObject.SetActive(true);
        if (_upgradeStationUI != null) _upgradeStationUI.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        inventoryPanel?.SetActive(false);
        _activeShopUI?.gameObject.SetActive(false);
        if (_upgradeStationUI != null) _upgradeStationUI.gameObject.SetActive(false);

        var sellUI = FindObjectsByType<SellConfirmationUI>(FindObjectsSortMode.None);
        foreach (var ui in sellUI)
        {
            ui.gameObject.SetActive(true);
            ui.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateHUDVisibility();

        if (!isInBattle)
        {
            if (Keyboard.current[Key.Tab].wasPressedThisFrame)
            {
                if (gamblerScreenUI != null && gamblerScreenUI.IsOpen)
                    gamblerScreenUI.Close();
                else if (!passiveScreenUI.IsOpen)
                    ToggleInventory();
                else
                    passiveScreenUI.Close();
            }

            if (Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                if (passiveScreenUI.IsOpen)
                    passiveScreenUI.Close();
                else if (gamblerScreenUI != null && gamblerScreenUI.IsOpen)
                    gamblerScreenUI.Close();
                else if (_upgradeOpen)
                { } // blocked — player must select an upgrade
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
            StartCoroutine(ForceMoveToBagNextFrame());

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

    public void OpenUpgrade(UpgradeStation station)
    {
        if (IsInventoryOpen) ToggleInventory();
        if (IsShopOpen) CloseShop();

        if (_upgradeStationUI == null)
            _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);

        if (_upgradeStationUI == null) { Debug.LogError("[UIManager] UpgradeStationUI not found!"); return; }

        _upgradeOpen = true;
        _upgradeStationUI.gameObject.SetActive(true);
        _upgradeStationUI.Open(station);
        UpdateHUDVisibility();
    }

    public void CloseUpgrade()
    {
        if (_upgradeStationUI != null)
            _upgradeStationUI.gameObject.SetActive(false);
        _upgradeOpen = false;
        UpdateHUDVisibility();
    }

    private void UpdateHUDVisibility()
    {
        bool shouldHide =
            IsInventoryOpen ||
            (passiveScreenUI != null && passiveScreenUI.IsOpen) ||
            (gamblerScreenUI != null && gamblerScreenUI.IsOpen) ||
            (_activeShopUI != null && _activeShopUI.gameObject.activeSelf);

        bool shouldShow = !shouldHide;
        if (hud != null && hud.activeSelf != shouldShow)
            hud.SetActive(shouldShow);
    }

    private IEnumerator ForceMoveToBagNextFrame()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        shopUI?.ForceMoveToBag();
    }

    private void OnPlayerDeath()
    {
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }
}