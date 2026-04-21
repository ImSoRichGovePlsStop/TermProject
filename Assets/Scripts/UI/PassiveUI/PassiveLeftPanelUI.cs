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
    private WeaponData currentWeaponData;

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
        currentWeaponData = weaponData;

        weaponNameText.text = weaponData != null ? weaponData.weaponName : "Unknown";
        if (weaponIcon != null && weaponData != null && weaponData.icon != null)
            weaponIcon.sprite = weaponData.icon;

        Refresh();
    }

    public void Refresh()
    {
        if (currentData == null || currentWeaponData == null) return;

        int level = WeaponLevelManager.Instance?.GetLevel(currentWeaponData) ?? 1;
        weaponLevelText.text = $"Lv. {level}";

        if (playerStats != null)
        {
            hpText.text = $"HP:  {playerStats.MaxHealth:F0}";
            dmgText.text = $"DMG:  {playerStats.Damage:F1}";
            spdText.text = $"SPD:  {playerStats.MoveSpeed:F2}";
            critText.text = $"CRIT:  {playerStats.CritChance:P0} / {playerStats.CritDamage:P0}";
        }

        bool canLevelUp = WeaponLevelManager.Instance?.CanLevelUp(currentWeaponData) ?? false;
        upgradeButton.interactable = canLevelUp;
        int nextLevel = level + 1;
        if (canLevelUp)
        {
            int pts = WeaponLevelManager.Instance?.GetPointsForLevel(nextLevel) ?? 0;
            upgradeButtonText.text = "Upgrade";
        }
        else
        {
            upgradeButtonText.text = "Max Level";
        }
    }

    private void OnUpgradeClick()
    {
        Debug.Log($"[Upgrade] currentWeaponData: {currentWeaponData?.name}");
        Debug.Log($"[Upgrade] UpgradeConfirmPopupUI.Instance: {UpgradeConfirmPopupUI.Instance}");
        Debug.Log($"[Upgrade] WeaponLevelManager.Instance: {WeaponLevelManager.Instance}");

        if (currentWeaponData == null) return;
        int nextLevel = (WeaponLevelManager.Instance?.GetLevel(currentWeaponData) ?? 1) + 1;

        UpgradeConfirmPopupUI.Instance?.Show(currentWeaponData, nextLevel, () =>
        {
            if (WeaponLevelManager.Instance?.TryLevelUp(currentWeaponData) == true)
            {
                Refresh();
                FindFirstObjectByType<PassiveScreenUI>()?.RefreshPoints();
                FindFirstObjectByType<PassiveScreenUI>()?.RefreshAll();
            }
        });
    }
}