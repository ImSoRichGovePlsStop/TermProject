using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagGridUpgradeUI : MonoBehaviour
{
    [SerializeField] private BagGridUpgradeConfig config;

    private const float CellSize    = 60f;
    private const float CellSpacing = 3f;

    private TextMeshProUGUI LevelLabel        => transform.Find("LevelLabel")?.GetComponent<TextMeshProUGUI>();
    private TextMeshProUGUI TitleLabel        => transform.Find("TitleLabel")?.GetComponent<TextMeshProUGUI>();
    private GameObject      NormalView        => transform.Find("NormalView")?.gameObject;
    private RectTransform   CurrGrid          => transform.Find("NormalView/GridsRow/CurrBlock/CurrGridContainer")?.GetComponent<RectTransform>();
    private RectTransform   NextGrid          => transform.Find("NormalView/GridsRow/NextBlock/NextGridContainer")?.GetComponent<RectTransform>();
    private Transform       ReqContainer      => transform.Find("NormalView/RequireRow/RequireItemsContainer");
    private TextMeshProUGUI SlotsBadge        => transform.Find("NormalView/RequireRow/SlotsBadgeText")?.GetComponent<TextMeshProUGUI>();
    private Button          UpgradeButton     => transform.Find("NormalView/UpgradeButton")?.GetComponent<Button>();
    private GameObject      MaxView           => transform.Find("MaxView")?.gameObject;
    private RectTransform   MaxGrid           => transform.Find("MaxView/MaxGridContainer")?.GetComponent<RectTransform>();

    private static readonly Color ColCurr    = new Color(0.55f, 0.85f, 0.55f);
    private static readonly Color ColNext    = new Color(0.45f, 0.60f, 0.90f);
    private static readonly Color ColNew     = Color.cyan;
    private static readonly Color ColOk      = new Color(0.63f, 0.88f, 0.56f);
    private static readonly Color ColBad     = new Color(0.88f, 0.35f, 0.35f);

    private int _lastUpgradeFrame = -1;

    private void OnEnable()
    {
        SetupLayout();
        Refresh();
    }

    private void SetupLayout()
    {
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 16f);
        rt.offsetMax = new Vector2(-16f, -16f);

        var vlg = GetComponent<VerticalLayoutGroup>() ?? gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment         = TextAnchor.UpperCenter;
        vlg.padding                = new RectOffset(12, 12, 12, 12);
        vlg.spacing                = 10f;
        vlg.childControlWidth      = true;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight     = false;
        vlg.childForceExpandHeight = false;

        var row = transform.Find("NormalView/GridsRow");
        if (row != null)
        {
            var hlg = row.GetComponent<HorizontalLayoutGroup>() ?? row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment         = TextAnchor.UpperCenter;
            hlg.spacing                = 300f;
            hlg.childControlWidth      = false;
            hlg.childForceExpandWidth  = false;
            hlg.childControlHeight     = false;
            hlg.childForceExpandHeight = false;
        }
    }

    public void Refresh()
    {
        if (config == null || config.levels == null || config.levels.Length == 0) return;

        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        int lvl   = mgr.BagGridLevel;
        int maxLvl = config.levels.Length - 1;
        bool isMax = lvl >= maxLvl;

        var ll = LevelLabel;
        var tl = TitleLabel;
        if (ll != null) ll.text = $"Lv. {lvl}";
        if (tl != null) tl.text = "Upgrade Bag Grid";

        NormalView?.SetActive(!isMax);
        MaxView?.SetActive(isMax);

        if (!isMax)
        {
            var curr = config.levels[lvl];
            var next = config.levels[lvl + 1];

            BuildMiniGrid(CurrGrid, curr.cols, curr.rows, false, 0, 0);
            BuildMiniGrid(NextGrid, next.cols, next.rows, true, curr.cols, curr.rows);

            var sb = SlotsBadge;
            if (sb != null) sb.text = $"+{next.cols * next.rows - curr.cols * curr.rows} slots";

            BuildRequirements(next.cost);

            var btn = UpgradeButton;
            if (btn != null) btn.interactable = MaterialStorage.Instance.HasEnoughAll(next.cost);
        }
        else
        {
            var curr     = config.levels[lvl];
            var maxViewGo = MaxView;

            if (maxViewGo != null)
            {
                var maxRt = maxViewGo.GetComponent<RectTransform>();
                maxRt.anchorMin = Vector2.zero;
                maxRt.anchorMax = Vector2.one;
                maxRt.offsetMin = Vector2.zero;
                maxRt.offsetMax = Vector2.zero;

                var maxVlg = maxViewGo.GetComponent<VerticalLayoutGroup>() ?? maxViewGo.AddComponent<VerticalLayoutGroup>();
                maxVlg.childAlignment         = TextAnchor.MiddleCenter;
                maxVlg.childControlWidth      = false;
                maxVlg.childForceExpandWidth  = false;
                maxVlg.childControlHeight     = false;
                maxVlg.childForceExpandHeight = false;
            }

            var maxGrid = MaxGrid;
            if (maxGrid != null)
            {
                maxGrid.anchorMin = new Vector2(0.5f, 0.5f);
                maxGrid.anchorMax = new Vector2(0.5f, 0.5f);
                maxGrid.pivot     = new Vector2(0.5f, 0.5f);
            }

            BuildMiniGrid(maxGrid, curr.cols, curr.rows, true, 0, 0);
        }
    }

    public void OnUpgradeClicked()
    {
        if (Time.frameCount == _lastUpgradeFrame) return;
        _lastUpgradeFrame = Time.frameCount;

        if (config == null) return;
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        int lvl = mgr.BagGridLevel;
        if (lvl >= config.levels.Length - 1) return;

        var next = config.levels[lvl + 1];
        if (!MaterialStorage.Instance.HasEnoughAll(next.cost)) return;

        MaterialStorage.Instance.RemoveAll(next.cost);
        mgr.UpgradeBagGrid(next.cols, next.rows);
        Refresh();
    }

    private void BuildMiniGrid(RectTransform container, int cols, int rows, bool isNext, int prevCols, int prevRows)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);

        float w = cols * CellSize + Mathf.Max(0, cols - 1) * CellSpacing;
        float h = rows * CellSize + Mathf.Max(0, rows - 1) * CellSpacing;
        container.sizeDelta = new Vector2(w, h);
        container.anchorMin = new Vector2(0f, 1f);
        container.anchorMax = new Vector2(0f, 1f);
        container.pivot     = new Vector2(0f, 1f);

        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            bool isNew = isNext && (r >= prevRows || c >= prevCols);
            var go = new GameObject($"c{c}r{r}", typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(container, false);
            var img = go.GetComponent<Image>();
            img.color         = isNew ? ColNew : (isNext ? ColNext : ColCurr);
            img.raycastTarget = false;

            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0f, 1f);
            rt.sizeDelta        = new Vector2(CellSize, CellSize);
            rt.anchoredPosition = new Vector2(c * (CellSize + CellSpacing), -r * (CellSize + CellSpacing));
        }
    }

    private void BuildRequirements(MaterialRequirement[] reqs)
    {
        var container = ReqContainer;
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        if (reqs == null || reqs.Length == 0) return;

        foreach (var req in reqs)
        {
            if (req.material == null) continue;

            int have    = MaterialStorage.Instance.GetAll().TryGetValue(req.material, out var v) ? v : 0;
            Color col   = have >= req.count ? ColOk : ColBad;

            var item = new GameObject("ReqItem", typeof(RectTransform));
            var vlg  = item.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment        = TextAnchor.UpperCenter;
            vlg.childControlWidth     = true;
            vlg.childControlHeight    = false;
            vlg.childForceExpandWidth = false;
            vlg.spacing               = 4f;
            var fitter = item.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            item.transform.SetParent(container, false);

            var nameGo  = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(item.transform, false);
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.text          = req.material.moduleName;
            nameTmp.fontSize      = 40f;
            nameTmp.fontStyle     = FontStyles.Bold;
            nameTmp.color         = col;
            nameTmp.alignment     = TextAlignmentOptions.Center;
            nameTmp.raycastTarget = false;

            var countGo  = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGo.transform.SetParent(item.transform, false);
            var countTmp = countGo.GetComponent<TextMeshProUGUI>();
            countTmp.text          = $"{have} / {req.count}";
            countTmp.fontSize      = 40f;
            countTmp.color         = col;
            countTmp.alignment     = TextAlignmentOptions.Center;
            countTmp.raycastTarget = false;
        }
    }
}
