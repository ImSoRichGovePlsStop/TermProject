using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagGridUpgradeUI : MonoBehaviour
{
    private BagGridUpgradeConfig config;
    private System.Action _onBack;

    private const float CS = 45f;
    private const float SP = 3f;

    private static readonly Color ColCurr = new Color(0.55f, 0.85f, 0.55f);
    private static readonly Color ColNext = new Color(0.45f, 0.60f, 0.90f);
    private static readonly Color ColNew = Color.cyan;
    private static readonly Color ColOk = new Color(0.63f, 0.88f, 0.56f);
    private static readonly Color ColBad = new Color(0.88f, 0.35f, 0.35f);

    private int _lastUpgradeFrame = -1;
    private bool _built = false;

    private TextMeshProUGUI _levelArrowLabel;
    private TextMeshProUGUI _slotsLabel;
    private TextMeshProUGUI _levelLabel;
    private RectTransform _currGrid;
    private RectTransform _nextGrid;
    private Transform _reqIconRow;
    private Button _upgradeButton;
    private GameObject _normalView;
    private GameObject _maxView;
    private RectTransform _maxGrid;

    private void OnEnable() { if (_built) Refresh(); }

    public void Init(BagGridUpgradeConfig cfg, System.Action onBack)
    {
        if (_built) return;
        config = cfg;
        _onBack = onBack;
        Build();
    }

    private void Build()
    {
        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var rootVlg = gameObject.AddComponent<VerticalLayoutGroup>();
        rootVlg.childAlignment = TextAnchor.UpperCenter;
        rootVlg.spacing = 0f; rootVlg.padding = new RectOffset(0, 0, 0, 0);
        rootVlg.childControlWidth = true; rootVlg.childForceExpandWidth = true;
        rootVlg.childControlHeight = true; rootVlg.childForceExpandHeight = false;

        // ?? Header (60px) ????????????????????????????????????????????????
        var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGo.transform.SetParent(transform, false);
        headerGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);
        var headerLe = headerGo.AddComponent<LayoutElement>();
        headerLe.preferredHeight = 60f; headerLe.flexibleHeight = 0f;

        // Title center
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(headerGo.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Storage"; titleTmp.fontSize = 30f;
        titleTmp.fontStyle = FontStyles.Bold; titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.raycastTarget = false;

        // Subtitle "Upgrade Bag Grid"
        var subGo = new GameObject("SubTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        subGo.transform.SetParent(headerGo.transform, false);
        var subRt = subGo.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0f, 0f); subRt.anchorMax = new Vector2(1f, 0f);
        subRt.pivot = new Vector2(0.5f, 0f);
        subRt.offsetMin = new Vector2(0f, 2f); subRt.offsetMax = new Vector2(0f, 22f);
        var subTmp = subGo.GetComponent<TextMeshProUGUI>();
        subTmp.text = "Upgrade Bag Grid"; subTmp.fontSize = 16f;
        subTmp.fontStyle = FontStyles.Bold; subTmp.color = new Color(0.8f, 0.8f, 0.8f);
        subTmp.alignment = TextAlignmentOptions.Center; subTmp.raycastTarget = false;

        // Lv label top-left
        var lvlGo = new GameObject("LvLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlGo.transform.SetParent(headerGo.transform, false);
        var lvlRt = lvlGo.GetComponent<RectTransform>();
        lvlRt.anchorMin = new Vector2(0f, 1f); lvlRt.anchorMax = new Vector2(0.3f, 1f);
        lvlRt.pivot = new Vector2(0f, 1f);
        lvlRt.offsetMin = new Vector2(12f, -28f); lvlRt.offsetMax = Vector2.zero;
        _levelLabel = lvlGo.GetComponent<TextMeshProUGUI>();
        _levelLabel.fontSize = 14f; _levelLabel.color = new Color(0.7f, 0.7f, 0.7f);
        _levelLabel.alignment = TextAlignmentOptions.Left; _levelLabel.raycastTarget = false;

        // Back button top-right
        var backGo = new GameObject("BackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        backGo.transform.SetParent(headerGo.transform, false);
        var backRt = backGo.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(1f, 1f); backRt.anchorMax = new Vector2(1f, 1f);
        backRt.pivot = new Vector2(1f, 1f);
        backRt.sizeDelta = new Vector2(148f, 34f);
        backRt.anchoredPosition = new Vector2(-10f, -11f);
        backGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var backBtn = backGo.GetComponent<Button>();
        var backColors = backBtn.colors;
        backColors.normalColor = new Color(1f, 1f, 1f, 0.08f);
        backColors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
        backColors.pressedColor = new Color(1f, 1f, 1f, 0.28f);
        backBtn.colors = backColors;
        backBtn.onClick.AddListener(() => _onBack?.Invoke());
        var backLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        backLblGo.transform.SetParent(backGo.transform, false);
        var backLblRt = backLblGo.GetComponent<RectTransform>();
        backLblRt.anchorMin = Vector2.zero; backLblRt.anchorMax = Vector2.one;
        backLblRt.offsetMin = Vector2.zero; backLblRt.offsetMax = Vector2.zero;
        var backLblTmp = backLblGo.GetComponent<TextMeshProUGUI>();
        backLblTmp.text = "<"; backLblTmp.fontSize = 15f;
        backLblTmp.color = new Color(0.8f, 0.8f, 0.8f);
        backLblTmp.alignment = TextAlignmentOptions.Center; backLblTmp.raycastTarget = false;

        // Divider
        var divGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGo.transform.SetParent(headerGo.transform, false);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 0f); divRt.anchorMax = new Vector2(1f, 0f);
        divRt.pivot = new Vector2(0.5f, 0f);
        divRt.offsetMin = new Vector2(16f, 0f); divRt.offsetMax = new Vector2(-16f, 1f);
        divGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

        // ?? Normal view ??????????????????????????????????????????????????
        _normalView = new GameObject("NormalView", typeof(RectTransform));
        _normalView.transform.SetParent(transform, false);
        var nvLe = _normalView.AddComponent<LayoutElement>();
        nvLe.flexibleHeight = 1f;
        var nvVlg = _normalView.AddComponent<VerticalLayoutGroup>();
        nvVlg.childAlignment = TextAnchor.UpperCenter;
        nvVlg.spacing = 0f;
        nvVlg.childControlWidth = true; nvVlg.childForceExpandWidth = true;
        nvVlg.childControlHeight = true; nvVlg.childForceExpandHeight = false;

        // Upper
        var upperGo = new GameObject("Upper", typeof(RectTransform), typeof(Image));
        upperGo.transform.SetParent(_normalView.transform, false);
        upperGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        var upperLe = upperGo.AddComponent<LayoutElement>();
        upperLe.flexibleHeight = 1f;
        var upperVlg = upperGo.AddComponent<VerticalLayoutGroup>();
        upperVlg.childAlignment = TextAnchor.MiddleCenter;
        upperVlg.childControlWidth = false; upperVlg.childForceExpandWidth = false;
        upperVlg.childControlHeight = false; upperVlg.childForceExpandHeight = false;
        var currGo = new GameObject("CurrGrid", typeof(RectTransform));
        currGo.transform.SetParent(upperGo.transform, false);
        _currGrid = currGo.GetComponent<RectTransform>();
        _currGrid.sizeDelta = Vector2.zero;

        // Middle (60px)
        var midGo = new GameObject("Middle", typeof(RectTransform));
        midGo.transform.SetParent(_normalView.transform, false);
        var midLe = midGo.AddComponent<LayoutElement>();
        midLe.preferredHeight = 60f; midLe.flexibleHeight = 0f;
        var midHlg = midGo.AddComponent<HorizontalLayoutGroup>();
        midHlg.childAlignment = TextAnchor.MiddleCenter;
        midHlg.padding = new RectOffset(16, 16, 0, 0); midHlg.spacing = 0f;
        midHlg.childControlWidth = true; midHlg.childForceExpandWidth = true;
        midHlg.childControlHeight = true; midHlg.childForceExpandHeight = false;

        // Left: Lv label (flexible, right-aligned ? pushes toward center)
        var lvlLblGo = new GameObject("LevelLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlLblGo.transform.SetParent(midGo.transform, false);
        lvlLblGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _levelArrowLabel = lvlLblGo.GetComponent<TextMeshProUGUI>();
        _levelArrowLabel.fontSize = 14f; _levelArrowLabel.color = new Color(0.65f, 0.65f, 0.65f);
        _levelArrowLabel.alignment = TextAlignmentOptions.Right; _levelArrowLabel.raycastTarget = false;

        // Center: arrow (fixed width)
        var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGo.transform.SetParent(midGo.transform, false);
        var arrowLe = arrowGo.AddComponent<LayoutElement>();
        arrowLe.preferredWidth = 40f; arrowLe.flexibleWidth = 0f;
        var arrowTmp = arrowGo.GetComponent<TextMeshProUGUI>();
        arrowTmp.text = "v"; arrowTmp.fontSize = 22f;
        arrowTmp.color = new Color(0.55f, 0.55f, 0.55f);
        arrowTmp.alignment = TextAlignmentOptions.Center; arrowTmp.raycastTarget = false;

        // Right: slots label (flexible, left-aligned)
        var slotsGo = new GameObject("SlotsLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotsGo.transform.SetParent(midGo.transform, false);
        slotsGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _slotsLabel = slotsGo.GetComponent<TextMeshProUGUI>();
        _slotsLabel.fontSize = 14f; _slotsLabel.color = new Color(0.5f, 0.9f, 1f);
        _slotsLabel.alignment = TextAlignmentOptions.Left; _slotsLabel.raycastTarget = false;

        // Bottom
        var bottomGo = new GameObject("Bottom", typeof(RectTransform), typeof(Image));
        bottomGo.transform.SetParent(_normalView.transform, false);
        bottomGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        var bottomLe = bottomGo.AddComponent<LayoutElement>();
        bottomLe.flexibleHeight = 1f;
        var bottomVlg = bottomGo.AddComponent<VerticalLayoutGroup>();
        bottomVlg.childAlignment = TextAnchor.MiddleCenter;
        bottomVlg.childControlWidth = false; bottomVlg.childForceExpandWidth = false;
        bottomVlg.childControlHeight = false; bottomVlg.childForceExpandHeight = false;
        var nextGo = new GameObject("NextGrid", typeof(RectTransform));
        nextGo.transform.SetParent(bottomGo.transform, false);
        _nextGrid = nextGo.GetComponent<RectTransform>();
        _nextGrid.sizeDelta = Vector2.zero;

        // ?? Req section (90px) ???????????????????????????????????????????
        var reqGo = new GameObject("ReqSection", typeof(RectTransform), typeof(Image));
        reqGo.transform.SetParent(transform, false);
        reqGo.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f, 1f);
        var reqLe = reqGo.AddComponent<LayoutElement>();
        reqLe.preferredHeight = 90f; reqLe.flexibleHeight = 0f;
        var reqVlg = reqGo.AddComponent<VerticalLayoutGroup>();
        reqVlg.childAlignment = TextAnchor.UpperCenter;
        reqVlg.spacing = 4f; reqVlg.padding = new RectOffset(8, 8, 8, 6);
        reqVlg.childControlWidth = true; reqVlg.childForceExpandWidth = true;
        reqVlg.childControlHeight = false; reqVlg.childForceExpandHeight = false;

        var reqHeaderGo = new GameObject("ReqHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        reqHeaderGo.transform.SetParent(reqGo.transform, false);
        reqHeaderGo.AddComponent<LayoutElement>().preferredHeight = 22f;
        var reqHeaderTmp = reqHeaderGo.GetComponent<TextMeshProUGUI>();
        reqHeaderTmp.text = "REQUIRED MATERIALS"; reqHeaderTmp.fontSize = 14f;
        reqHeaderTmp.fontStyle = FontStyles.Bold; reqHeaderTmp.color = new Color(0.7f, 0.7f, 0.7f);
        reqHeaderTmp.alignment = TextAlignmentOptions.Center; reqHeaderTmp.raycastTarget = false;

        var iconRowGo = new GameObject("ReqIconRow", typeof(RectTransform));
        iconRowGo.transform.SetParent(reqGo.transform, false);
        iconRowGo.AddComponent<LayoutElement>().preferredHeight = 56f;
        _reqIconRow = iconRowGo.transform;
        var iconHlg = iconRowGo.AddComponent<HorizontalLayoutGroup>();
        iconHlg.childAlignment = TextAnchor.MiddleCenter; iconHlg.spacing = 16f;
        iconHlg.childControlWidth = false; iconHlg.childForceExpandWidth = false;
        iconHlg.childControlHeight = false; iconHlg.childForceExpandHeight = false;

        // ?? Upgrade button (56px) ????????????????????????????????????????
        var btnWrapGo = new GameObject("UpgradeButtonWrap", typeof(RectTransform), typeof(Image));
        btnWrapGo.transform.SetParent(transform, false);
        btnWrapGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);
        var btnWrapLe = btnWrapGo.AddComponent<LayoutElement>();
        btnWrapLe.preferredHeight = 56f; btnWrapLe.flexibleHeight = 0f;

        var btnGo = new GameObject("UpgradeButton", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(btnWrapGo.transform, false);
        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0.04f, 0.15f); btnRt.anchorMax = new Vector2(0.96f, 0.85f);
        btnRt.offsetMin = Vector2.zero; btnRt.offsetMax = Vector2.zero;
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
        btnLblRt.anchorMin = Vector2.zero; btnLblRt.anchorMax = Vector2.one;
        btnLblRt.offsetMin = Vector2.zero; btnLblRt.offsetMax = Vector2.zero;
        var btnTmp = btnLbl.GetComponent<TextMeshProUGUI>();
        btnTmp.text = "Upgrade"; btnTmp.fontSize = 20f; btnTmp.fontStyle = FontStyles.Bold;
        btnTmp.color = new Color(0.1f, 0.1f, 0.1f);
        btnTmp.alignment = TextAlignmentOptions.Center; btnTmp.raycastTarget = false;

        // ?? Max view ?????????????????????????????????????????????????????
        _maxView = new GameObject("MaxView", typeof(RectTransform));
        _maxView.transform.SetParent(transform, false);
        var mvRt = _maxView.GetComponent<RectTransform>();
        mvRt.anchorMin = Vector2.zero; mvRt.anchorMax = Vector2.one;
        mvRt.offsetMin = Vector2.zero; mvRt.offsetMax = Vector2.zero;
        var maxLblGo = new GameObject("MaxLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        maxLblGo.transform.SetParent(_maxView.transform, false);
        var maxLblRt = maxLblGo.GetComponent<RectTransform>();
        maxLblRt.anchorMin = new Vector2(0f, 0.55f); maxLblRt.anchorMax = new Vector2(1f, 0.55f);
        maxLblRt.pivot = new Vector2(0.5f, 0.5f);
        maxLblRt.sizeDelta = new Vector2(0f, 30f); maxLblRt.anchoredPosition = Vector2.zero;
        var maxLblTmp = maxLblGo.GetComponent<TextMeshProUGUI>();
        maxLblTmp.text = "MAX LEVEL"; maxLblTmp.fontSize = 22f; maxLblTmp.fontStyle = FontStyles.Bold;
        maxLblTmp.color = new Color(1f, 0.85f, 0.2f);
        maxLblTmp.alignment = TextAlignmentOptions.Center; maxLblTmp.raycastTarget = false;
        var maxContGo = new GameObject("MaxGridContainer", typeof(RectTransform));
        maxContGo.transform.SetParent(_maxView.transform, false);
        _maxGrid = maxContGo.GetComponent<RectTransform>();
        _maxGrid.anchorMin = new Vector2(0.5f, 0.5f); _maxGrid.anchorMax = new Vector2(0.5f, 0.5f);
        _maxGrid.pivot = new Vector2(0.5f, 0.5f);
        _maxGrid.anchoredPosition = new Vector2(0f, -30f); _maxGrid.sizeDelta = Vector2.zero;

        _built = true;
    }

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

        if (!isMax)
        {
            var curr = config.levels[lvl];
            var next = config.levels[lvl + 1];
            if (_levelArrowLabel != null) _levelArrowLabel.text = $"Lv.{lvl} ? Lv.{lvl + 1}";
            if (_slotsLabel != null) _slotsLabel.text = $"+{next.cols * next.rows - curr.cols * curr.rows} slots";
            BuildMiniGrid(_currGrid, curr.cols, curr.rows, false, 0, 0);
            BuildMiniGrid(_nextGrid, next.cols, next.rows, true, curr.cols, curr.rows);
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
                crt.anchorMin = new Vector2(0f, 1f); crt.anchorMax = new Vector2(0f, 1f);
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
        if (_reqIconRow == null) return;
        foreach (Transform child in _reqIconRow) Destroy(child.gameObject);
        if (reqs == null || reqs.Length == 0) return;

        const float CardSize = 48f;

        foreach (var req in reqs)
        {
            if (req.material == null) continue;
            int have = MaterialStorage.Instance.GetAll().TryGetValue(req.material, out var v) ? v : 0;
            Color col = have >= req.count ? ColOk : ColBad;
            Color rarityCol = SpriteOutlineUtility.RarityColor(req.material.rarity);

            var card = new GameObject("ReqCard", typeof(RectTransform));
            card.transform.SetParent(_reqIconRow, false);
            var cardCsf = card.AddComponent<ContentSizeFitter>();
            cardCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cardVlg = card.AddComponent<VerticalLayoutGroup>();
            cardVlg.childAlignment = TextAnchor.UpperCenter;
            cardVlg.spacing = 3f; cardVlg.padding = new RectOffset(2, 2, 2, 2);
            cardVlg.childControlWidth = false; cardVlg.childForceExpandWidth = false;
            cardVlg.childControlHeight = false; cardVlg.childForceExpandHeight = false;

            // Icon card (same as StorageItemUI)
            var itemGo = new GameObject("Item", typeof(RectTransform));
            itemGo.transform.SetParent(card.transform, false);
            var itemLe = itemGo.AddComponent<LayoutElement>();
            itemLe.preferredWidth = CardSize; itemLe.preferredHeight = CardSize;

            // Grey bg
            var bgGo = CreateGO("Background", itemGo.transform);
            SetStretch(bgGo); bgGo.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.20f, 1f);

            // Rarity outline
            var outlineGo = CreateGO("Outline", itemGo.transform);
            var outlineRt = outlineGo.GetComponent<RectTransform>();
            outlineRt.anchorMin = Vector2.zero; outlineRt.anchorMax = Vector2.one;
            outlineRt.offsetMin = new Vector2(2f, 2f); outlineRt.offsetMax = new Vector2(-2f, -2f);
            outlineGo.AddComponent<Image>().color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.7f);

            // Inner
            var innerGo = CreateGO("Inner", itemGo.transform);
            var innerRt = innerGo.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero; innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(4f, 4f); innerRt.offsetMax = new Vector2(-4f, -4f);
            innerGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);

            // Icon with aspect ratio + size based on bound
            if (req.material.icon != null)
            {
                var bound = req.material.GetBoundingSize();
                int maxSide = Mathf.Max(bound.x, bound.y);
                float pct = maxSide >= 3 ? 1.0f : maxSide == 2 ? 0.8f : 0.6f;
                float half = (1f - pct) / 2f;

                var wrapGo = CreateGO("IconWrap", itemGo.transform);
                var wrapRt = wrapGo.GetComponent<RectTransform>();
                wrapRt.anchorMin = new Vector2(half, half);
                wrapRt.anchorMax = new Vector2(1f - half, 1f - half);
                wrapRt.offsetMin = new Vector2(2f, 2f); wrapRt.offsetMax = new Vector2(-2f, -2f);

                var iconGo = CreateGO("Icon", wrapGo.transform);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = Vector2.zero; iconRt.offsetMax = Vector2.zero;
                var raw = iconGo.AddComponent<RawImage>();
                raw.texture = req.material.icon.texture;
                raw.color = Color.white; raw.raycastTarget = false;
                var arf = iconGo.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = (float)req.material.icon.texture.width / req.material.icon.texture.height;
            }

            // have/need count
            var availGo = new GameObject("Avail", typeof(RectTransform), typeof(TextMeshProUGUI));
            availGo.transform.SetParent(card.transform, false);
            availGo.AddComponent<LayoutElement>().preferredHeight = 14f;
            var availTmp = availGo.GetComponent<TextMeshProUGUI>();
            availTmp.text = $"{have}/{req.count}"; availTmp.fontSize = 12f;
            availTmp.color = col;
            availTmp.alignment = TextAlignmentOptions.Center; availTmp.raycastTarget = false;
        }
    }

    private static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}