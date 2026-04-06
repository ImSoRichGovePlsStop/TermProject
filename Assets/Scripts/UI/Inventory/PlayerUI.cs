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

    private void RefreshStats()
    {
        if (playerStats == null) return;

        hp.text      = $"HP<pos=42%><color={BaseColor}>{playerStats.BaseHP:F0}</color>{Bonus(playerStats.MaxHealth - playerStats.BaseHP, "F0")}";
        dmg.text     = $"DMG<pos=42%><color={BaseColor}>{playerStats.BaseDMG:F1}</color>{Bonus(playerStats.Damage - playerStats.BaseDMG)}";
        atkSpd.text  = $"ATK SPD<pos=42%><color={BaseColor}>{playerStats.BaseATKSPD:F2}</color>{Bonus(playerStats.AttackSpeed - playerStats.BaseATKSPD, "F2")}";
        movSpd.text  = $"MOV SPD<pos=42%><color={BaseColor}>{playerStats.BaseMOVSPD:F2}</color>{Bonus(playerStats.MoveSpeed - playerStats.BaseMOVSPD, "F2")}";
        crit.text    = $"CRIT<pos=42%><color={BaseColor}>{playerStats.BaseCrit:P0}</color>{Bonus(playerStats.CritChance - playerStats.BaseCrit, "P0")}";
        critDmg.text = $"CRIT DMG<pos=42%><color={BaseColor}>{playerStats.BaseCritDMG:P0}</color>{Bonus(playerStats.CritDamage - playerStats.BaseCritDMG, "P0")}";
        evade.text   = $"EVADE<pos=42%><color={BaseColor}>{playerStats.BaseEvade:P0}</color>{Bonus(playerStats.EvadeChance - playerStats.BaseEvade, "P0")}";
    }
}
