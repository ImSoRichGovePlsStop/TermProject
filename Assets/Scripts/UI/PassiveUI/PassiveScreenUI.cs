using UnityEngine;
using TMPro;

public class PassiveScreenUI : MonoBehaviour, IGenericTreeScreenUI
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private PassiveTreeUI[] treeUIs;
    [SerializeField] private PassiveLeftPanelUI leftPanelUI;

    private WeaponPassiveManager manager;
    private WeaponPassiveData currentData;
    private WeaponData currentWeaponData;
    private PlayerStats playerStats;
    private bool isSetup = false;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        panel.SetActive(false);
    }

    private void Start()
    {
        if (manager == null)
            manager = FindFirstObjectByType<WeaponPassiveManager>(FindObjectsInactive.Include);
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);
    }

    public void Open(WeaponPassiveData data, WeaponData weaponData = null)
    {
        if (manager == null)
            manager = FindFirstObjectByType<WeaponPassiveManager>(FindObjectsInactive.Include);
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);

        playerStats?.SetDebugUI(false);
        IsOpen = true;
        panel.SetActive(true);
        currentWeaponData = weaponData;

        if (!isSetup || currentData != data)
        {
            currentData = data;
            isSetup = false;
            ClearTrees();
        }

        if (!isSetup)
        {
            Setup(data);
            isSetup = true;
        }

        leftPanelUI?.Setup(data, weaponData);
        RefreshAll();
        RefreshPoints();
    }

    public void Close()
    {
        playerStats?.SetDebugUI(true);
        IsOpen = false;
        panel.SetActive(false);
        GenericTreeTooltipUI.Instance?.Hide();
        UpgradeConfirmPopupUI.Instance?.Hide();
    }

    private void ClearTrees()
    {
        foreach (var treeUI in treeUIs)
            treeUI.Clear();
    }

    private void Setup(WeaponPassiveData data)
    {
        for (int i = 0; i < treeUIs.Length; i++)
            treeUIs[i].Setup(data.trees[i], manager, this, data, tooltipAnchorLeft: i == treeUIs.Length - 1);
    }

    public void RefreshPoints()
    {
        if (currentData == null) return;
        int points = manager.GetAvailablePoints(currentData);
        pointsText.text = $"Points: {points}";
        leftPanelUI?.Refresh();
    }

    public void OnResetHeld()
    {
        int totalPoints = WeaponLevelManager.Instance.GetTotalPoints(currentWeaponData);
        manager.ResetPassive(currentData, totalPoints);
        RefreshAll();
        RefreshPoints();
    }

    public void RefreshAll()
    {
        foreach (var treeUI in treeUIs)
            treeUI.RefreshAll();
        leftPanelUI?.Refresh();
    }
}