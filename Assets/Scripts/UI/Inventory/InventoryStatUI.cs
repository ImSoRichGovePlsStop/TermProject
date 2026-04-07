using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryStatUI : MonoBehaviour
{
    [SerializeField] private float fontSize     = 20f;
    [SerializeField] private float paddingLeft  = 12f;
    [SerializeField] private float paddingTop   = 12f;
    [SerializeField] private float spacing      = 4f;
    [SerializeField] private float valueOffset  = 80f;
    [SerializeField] private Color labelColor   = Color.gray;
    [SerializeField] private Color valueColor   = Color.white;

    private const float RefreshInterval = 0.2f;

    private PlayerStats playerStats;
    private float refreshTimer;

    private TextMeshProUGUI hp, dmg, atkSpd, movSpd, crit, critDmg, evade;

    private void Awake()
    {
        playerStats = FindFirstObjectByType<PlayerStats>();
        BuildUI();
    }

    private void OnEnable() => RefreshStats();

    private void Update()
    {
        refreshTimer += Time.deltaTime;
        if (refreshTimer < RefreshInterval) return;
        refreshTimer = 0f;
        RefreshStats();
    }

    private void BuildUI()
    {
        var container = new GameObject("StatRows", typeof(RectTransform));
        container.transform.SetParent(transform, false);

        var rt = container.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(0f, 1f);
        rt.offsetMin = new Vector2(paddingLeft, 0f);
        rt.offsetMax = new Vector2(0f, -paddingTop);

        var vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.spacing                = spacing;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;

        container.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        hp      = MakeRow(container, "HP");
        dmg     = MakeRow(container, "DMG");
        atkSpd  = MakeRow(container, "ATK SPD");
        movSpd  = MakeRow(container, "MOV SPD");
        crit    = MakeRow(container, "CRIT");
        critDmg = MakeRow(container, "CRIT DMG");
        evade   = MakeRow(container, "EVADE");
    }

    private TextMeshProUGUI MakeRow(GameObject parent, string label)
    {
        var go = new GameObject(label, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<LayoutElement>().preferredHeight = fontSize + 4f;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize           = fontSize;
        tmp.richText           = true;
        tmp.alignment          = TextAlignmentOptions.Left;
        tmp.textWrappingMode   = TextWrappingModes.NoWrap;
        tmp.text               = label;
        return tmp;
    }

    private void RefreshStats()
    {
        if (playerStats == null) return;

        hp.text      = Row("HP",       $"{playerStats.CurrentHealth:F0}/{playerStats.MaxHealth:F0}");
        dmg.text     = Row("DMG",      $"{playerStats.Damage:F1}");
        atkSpd.text  = Row("ATK SPD",  $"{playerStats.AttackSpeed * 100:F0}%");
        movSpd.text  = Row("MOV SPD",  $"{playerStats.MoveSpeed:F2}");
        crit.text    = Row("CRIT",     $"{playerStats.CritChance * 100:F0}%");
        critDmg.text = Row("CRIT DMG", $"{playerStats.CritDamage * 100:F0}%");
        evade.text   = Row("EVADE",    $"{playerStats.EvadeChance * 100:F0}%");
    }

    private string Row(string label, string value)
    {
        string lc = ColorUtility.ToHtmlStringRGB(labelColor);
        string vc = ColorUtility.ToHtmlStringRGB(valueColor);
        return $"<color=#{lc}>{label}</color><pos={valueOffset}><color=#{vc}>{value}</color>";
    }
}
