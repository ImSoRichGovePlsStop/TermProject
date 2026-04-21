using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class LuckStationUpgradeUI : MonoBehaviour
{
    public static LuckStationUpgradeUI Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Transform levelListContainer;
    [SerializeField] private GameObject upgradeSection;
    [SerializeField] private TextMeshProUGUI upgradeNextText;
    [SerializeField] private RequiredMaterialsUI reqMaterialsUI;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private GameObject maxLevelSection;

    [Header("Row Styling")]
    [SerializeField] private float rowHeight = 90f;
    [SerializeField] private int labelFontSize = 80;
    [SerializeField] private int descFontSize = 36;
    [SerializeField] private float rowSpacing = 24f;

    private readonly List<(GameObject root, TextMeshProUGUI label, TextMeshProUGUI desc)> rows = new();

    private static readonly Color ColUnlockedLabel = new(0.78f, 0.66f, 0.33f);
    private static readonly Color ColUnlockedDesc = new(0.78f, 0.66f, 0.33f);
    private static readonly Color ColFutureLabel = new(0.45f, 0.45f, 0.45f);
    private static readonly Color ColFutureDesc = new(0.35f, 0.35f, 0.35f);
    private static readonly Color ColBgUnlocked = new(0.18f, 0.12f, 0.04f);
    private static readonly Color ColBgFuture = new(0.12f, 0.12f, 0.12f);
    private static readonly Color ColGold = new(0.96f, 0.85f, 0.48f);
    private static readonly Color ColRed = new(0.89f, 0.22f, 0.22f);

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        BuildRows();
    }

    public void Open() { IsOpen = true; panel.SetActive(true); Refresh(); }
    public void Close() { IsOpen = false; panel.SetActive(false); }

    public void Refresh()
    {
        var mgr = LuckStationManager.Instance;
        if (mgr == null) return;
        if (rows.Count == 0) BuildRows();
        RefreshRows(mgr);
        RefreshUpgrade(mgr);
    }

    private void BuildRows()
    {
        foreach (var (r, _, _) in rows) if (r != null) DestroyImmediate(r);
        rows.Clear();
        var mgr = LuckStationManager.Instance;
        if (mgr == null) return;
        for (int i = 1; i <= mgr.MaxLevel; i++)
            rows.Add(CreateRow(i, mgr.GetLevelData(i)?.description ?? ""));
    }

    private (GameObject, TextMeshProUGUI, TextMeshProUGUI) CreateRow(int level, string desc)
    {
        var row = new GameObject($"Level{level}Row", typeof(RectTransform));
        row.transform.SetParent(levelListContainer, false);

        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = rowHeight;
        le.minHeight = rowHeight;

        var bg = row.AddComponent<Image>();
        bg.color = ColBgFuture;

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(12, 12, 8, 8);
        hlg.spacing = rowSpacing;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandHeight = false;

        var lblObj = new GameObject("Label", typeof(RectTransform));
        lblObj.transform.SetParent(row.transform, false);
        var lblLE = lblObj.AddComponent<LayoutElement>();
        lblLE.minWidth = lblLE.preferredWidth = 64;
        var lbl = lblObj.AddComponent<TextMeshProUGUI>();
        lbl.text = $"Lv. {level}";
        lbl.fontSize = labelFontSize;
        lbl.fontStyle = FontStyles.Bold;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.textWrappingMode = TextWrappingModes.NoWrap;

        var dscObj = new GameObject("Desc", typeof(RectTransform));
        dscObj.transform.SetParent(row.transform, false);
        dscObj.AddComponent<LayoutElement>().flexibleWidth = 1;
        var dsc = dscObj.AddComponent<TextMeshProUGUI>();
        dsc.text = desc;
        dsc.fontSize = descFontSize;
        dsc.alignment = TextAlignmentOptions.Left;

        return (row, lbl, dsc);
    }

    private void RefreshRows(LuckStationManager mgr)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            var (row, lbl, dsc) = rows[i];
            bool unlocked = (i + 1) <= mgr.CurrentLevel;
            row.GetComponent<Image>().color = unlocked ? ColBgUnlocked : ColBgFuture;
            lbl.color = unlocked ? ColUnlockedLabel : ColFutureLabel;
            dsc.color = unlocked ? ColUnlockedDesc : ColFutureDesc;
        }
    }

    private void RefreshUpgrade(LuckStationManager mgr)
    {
        bool atMax = mgr.CurrentLevel >= mgr.MaxLevel;
        upgradeSection?.SetActive(true);
        upgradeNextText?.gameObject.SetActive(!atMax);
        reqMaterialsUI?.gameObject.SetActive(!atMax);
        upgradeButton?.gameObject.SetActive(!atMax);
        maxLevelSection?.SetActive(atMax);
        if (atMax) return;

        upgradeNextText.text = $"Lv. {mgr.CurrentLevel} \u2192 Lv. {mgr.CurrentLevel + 1}";
        upgradeButton.interactable = mgr.CanUpgrade();
        reqMaterialsUI?.Show(mgr.GetUpgradeCost());
    }

    private static int GetStock(MaterialData data)
    {
        var all = MaterialStorage.Instance.GetAll();
        return all.TryGetValue(data, out int n) ? n : 0;
    }

    private void OnUpgradeClicked()
    {
        if (LuckStationManager.Instance?.TryUpgrade() == true) Refresh();
    }
}