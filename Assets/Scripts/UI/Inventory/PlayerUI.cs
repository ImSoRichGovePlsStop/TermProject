using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private Image playerImage;

    [SerializeField] private TextMeshProUGUI hp;
    [SerializeField] private TextMeshProUGUI dmg;
    [SerializeField] private TextMeshProUGUI atkSpd;
    [SerializeField] private TextMeshProUGUI movSpd;
    [SerializeField] private TextMeshProUGUI crit;
    [SerializeField] private TextMeshProUGUI critDmg;
    [SerializeField] private TextMeshProUGUI evade;

    private PlayerStats playerStats;
    private float refreshTimer;
    private const float RefreshInterval = 0.2f;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
    }

    private void OnEnable()
    {
        RefreshStats();
    }

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer >= RefreshInterval)
        {
            refreshTimer = 0f;
            RefreshStats();
        }
    }

    private const string BaseColor  = "#414141";
    private const string BonusColor = "#329632";

    private static string Bonus(float bonus, string fmt = "F1")
        => bonus > 0.01f ? $" <color={BonusColor}>+{bonus.ToString(fmt)}</color>" : "";

    private static string BonusPct(float bonus)
        => bonus > 0.0001f ? $" <color={BonusColor}>+{bonus * 100:F0}%</color>" : "";

    private void RefreshStats()
    {
        if (playerStats == null) return;

        hp.text      = $"HP<pos=42%><color={BaseColor}>{playerStats.BaseHP:F0}</color>{Bonus(playerStats.MaxHealth - playerStats.BaseHP, "F0")}";
        dmg.text     = $"DMG<pos=42%><color={BaseColor}>{playerStats.BaseDMG:F1}</color>{Bonus(playerStats.Damage - playerStats.BaseDMG)}";
        float atkSpdBonus = playerStats.AttackSpeed - playerStats.BaseATKSPD;
        string atkSpdBonusStr = atkSpdBonus > 0.01f ? $" <color={BonusColor}>+{atkSpdBonus * 100:F0}%</color>" : "";
        atkSpd.text  = $"ATK SPD<pos=42%><color={BaseColor}>{playerStats.BaseATKSPD * 100:F0}%</color>{atkSpdBonusStr}";
        movSpd.text  = $"MOV SPD<pos=42%><color={BaseColor}>{playerStats.BaseMOVSPD:F2}</color>{Bonus(playerStats.MoveSpeed - playerStats.BaseMOVSPD, "F2")}";
        crit.text    = $"CRIT<pos=42%><color={BaseColor}>{playerStats.BaseCrit * 100:F0}%</color>{BonusPct(playerStats.CritChance - playerStats.BaseCrit)}";
        critDmg.text = $"CRIT DMG<pos=42%><color={BaseColor}>{playerStats.BaseCritDMG * 100:F0}%</color>{BonusPct(playerStats.CritDamage - playerStats.BaseCritDMG)}";
        evade.text   = $"EVADE<pos=42%><color={BaseColor}>{playerStats.BaseEvade * 100:F0}%</color>{BonusPct(playerStats.EvadeChance - playerStats.BaseEvade)}";
    }
}
