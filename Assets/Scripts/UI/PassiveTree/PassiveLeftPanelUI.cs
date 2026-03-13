using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PassiveLeftPanelUI : MonoBehaviour
{
    [Header("Weapon Info")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI weaponLevelText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI dmgText;
    [SerializeField] private TextMeshProUGUI spdText;
    [SerializeField] private TextMeshProUGUI critText;

    [Header("Upgrade")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TextMeshProUGUI upgradeButtonText;

    private WeaponPassiveManager manager;
    private PlayerStats playerStats;
    private WeaponPassiveData currentData;

    private void Awake()
    {
        manager = FindFirstObjectByType<WeaponPassiveManager>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        upgradeButton.onClick.AddListener(OnUpgradeClick);

        weaponNameText.fontSize = 48;
        weaponNameText.fontStyle = FontStyles.Bold;
        weaponNameText.color = Color.white;

        weaponLevelText.fontSize = 28;
        weaponLevelText.color = new Color(1f, 0.82f, 0.2f);

        Color statColor = new Color(0.85f, 0.85f, 0.85f);
        int statFontSize = 24;
        foreach (var t in new[] { hpText, dmgText, spdText, critText })
        {
            t.fontSize = statFontSize;
            t.color = statColor;
        }

        upgradeButton.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        upgradeButtonText.fontSize = 32;
        upgradeButtonText.fontStyle = FontStyles.Bold;
        upgradeButtonText.color = new Color(1f, 0.82f, 0.2f);
    }

    public void Setup(WeaponPassiveData data, WeaponData weaponData)
    {
        currentData = data;

        weaponNameText.text = weaponData != null ? weaponData.weaponName : "Unknown";
        if (weaponIcon != null && weaponData != null && weaponData.icon != null)
            weaponIcon.sprite = weaponData.icon;

        Refresh();
    }

    public void Refresh()
    {
        if (currentData == null) return;

        int level = manager.GetLevel(currentData);
        weaponLevelText.text = $"Lv. {level}";

        // stats
        if (playerStats != null)
        {
            hpText.text = $"HP:  {playerStats.MaxHealth:F0}";
            dmgText.text = $"DMG:  {playerStats.Damage:F1}";
            spdText.text = $"SPD:  {playerStats.MoveSpeed:F2}";
            critText.text = $"CRIT:  {playerStats.CritChance:P0} / {playerStats.CritDamage:P0}";
        }

        // upgrade button
        bool canLevelUp = manager.CanLevelUp(currentData);
        upgradeButton.interactable = canLevelUp;
        int nextLevel = manager.GetLevel(currentData) + 1;
        if (canLevelUp)
        {
            int pts = nextLevel <= 15 ? GetPointsForLevel(nextLevel) : 0;
            upgradeButtonText.text = $"Upgrade (Lv.{nextLevel} +{pts}pt)";
        }
        else
        {
            upgradeButtonText.text = "Max Level";
        }
    }

    private void OnUpgradeClick()
    {
        if (manager.TryLevelUp(currentData))
            Refresh();

        FindFirstObjectByType<PassiveScreenUI>()?.RefreshPoints();
        FindFirstObjectByType<PassiveScreenUI>()?.RefreshAll();
    }

    private int GetPointsForLevel(int level)
    {
        int[] table = { 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 3, 4, 4, 4, 5 };
        if (level < 0 || level >= table.Length) return 0;
        return table[level];
    }
}