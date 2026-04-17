using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject hud;
    [SerializeField] private InventoryUI inventoryUI;

    private GameObject inventoryPanel;
    private PassiveScreenUI passiveScreenUI;
    private ShopUI _activeShopUI;
    private MergeUI _activeMergeUI;
    private GamblerScreenUI gamblerScreenUI;
    private UpgradeStationUI _upgradeStationUI;
    private LootRewardUI _lootRewardUI;
    private HubStorageUI _storageUI;
    private EndGameUI _endGameUI;
    private PlayerStats playerStats;
    private bool _upgradeOpen;

    public bool isInBattle { get; set; }
    public bool IsInventoryOpen { get; private set; }
    public static bool IsRightPanelOpen { get; private set; }
    public bool IsPassiveOpen => passiveScreenUI != null && passiveScreenUI.IsOpen;
    public bool IsGamblerOpen => gamblerScreenUI != null && gamblerScreenUI.IsOpen;
    public bool IsCardPhaseOpen => gamblerScreenUI != null && gamblerScreenUI.IsCardPhaseOpen;
    public bool IsShopOpen => _activeShopUI != null && _activeShopUI.gameObject.activeSelf;
    public bool IsMergeOpen => _activeMergeUI != null && _activeMergeUI.gameObject.activeSelf;
    public bool IsUpgradeOpen => _upgradeOpen;
    public bool IsStorageOpen => _storageUI != null && _storageUI.IsOpen;

    public PassiveScreenUI GetPassiveScreen() => passiveScreenUI;
    public GamblerScreenUI GetGamblerScreen() => gamblerScreenUI;

    private float holdTime = 0f;
    private float holdDuration = 1f;

    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null) playerStats.OnPlayerDeath += OnPlayerDeath;

        inventoryPanel = GameObject.FindWithTag("InventoryPanel");
        inventoryUI = inventoryUI != null ? inventoryUI : FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>(FindObjectsInactive.Include);
        gamblerScreenUI = FindFirstObjectByType<GamblerScreenUI>(FindObjectsInactive.Include);
        _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);
        _lootRewardUI = FindFirstObjectByType<LootRewardUI>(FindObjectsInactive.Include);

        var shopUI = FindFirstObjectByType<ShopUI>(FindObjectsInactive.Include);
        var mergeUI = FindFirstObjectByType<MergeUI>(FindObjectsInactive.Include);
        var sellUI = FindFirstObjectByType<SellConfirmationUI>(FindObjectsInactive.Include);
        _storageUI = FindFirstObjectByType<HubStorageUI>(FindObjectsInactive.Include);
        _endGameUI = FindFirstObjectByType<EndGameUI>(FindObjectsInactive.Include);

        // Force Awake on all panels, then hide
        SetActive(inventoryPanel, true);
        SetActive(shopUI?.gameObject, true);
        SetActive(mergeUI?.gameObject, true);
        SetActive(_upgradeStationUI?.gameObject, true);
        SetActive(_lootRewardUI?.gameObject, true);
        SetActive(sellUI?.gameObject, true);

        Canvas.ForceUpdateCanvases();

        SetActive(inventoryPanel, false);
        SetActive(shopUI?.gameObject, false);
        SetActive(mergeUI?.gameObject, false);
        SetActive(_upgradeStationUI?.gameObject, false);
        SetActive(_lootRewardUI?.gameObject, false);
        SetActive(sellUI?.gameObject, false);
    }

    private static void SetActive(GameObject go, bool active) { if (go != null) go.SetActive(active); }

    private void Update()
    {
        if (isInBattle) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.I].wasPressedThisFrame)
        {
            if (IsCardPhaseOpen) return;
            if (IsGamblerOpen) CloseGambler();
            else if (IsStorageOpen) CloseStorage();
            else if (!IsPassiveOpen) ToggleInventory();
            else ClosePassive();
        }

        if (kb[Key.Escape].wasPressedThisFrame)
        {
            if (IsCardPhaseOpen) return;
            if (IsPassiveOpen) ClosePassive();
            else if (IsGamblerOpen) CloseGambler();
            else if (IsStorageOpen) CloseStorage();
            else if (_upgradeOpen) return;
            else if (IsMergeOpen) { CloseMerge(); CloseInventoryImmediate(); }
            else if (IsShopOpen) { CloseShop(); CloseInventoryImmediate(); }
            else if (IsInventoryOpen) ToggleInventory();
        }

        if (IsInventoryOpen && inventoryUI != null)
        {
            if (kb[Key.Digit1].wasPressedThisFrame) inventoryUI.SwitchTab(InventoryUI.Tab.Inventory);
            if (kb[Key.Digit2].wasPressedThisFrame) inventoryUI.SwitchTab(InventoryUI.Tab.PlayerStat);
            if (kb[Key.Digit3].wasPressedThisFrame) inventoryUI.SwitchTab(InventoryUI.Tab.Skill);
            if (kb[Key.Digit4].wasPressedThisFrame) inventoryUI.SwitchTab(InventoryUI.Tab.Quest);
            if (kb[Key.Digit5].wasPressedThisFrame) inventoryUI.SwitchTab(InventoryUI.Tab.Menu);
        }

        if (IsPassiveOpen)
        {
            if (kb[Key.R].isPressed)
            {
                holdTime += Time.deltaTime;
                if (holdTime >= holdDuration) { passiveScreenUI.OnResetHeld(); holdTime = 0f; }
            }
            else holdTime = 0f;
        }
    }

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError("[UIManager] ToggleInventory called but inventoryPanel is null — is the InventoryPanel tag set?");
            return;
        }

        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();

        if (IsInventoryOpen)
        {
            if (ModuleItemUI.IsDragging) return;
            CloseInventoryImmediate();
        }
        else
        {
            IsInventoryOpen = true;
            inventoryPanel.SetActive(true);
            inventoryUI?.SwitchTab(InventoryUI.Tab.Inventory);
            UpdatePanelVisibility();
        }
    }

    private void CloseInventoryImmediate()
    {
        IsInventoryOpen = false;
        inventoryPanel.SetActive(false);

        ModuleTooltipUI.Instance?.Hide();
        inventoryUI?.RestoreBagItemRefs();
        DiscardGridUI.Instance?.ForceHide();

        UpdatePanelVisibility();
    }

    public void OpenShop(ShopUI shop)
    {
        if (IsMergeOpen) CloseMerge();
        DiscardGridUI.Instance?.ForceHide();

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
        if (IsShopOpen) CloseShop();
        DiscardGridUI.Instance?.ForceHide();

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
        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();

        if (_upgradeStationUI == null)
            _upgradeStationUI = FindFirstObjectByType<UpgradeStationUI>(FindObjectsInactive.Include);
        if (_upgradeStationUI == null) { Debug.LogError("[UIManager] UpgradeStationUI not found!"); return; }

        _upgradeOpen = true;
        DiscardGridUI.Instance?.ForceHide();
        _upgradeStationUI.gameObject.SetActive(true);
        _upgradeStationUI.Open(station);
        UpdatePanelVisibility();
    }

    public void CloseUpgrade()
    {
        if (_upgradeStationUI != null) _upgradeStationUI.gameObject.SetActive(false);
        _upgradeOpen = false;
        UpdatePanelVisibility();
    }

    public void OpenRewardLoot(RandomLoot station, System.Collections.Generic.List<TestModuleEntry> rolled)
    {
        if (_lootRewardUI == null)
            _lootRewardUI = FindFirstObjectByType<LootRewardUI>(FindObjectsInactive.Include);
        if (_lootRewardUI == null) { Debug.LogError("[UIManager] LootRewardUI not found!"); return; }

        _lootRewardUI.gameObject.SetActive(true);
        DiscardGridUI.Instance?.ForceHide();
        _lootRewardUI.Open(station, rolled);
        UpdatePanelVisibility();
    }

    public void CloseRewardLoot()
    {
        if (_lootRewardUI != null) _lootRewardUI.gameObject.SetActive(false);
        UpdatePanelVisibility();
    }

    public void OpenStorage()
    {
        _storageUI.Open();
        playerStats?.SetDebugUI(false);
        UpdatePanelVisibility();
    }

    public void CloseStorage()
    {
        _storageUI.Close();
        playerStats?.SetDebugUI(true);
        UpdatePanelVisibility();
    }

    public void OpenGambler(GenericTreeConfig config, object owner, GamblerStation station)
    {
        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();
        if (IsInventoryOpen) CloseInventoryImmediate();

        if (gamblerScreenUI == null)
            gamblerScreenUI = FindFirstObjectByType<GamblerScreenUI>(FindObjectsInactive.Include);
        if (gamblerScreenUI == null) { Debug.LogError("[UIManager] GamblerScreenUI not found!"); return; }

        gamblerScreenUI.Open(config, owner, station);
        UpdatePanelVisibility();
    }

    public void CloseGambler()
    {
        if (gamblerScreenUI != null && gamblerScreenUI.IsOpen) gamblerScreenUI.Close();
        UpdatePanelVisibility();
    }

    public void OpenPassive(WeaponPassiveData data, WeaponData weaponData = null)
    {
        if (IsShopOpen) CloseShop();
        if (IsMergeOpen) CloseMerge();
        if (IsInventoryOpen) CloseInventoryImmediate();

        if (passiveScreenUI == null)
            passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>(FindObjectsInactive.Include);
        if (passiveScreenUI == null) { Debug.LogWarning("[UIManager] PassiveScreenUI not found!"); return; }

        passiveScreenUI.Open(data, weaponData);
        UpdatePanelVisibility();
    }

    public void ClosePassive()
    {
        if (passiveScreenUI != null && passiveScreenUI.IsOpen) passiveScreenUI.Close();
        UpdatePanelVisibility();
    }

    private void UpdatePanelVisibility()
    {
        bool anyRightPanelOpen = IsShopOpen || IsMergeOpen || _upgradeOpen || (_lootRewardUI != null && _lootRewardUI.gameObject.activeSelf);
        IsRightPanelOpen = anyRightPanelOpen;

        if (anyRightPanelOpen && !IsInventoryOpen && inventoryPanel != null)
        {
            IsInventoryOpen = true;
            inventoryPanel.SetActive(true);
            inventoryUI?.SwitchTab(InventoryUI.Tab.Inventory);
        }

        bool hideHUD = IsInventoryOpen || IsShopOpen || IsMergeOpen || _upgradeOpen
                    || IsPassiveOpen || IsGamblerOpen || IsStorageOpen;

        if (hud != null) hud.SetActive(!hideHUD);
    }

    public void ShowEndGame(bool isWin)
    {
        if (_endGameUI == null)
            _endGameUI = FindFirstObjectByType<EndGameUI>(FindObjectsInactive.Include);

        if (_endGameUI != null)
            StartCoroutine(ShowEndGameRoutine(isWin));
        else
            Debug.LogWarning("[UIManager] EndGameUI not found.");
    }

    private IEnumerator ShowEndGameRoutine(bool isWin)
    {
        var cam = CameraController.Instance;
        if (cam != null)
            yield return StartCoroutine(cam.EndgameEffect(1f));
        else
            yield return new WaitForSecondsRealtime(1f);

        _endGameUI.Show(isWin);
    }

    private void OnPlayerDeath()
    {
        isInBattle = false;
        ShowEndGame(false);
    }
}
