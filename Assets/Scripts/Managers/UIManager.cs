using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    private GameObject inventoryPanel;
    private GameObject bagGrid;
    private PassiveScreenUI passiveScreenUI;
    private ShopUI _activeShopUI;
    private MergeUI _activeMergeUI;
    private GamblerScreenUI gamblerScreenUI;
    private GameObject gameOverScreen;
    private UpgradeStationUI _upgradeStationUI;
    private bool _upgradeOpen = false;

    [SerializeField] private GameObject hud;

    public bool isInBattle { get; set; }
    public bool IsInventoryOpen { get; private set; }
    public bool IsShopOpen => _activeShopUI != null && _activeShopUI.gameObject.activeSelf;
    public bool IsMergeOpen => _activeMergeUI != null && _activeMergeUI.gameObject.activeSelf;
    public bool IsUpgradeOpen => _upgradeOpen;

    public PassiveScreenUI GetPassiveScreen() => passiveScreenUI;
    public GamblerScreenUI GetGamblerScreen() => gamblerScreenUI;

    private float holdTime = 0f;
    private float holdDuration = 1f;

    [SerializeField] private InventoryUI inventoryUI;

    private void Start()
    {
        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
            playerStats.OnPlayerDeath += OnPlayerDeath;

        inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        bagGrid = GameObject.FindWithTag("BagGrid");
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>(FindObjectsInactive.Include);
        gamblerScreenUI = FindFirstObjectByType<GamblerScreenUI>(FindObjectsInactive.Include);
        _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);

        var shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        var mergeUI = FindFirstObjectByType<MergeUI>(FindObjectsInactive.Include);
        var sellUI = FindFirstObjectByType<SellConfirmationUI>(FindObjectsInactive.Include);

        if (inventoryPanel != null) inventoryPanel.SetActive(true);
        if (bagGrid != null) bagGrid.SetActive(true);
        if (shopUI != null) shopUI.gameObject.SetActive(true);
        if (mergeUI != null) mergeUI.gameObject.SetActive(true);
        if (_upgradeStationUI != null) _upgradeStationUI.gameObject.SetActive(true);
        if (sellUI != null) sellUI.gameObject.SetActive(true);

        Canvas.ForceUpdateCanvases();

        if (inventoryPanel != null) inventoryPanel.SetActive(false);
        if (bagGrid != null) bagGrid.SetActive(false);
        if (shopUI != null) shopUI.gameObject.SetActive(false);
        if (mergeUI != null) mergeUI.gameObject.SetActive(false);
        if (_upgradeStationUI != null) _upgradeStationUI.gameObject.SetActive(false);
        if (sellUI != null) sellUI.gameObject.SetActive(false);
    }

    private void Update()
    {
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
                { }
                else if (IsMergeOpen)
                    CloseMerge();
                else if (IsShopOpen)
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

    public void ToggleInventory()
    {
        if (inventoryPanel == null) return;

        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();

        IsInventoryOpen = !IsInventoryOpen;
        inventoryPanel.SetActive(IsInventoryOpen);

        if (!IsInventoryOpen)
        {
            ModuleTooltipUI.Instance?.Hide();
            inventoryUI?.ClearEnvGrid();
            inventoryUI?.SetEnvGridVisible(false);
            inventoryUI?.RestoreBagItemRefs();
        }

        UpdatePanelVisibility();
    }

    public void OpenShop(ShopUI shop)
    {
        if (IsInventoryOpen) ToggleInventory();
        if (IsMergeOpen) CloseMerge();

        _activeShopUI = shop;
        shop.gameObject.SetActive(true);
        shop.OnOpened();
        UpdatePanelVisibility();
    }

    public void CloseShop()
    {
        if (_activeShopUI == null) return;
        ModuleTooltipUI.Instance?.Hide();
        _activeShopUI.OnClosed();
        _activeShopUI.gameObject.SetActive(false);
        _activeShopUI = null;
        UpdatePanelVisibility();
    }

    public void OpenMerge(MergeUI mergeUI, MergeStation station)
    {
        if (IsInventoryOpen) ToggleInventory();
        if (IsShopOpen) CloseShop();

        _activeMergeUI = mergeUI;
        mergeUI.gameObject.SetActive(true);
        mergeUI.Open(station);
        UpdatePanelVisibility();
    }

    public void CloseMerge()
    {
        if (_activeMergeUI == null) return;
        ModuleTooltipUI.Instance?.Hide();
        _activeMergeUI.gameObject.SetActive(false);
        _activeMergeUI = null;
        UpdatePanelVisibility();
    }

    public void OpenUpgrade(UpgradeStation station)
    {
        if (IsInventoryOpen) ToggleInventory();
        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();

        if (_upgradeStationUI == null)
            _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);

        if (_upgradeStationUI == null) { Debug.LogError("[UIManager] UpgradeStationUI not found!"); return; }

        _upgradeOpen = true;
        _upgradeStationUI.gameObject.SetActive(true);
        _upgradeStationUI.Open(station);
        UpdatePanelVisibility();
    }

    public void CloseUpgrade()
    {
        if (_upgradeStationUI != null)
            _upgradeStationUI.gameObject.SetActive(false);
        _upgradeOpen = false;
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        bool showBagGrid = IsInventoryOpen || IsShopOpen || IsMergeOpen;
        if (bagGrid != null) bagGrid.SetActive(showBagGrid);

        bool shouldHideHUD =
            IsInventoryOpen ||
            IsShopOpen ||
            IsMergeOpen ||
            _upgradeOpen ||
            (passiveScreenUI != null && passiveScreenUI.IsOpen) ||
            (gamblerScreenUI != null && gamblerScreenUI.IsOpen);

        if (hud != null) hud.SetActive(!shouldHideHUD);
    }

    private void OnPlayerDeath()
    {
        if (gameOverScreen != null)
            gameOverScreen.SetActive(true);
    }
}