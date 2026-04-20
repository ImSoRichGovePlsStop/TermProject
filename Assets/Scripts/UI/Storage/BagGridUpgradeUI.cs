using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagGridUpgradeUI : MonoBehaviour
{
    [SerializeField] private BagGridUpgradeConfig config;

    private const float CS = 28f;
    private const float SP = 2f;

    private static readonly Color ColCurr = new Color(0.55f, 0.85f, 0.55f);
    private static readonly Color ColNext = new Color(0.45f, 0.60f, 0.90f);
    private static readonly Color ColNew = Color.cyan;
    private static readonly Color ColOk = new Color(0.63f, 0.88f, 0.56f);
    private static readonly Color ColBad = new Color(0.88f, 0.35f, 0.35f);

    private int _lastUpgradeFrame = -1;
    private bool _built = false;

    private TextMeshProUGUI _levelLabel;
    private RectTransform _currGrid;
    private RectTransform _nextGrid;
    private Transform _reqContainer;
    private TextMeshProUGUI _slotsBadge;
    private Button _upgradeButton;
    private GameObject _normalView;
    private GameObject _maxView;
    private RectTransform _maxGrid;

    private void OnEnable()
    {
        if (_built) Refresh();
    }

    public void Init()
    {
        if (_built) return;
        Build();
    }

    // ??? Build ??????????????????????????????????????????????????????????????

    private void Build()
    {
        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, 16f);
        rt.offsetMax = new Vector2(-16f, -16f);

        // Level label top-left
        var lvlGo = new GameObject("LevelLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlGo.transform.SetParent(transform, false);
        var lvlRt = lvlGo.GetComponent<RectTransform>();
        lvlRt.anchorMin = new Vector2(0f, 1f);
        lvlRt.anchorMax = new Vector2(0.5f, 1f);
        lvlRt.pivot = new Vector2(0f, 1f);
        lvlRt.offsetMin = new Vector2(4f, -26f);
        lvlRt.offsetMax = new Vector2(0f, 0f);
        _levelLabel = lvlGo.GetComponent<TextMeshProUGUI>();
        _levelLabel.fontSize = 16f;
        _levelLabel.color = new Color(0.7f, 0.7f, 0.7f);
        _levelLabel.raycastTarget = false;

        // Subtitle center
        var subGo = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(transform, false);
        var subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.offsetMin = new Vector2(0f, -50f);
        subRt.offsetMax = new Vector2(0f, -26f);
        var subTmp = subGo.GetComponent<TextMeshProUGUI>();
        subTmp.text = "Upgrade Bag Grid";
        subTmp.fontSize = 17f;
        subTmp.fontStyle = FontStyles.Bold;
        subTmp.color = Color.white;
        subTmp.alignment = TextAlignmentOptions.Center;
        subTmp.raycastTarget = false;

        // ?? Normal view ??????????????????????????????????????????????????
        _normalView = new GameObject("NormalView", typeof(RectTransform));
        _normalView.transform.SetParent(transform, false);
        var nvRt = _normalView.GetComponent<RectTransform>();
        nvRt.anchorMin = new Vector2(0f, 0f);
        nvRt.anchorMax = new Vector2(1f, 1f);
        nvRt.offsetMin = new Vector2(0f, 130f); // bottom: space for btn+req
        nvRt.offsetMax = new Vector2(0f, -54f);  // top: space for title

        // "Current" label top of normalView
        MakeLabel(_normalView.transform, "Current",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -20f), new Vector2(0f, 0f),
            13f, new Color(0.65f, 0.65f, 0.65f), TextAlignmentOptions.Center);

        // Current grid — anchored top-center
        var currGo = new GameObject("CurrGrid", typeof(RectTransform));
        currGo.transform.SetParent(_normalView.transform, false);
        _currGrid = currGo.GetComponent<RectTransform>();
        _currGrid.anchorMin = new Vector2(0.5f, 1f);
        _currGrid.anchorMax = new Vector2(0.5f, 1f);
        _currGrid.pivot = new Vector2(0.5f, 1f);
        _currGrid.anchoredPosition = new Vector2(0f, -24f);
        _currGrid.sizeDelta = Vector2.zero;

        // Arrow — middle
        var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGo.transform.SetParent(_normalView.transform, false);
        var arrowRt = arrowGo.GetComponent<RectTransform>();
        arrowRt.anchorMin = new Vector2(0f, 0.5f);
        arrowRt.anchorMax = new Vector2(1f, 0.5f);
        arrowRt.pivot = new Vector2(0.5f, 0.5f);
        arrowRt.sizeDelta = new Vector2(0f, 30f);
        arrowRt.anchoredPosition = Vector2.zero;
        var arrowTmp = arrowGo.GetComponent<TextMeshProUGUI>();
        arrowTmp.text = "v";
        arrowTmp.fontSize = 20f;
        arrowTmp.color = new Color(0.55f, 0.55f, 0.55f);
        arrowTmp.alignment = TextAlignmentOptions.Center;
        arrowTmp.raycastTarget = false;

        // "Next" label bottom of normalView
        MakeLabel(_normalView.transform, "Next",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 22f), new Vector2(0f, 42f),
            13f, new Color(0.65f, 0.65f, 0.65f), TextAlignmentOptions.Center);

        // Next grid — anchored bottom-center
        var nextGo = new GameObject("NextGrid", typeof(RectTransform));
        nextGo.transform.SetParent(_normalView.transform, false);
        _nextGrid = nextGo.GetComponent<RectTransform>();
        _nextGrid.anchorMin = new Vector2(0.5f, 0f);
        _nextGrid.anchorMax = new Vector2(0.5f, 0f);
        _nextGrid.pivot = new Vector2(0.5f, 0f);
        _nextGrid.anchoredPosition = new Vector2(0f, 46f);
        _nextGrid.sizeDelta = Vector2.zero;

        // Slots badge
        var slotGo = new GameObject("SlotsBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotGo.transform.SetParent(transform, false);
        var slotRt = slotGo.GetComponent<RectTransform>();
        slotRt.anchorMin = new Vector2(0f, 0f);
        slotRt.anchorMax = new Vector2(1f, 0f);
        slotRt.pivot = new Vector2(0.5f, 0f);
        slotRt.offsetMin = new Vector2(0f, 122f);
        slotRt.offsetMax = new Vector2(0f, 144f);
        _slotsBadge = slotGo.GetComponent<TextMeshProUGUI>();
        _slotsBadge.fontSize = 13f;
        _slotsBadge.color = new Color(0.5f, 0.9f, 1f);
        _slotsBadge.alignment = TextAlignmentOptions.Center;
        _slotsBadge.raycastTarget = false;

        // "Require" label
        MakeLabel(transform, "Require",
            new Vector2(0f, 0f), new Vector2(0.5f, 0f),
            new Vector2(4f, 92f), new Vector2(0f, 114f),
            12f, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Center);

        // Req container
        var reqGo = new GameObject("ReqContainer", typeof(RectTransform));
        reqGo.transform.SetParent(transform, false);
        var reqRt = reqGo.GetComponent<RectTransform>();
        reqRt.anchorMin = new Vector2(0f, 0f);
        reqRt.anchorMax = new Vector2(1f, 0f);
        reqRt.pivot = new Vector2(0.5f, 0f);
        reqRt.offsetMin = new Vector2(12f, 52f);
        reqRt.offsetMax = new Vector2(-12f, 88f);
        _reqContainer = reqGo.transform;
        var hlg = reqGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 10f;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandHeight = false;

        // Upgrade button
        var btnGo = new GameObject("UpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.05f, 0f);
        btnRt.anchorMax = new Vector2(0.95f, 0f);
        btnRt.pivot = new Vector2(0.5f, 0f);
        btnRt.offsetMin = new Vector2(0f, 8f);
        btnRt.offsetMax = new Vector2(0f, 46f);
        btnGo.GetComponent<Image>().color = new Color(0.95f, 0.80f, 0.20f);
        _upgradeButton = btnGo.GetComponent<Button>();
        var bc = _upgradeButton.colors;
        bc.normalColor = new Color(0.95f, 0.80f, 0.20f);
        bc.highlightedColor = new Color(1f, 0.90f, 0.35f);
        bc.pressedColor = new Color(0.75f, 0.62f, 0.10f);
        bc.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.6f);
        _upgradeButton.colors = bc;
        _upgradeButton.onClick.AddListener(OnUpgradeClicked);

        var btnLbl = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        btnLbl.transform.SetParent(btnGo.transform, false);
        var btnLblRt = btnLbl.GetComponent<RectTransform>();
        btnLblRt.anchorMin = Vector2.zero;
        btnLblRt.anchorMax = Vector2.one;
        btnLblRt.offsetMin = Vector2.zero;
        btnLblRt.offsetMax = Vector2.zero;
        var btnTmp = btnLbl.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Upgrade";
        btnTmp.fontSize = 18f;
        btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = new Color(0.1f, 0.1f, 0.1f);
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.raycastTarget = false;

        // ?? Max view ?????????????????????????????????????????????????????
        _maxView = new GameObject("MaxView", typeof(RectTransform));
        _maxView.transform.SetParent(transform, false);
        var mvRt = _maxView.GetComponent<RectTransform>();
        mvRt.anchorMin = Vector2.zero;
        mvRt.anchorMax = Vector2.one;
        mvRt.offsetMin = new Vector2(0f, 0f);
        mvRt.offsetMax = new Vector2(0f, -54f);

        MakeLabel(_maxView.transform, "MAX LEVEL",
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -52f), new Vector2(0f, -22f),
            22f, new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center);

        var maxContGo = new GameObject("MaxGridContainer", typeof(RectTransform));
        maxContGo.transform.SetParent(_maxView.transform, false);
        _maxGrid = maxContGo.GetComponent<RectTransform>();
        _maxGrid.anchorMin = new Vector2(0.5f, 0.5f);
        _maxGrid.anchorMax = new Vector2(0.5f, 0.5f);
        _maxGrid.pivot = new Vector2(0.5f, 0.5f);
        _maxGrid.anchoredPosition = new Vector2(0f, 10f);
        _maxGrid.sizeDelta = Vector2.zero;

        _built = true;
    }

    // ??? Refresh ????????????????????????????????????????????????????????????

    public void Refresh()
    {
        if (!_built) return;
        if (config == null || config.levels == null || config.levels.Length == 0) return;
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        int lvl = mgr.BagGridLevel;
        int maxLvl = config.levels.Length - 1;
        bool isMax = lvl >= maxLvl;

        if (_levelLabel != null) _levelLabel.text = $"Lv. {lvl}";

        _normalView?.SetActive(!isMax);
        _maxView?.SetActive(isMax);
        _upgradeButton?.gameObject.SetActive(!isMax);
        if (_reqContainer != null) _reqContainer.gameObject.SetActive(!isMax);
        if (_slotsBadge != null) _slotsBadge.gameObject.SetActive(!isMax);

        if (!isMax)
        {
            var curr = config.levels[lvl];
            var next = config.levels[lvl + 1];
            BuildMiniGrid(_currGrid, curr.cols, curr.rows, false, 0, 0);
            BuildMiniGrid(_nextGrid, next.cols, next.rows, true, curr.cols, curr.rows);
            if (_slotsBadge != null)
                _slotsBadge.text = $"+{next.cols * next.rows - curr.cols * curr.rows} slots";
            BuildRequirements(next.cost);
            if (_upgradeButton != null)
                _upgradeButton.interactable = MaterialStorage.Instance.HasEnoughAll(next.cost);
        }
        else
        {
            BuildMiniGrid(_maxGrid, config.levels[lvl].cols, config.levels[lvl].rows, true, 0, 0);
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

    // ??? Helpers ????????????????????????????????????????????????????????????

    private void BuildMiniGrid(RectTransform container, int cols, int rows, bool isNext, int prevCols, int prevRows)
    {
        if (container == null) return;
        foreach (Transform child in container) Destroy(child.gameObject);
        float w = cols * CS + Mathf.Max(0, cols - 1) * SP;
        float h = rows * CS + Mathf.Max(0, rows - 1) * SP;
        container.sizeDelta = new Vector2(w, h);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                bool isNew = isNext && (r >= prevRows || c >= prevCols);
                var go = new GameObject($"c{c}r{r}", typeof(RectTransform), typeof(Image));
                var crt = go.GetComponent<RectTransform>();
                crt.SetParent(container, false);
                crt.anchorMin = new Vector2(0f, 1f);
                crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 1f);
                crt.sizeDelta = new Vector2(CS, CS);
                crt.anchoredPosition = new Vector2(c * (CS + SP), -r * (CS + SP));
                var img = go.GetComponent<Image>();
                img.color = isNew ? ColNew : (isNext ? ColNext : ColCurr);
                img.raycastTarget = false;
            }
    }

    private void BuildRequirements(MaterialRequirement[] reqs)
    {
        if (_reqContainer == null) return;
        foreach (Transform child in _reqContainer) Destroy(child.gameObject);
        if (reqs == null || reqs.Length == 0) return;
        foreach (var req in reqs)
        {
            if (req.material == null) continue;
            int have = MaterialStorage.Instance.GetAll().TryGetValue(req.material, out var v) ? v : 0;
            Color col = have >= req.count ? ColOk : ColBad;

            var card = new GameObject("ReqCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(_reqContainer, false);
            card.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.18f);
            var fitter = card.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.padding = new RectOffset(8, 8, 4, 4);
            vlg.spacing = 2f;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGo.transform.SetParent(card.transform, false);
            var nameTmp = nameGo.GetComponent<TextMeshProUGUI>();
            nameTmp.text = req.material.moduleName; nameTmp.fontSize = 12f;
            nameTmp.fontStyle = FontStyles.Bold; nameTmp.color = col;
            nameTmp.alignment = TextAlignmentOptions.Center; nameTmp.raycastTarget = false;

            var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGo.transform.SetParent(card.transform, false);
            var countTmp = countGo.GetComponent<TextMeshProUGUI>();
            countTmp.text = $"{have}/{req.count}"; countTmp.fontSize = 12f;
            countTmp.color = col; countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.raycastTarget = false;
        }
    }

    private static void MakeLabel(Transform parent, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax,
        float fontSize, Color color, TextAlignmentOptions align)
    {
        var go = new GameObject(text + "_Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = align; tmp.raycastTarget = false;
    }
}