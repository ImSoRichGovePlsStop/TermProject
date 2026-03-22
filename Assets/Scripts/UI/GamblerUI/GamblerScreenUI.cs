using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GamblerScreenUI : MonoBehaviour, IGenericTreeScreenUI
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GenericTreeUI[] treeUIs;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    [Header("Card Phase")]
    [SerializeField] private Button cardPhaseButton;
    [SerializeField] private CardPhaseUI cardPhaseUI;

    private GamblerManager manager;
    private GenericTreeConfig currentConfig;
    private object currentOwner;
    private GamblerStation currentStation;
    private PlayerStats playerStats;
    private bool isSetup = false;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        panel.SetActive(false);
        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClick);
        if (cardPhaseButton != null)
            cardPhaseButton.onClick.AddListener(OnCardPhaseClick);
    }

    private void Start()
    {
        if (manager == null)
            manager = FindFirstObjectByType<GamblerManager>(FindObjectsInactive.Include);
    }

    public void Open(GenericTreeConfig config, object owner, GamblerStation station)
    {
        if (manager == null)
            manager = FindFirstObjectByType<GamblerManager>(FindObjectsInactive.Include);
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>(FindObjectsInactive.Include);

        playerStats?.SetDebugUI(false);

        IsOpen = true;
        panel.SetActive(true);
        currentStation = station;

        if (!isSetup || currentConfig != config || currentOwner != owner)
        {
            currentConfig = config;
            currentOwner = owner;
            isSetup = false;
            ClearTrees();
        }

        if (!isSetup)
        {
            Setup(config, owner);
            isSetup = true;
        }

        RefreshAll();
        RefreshPoints();
        RefreshLevel();
    }

    public void Close()
    {
        playerStats?.SetDebugUI(true);
        IsOpen = false;
        panel.SetActive(false);
        GenericTreeTooltipUI.Instance?.Hide();
    }

    private void Setup(GenericTreeConfig config, object owner)
    {
        for (int i = 0; i < treeUIs.Length && i < config.trees.Length; i++)
            treeUIs[i].Setup(config.trees[i], manager, this, owner);
    }

    private void ClearTrees()
    {
        foreach (var treeUI in treeUIs)
            treeUI.Clear();
    }

    public void RefreshAll()
    {
        foreach (var treeUI in treeUIs)
            treeUI.RefreshAll();
    }

    public void RefreshPoints()
    {
        if (currentConfig == null || currentOwner == null) return;
        int points = manager.GetAvailablePoints(currentOwner);
        if (pointsText != null)
            pointsText.text = $"Points: {points}";
    }

    private void RefreshLevel()
    {
        if (currentStation == null) return;

        int level = currentStation.GetLevel();
        bool canLevelUp = currentStation.CanLevelUp();

        if (levelText != null)
            levelText.text = $"Lv. {level}";

        if (upgradeButton != null)
            upgradeButton.interactable = canLevelUp;

        if (upgradeButtonText != null)
            upgradeButtonText.text = canLevelUp ? $"Upgrade (Lv.{level + 1})" : "Max Level";
    }

    private void OnUpgradeClick()
    {
        currentStation?.TryLevelUp();
        RefreshLevel();
        RefreshPoints();
        RefreshAll();
    }

    private void OnCardPhaseClick()
    {
        cardPhaseUI?.Open();
    }

    public void OnResetHeld()
    {
        if (currentConfig == null || currentOwner == null) return;
        int totalPoints = BuildingLevelManager.Instance?.GetTotalPoints(currentOwner as BuildingData) ?? 0;
        foreach (var tree in currentConfig.trees)
        {
            if (tree == null) continue;
            manager.ResetTree(currentOwner, tree, totalPoints);
        }
        RefreshAll();
        RefreshPoints();
    }
}