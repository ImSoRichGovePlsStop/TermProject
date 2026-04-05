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

    private void RefreshStats()
    {
        if (playerStats == null) return;

        float hpRatio = playerStats.MaxHealth > 0f
            ? playerStats.CurrentHealth / playerStats.MaxHealth
            : 1f;
        string hpColor = hpRatio > 0.6f ? "#33CC33" : hpRatio > 0.3f ? "#CCAA00" : "#CC2222";

        hp.text      = $"HP<pos=42%><color={hpColor}>{playerStats.CurrentHealth:F0} / {playerStats.MaxHealth:F0}</color>";
        dmg.text     = $"DMG<pos=42%><color=#CC7000>{playerStats.Damage:F1}</color>";
        atkSpd.text  = $"ATK SPD<pos=42%><color=#0099CC>{playerStats.AttackSpeed:F2}</color>";
        movSpd.text  = $"MOV SPD<pos=42%><color=#5AAABB>{playerStats.MoveSpeed:F2}</color>";
        crit.text    = $"CRIT<pos=42%><color=#CCAA00>{playerStats.CritChance:P0}</color>";
        critDmg.text = $"CRIT DMG<pos=42%><color=#CC4020>{playerStats.CritDamage:P0}</color>";
        evade.text   = $"EVADE<pos=42%><color=#66BB66>{playerStats.EvadeChance:P0}</color>";
    }
}
