using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BagGridUpgradeUI : MonoBehaviour
{
    private BagGridUpgradeConfig config;
    private System.Action _onBack;

    private const float CS = 45f;
    private const float SP = 3f;

    private static readonly Color ColCurr = new Color(0.30f, 0.55f, 0.35f);
    private static readonly Color ColNext = new Color(0.25f, 0.35f, 0.55f);
    private static readonly Color ColNew = new Color(0.20f, 0.75f, 0.85f);
    private static readonly Color ColOk = new Color(0.63f, 0.88f, 0.56f);
    private static readonly Color ColBad = new Color(0.88f, 0.35f, 0.35f);

    private int _lastUpgradeFrame = -1;
    private bool _built = false;
    private readonly System.Collections.Generic.List<Image> _newCells = new();

    private TextMeshProUGUI _levelArrowLabel;
    private TextMeshProUGUI _slotsLabel;
    private TextMeshProUGUI _levelLabel;
    private RectTransform _currGrid;
    private RectTransform _nextGrid;
    private RequiredMaterialsUI _reqMaterials;
    private RectTransform _reqSection;
    private Button _upgradeButton;
    private GameObject _normalView;
    private GameObject _maxView;
    private RectTransform _maxGrid;
    private GameObject _maxLabel;

    private void OnEnable() { if (_built) Refresh(); }
    private void OnDisable() { StopAllCoroutines(); }

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

        // Header (60px)
        var headerGo = new GameObject("Header", typeof(RectTransform), typeof(Image));
        headerGo.transform.SetParent(transform, false);
        headerGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);
        headerGo.AddComponent<LayoutElement>().preferredHeight = 60f;

        // Title: "Upgrade Bag Grid" only, centered
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(headerGo.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = Vector2.zero; titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Upgrade Bag Grid"; titleTmp.fontSize = 30f;
        titleTmp.fontStyle = FontStyles.Bold; titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center; titleTmp.raycastTarget = false;

        // Lv label — middle-left of header
        var lvlGo = new GameObject("LvLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlGo.transform.SetParent(headerGo.transform, false);
        var lvlRt = lvlGo.GetComponent<RectTransform>();
        lvlRt.anchorMin = new Vector2(0f, 0f); lvlRt.anchorMax = new Vector2(0.3f, 1f);
        lvlRt.pivot = new Vector2(0f, 0.5f);
        lvlRt.offsetMin = new Vector2(12f, 0f); lvlRt.offsetMax = Vector2.zero;
        _levelLabel = lvlGo.GetComponent<TextMeshProUGUI>();
        _levelLabel.fontSize = 18f; _levelLabel.color = new Color(0.7f, 0.7f, 0.7f);
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

        // Normal view (flexible)
        _normalView = new GameObject("NormalView", typeof(RectTransform));
        _normalView.transform.SetParent(transform, false);
        _normalView.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var nvVlg = _normalView.AddComponent<VerticalLayoutGroup>();
        nvVlg.childAlignment = TextAnchor.UpperCenter; nvVlg.spacing = 0f;
        nvVlg.childControlWidth = true; nvVlg.childForceExpandWidth = true;
        nvVlg.childControlHeight = true; nvVlg.childForceExpandHeight = false;

        // Upper
        var upperGo = new GameObject("Upper", typeof(RectTransform), typeof(Image));
        upperGo.transform.SetParent(_normalView.transform, false);
        upperGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        upperGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var upperVlg = upperGo.AddComponent<VerticalLayoutGroup>();
        upperVlg.childAlignment = TextAnchor.MiddleCenter;
        upperVlg.childControlWidth = false; upperVlg.childForceExpandWidth = false;
        upperVlg.childControlHeight = false; upperVlg.childForceExpandHeight = false;
        var currGo = new GameObject("CurrGrid", typeof(RectTransform));
        currGo.transform.SetParent(upperGo.transform, false);
        _currGrid = currGo.GetComponent<RectTransform>();
        _currGrid.sizeDelta = Vector2.zero;

        // Middle (60px)
        var midGo = new GameObject("Middle", typeof(RectTransform), typeof(Image));
        midGo.transform.SetParent(_normalView.transform, false);
        midGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        var midLe = midGo.AddComponent<LayoutElement>();
        midLe.preferredHeight = 60f; midLe.flexibleHeight = 0f;
        var midHlg = midGo.AddComponent<HorizontalLayoutGroup>();
        midHlg.childAlignment = TextAnchor.MiddleCenter;
        midHlg.padding = new RectOffset(16, 16, 0, 0); midHlg.spacing = 0f;
        midHlg.childControlWidth = true; midHlg.childForceExpandWidth = true;
        midHlg.childControlHeight = true; midHlg.childForceExpandHeight = false;

        var lvlLblGo = new GameObject("LevelLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        lvlLblGo.transform.SetParent(midGo.transform, false);
        lvlLblGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _levelArrowLabel = lvlLblGo.GetComponent<TextMeshProUGUI>();
        _levelArrowLabel.fontSize = 18f; _levelArrowLabel.color = new Color(0.65f, 0.65f, 0.65f);
        _levelArrowLabel.alignment = TextAlignmentOptions.Right; _levelArrowLabel.raycastTarget = false;

        var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGo.transform.SetParent(midGo.transform, false);
        var arrowLe = arrowGo.AddComponent<LayoutElement>();
        arrowLe.preferredWidth = 40f; arrowLe.flexibleWidth = 0f;
        var arrowTmp = arrowGo.GetComponent<TextMeshProUGUI>();
        arrowTmp.text = "\u2193"; arrowTmp.fontSize = 60f;
        arrowTmp.color = new Color(0.55f, 0.55f, 0.55f);
        arrowTmp.alignment = TextAlignmentOptions.Center; arrowTmp.raycastTarget = false;

        var slotsGo = new GameObject("SlotsLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        slotsGo.transform.SetParent(midGo.transform, false);
        slotsGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        _slotsLabel = slotsGo.GetComponent<TextMeshProUGUI>();
        _slotsLabel.fontSize = 18f; _slotsLabel.color = new Color(0.5f, 0.9f, 1f);
        _slotsLabel.alignment = TextAlignmentOptions.Left; _slotsLabel.raycastTarget = false;

        // Bottom
        var bottomGo = new GameObject("Bottom", typeof(RectTransform), typeof(Image));
        bottomGo.transform.SetParent(_normalView.transform, false);
        bottomGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);
        bottomGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var bottomVlg = bottomGo.AddComponent<VerticalLayoutGroup>();
        bottomVlg.childAlignment = TextAnchor.MiddleCenter;
        bottomVlg.childControlWidth = false; bottomVlg.childForceExpandWidth = false;
        bottomVlg.childControlHeight = false; bottomVlg.childForceExpandHeight = false;
        var nextGo = new GameObject("NextGrid", typeof(RectTransform));
        nextGo.transform.SetParent(bottomGo.transform, false);
        _nextGrid = nextGo.GetComponent<RectTransform>();
        _nextGrid.sizeDelta = Vector2.zero;

        // Req section (200px)
        var reqGo = new GameObject("ReqSection", typeof(RectTransform), typeof(Image));
        reqGo.transform.SetParent(transform, false);
        reqGo.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.12f, 1f);
        var reqLe = reqGo.AddComponent<LayoutElement>();
        reqLe.preferredHeight = 200f; reqLe.flexibleHeight = 0f;
        _reqSection = reqGo.GetComponent<RectTransform>();

        var reqMatGo = new GameObject("RequiredMaterials", typeof(RectTransform));
        reqMatGo.layer = LayerMask.NameToLayer("UI");
        reqMatGo.transform.SetParent(reqGo.transform, false);
        var reqMatRt = reqMatGo.GetComponent<RectTransform>();
        reqMatRt.anchorMin = Vector2.zero; reqMatRt.anchorMax = Vector2.one;
        reqMatRt.offsetMin = new Vector2(8f, 6f); reqMatRt.offsetMax = new Vector2(-8f, -6f);
        _reqMaterials = reqMatGo.AddComponent<RequiredMaterialsUI>();

        // Upgrade button wrap (56px)
        var btnWrapGo = new GameObject("UpgradeButtonWrap", typeof(RectTransform), typeof(Image));
        btnWrapGo.transform.SetParent(transform, false);
        btnWrapGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 1f);
        btnWrapGo.AddComponent<LayoutElement>().preferredHeight = 56f;

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

        // Max view
        _maxView = new GameObject("MaxView", typeof(RectTransform));
        _maxView.transform.SetParent(transform, false);
        _maxView.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var maxVlg = _maxView.AddComponent<VerticalLayoutGroup>();
        maxVlg.childAlignment = TextAnchor.MiddleCenter;
        maxVlg.spacing = 0f;
        maxVlg.childControlWidth = false; maxVlg.childForceExpandWidth = false;
        maxVlg.childControlHeight = false; maxVlg.childForceExpandHeight = false;

        var maxContGo = new GameObject("MaxGridContainer", typeof(RectTransform));
        maxContGo.transform.SetParent(_maxView.transform, false);
        _maxGrid = maxContGo.GetComponent<RectTransform>();
        _maxGrid.sizeDelta = Vector2.zero;

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
        if (_reqSection != null) _reqSection.gameObject.SetActive(!isMax);

        if (isMax) StopAllCoroutines();

        if (!isMax)
        {
            var curr = config.levels[lvl];
            var next = config.levels[lvl + 1];
            if (_levelArrowLabel != null) _levelArrowLabel.text = $"Lv.{lvl} \u2192 Lv.{lvl + 1}";
            if (_slotsLabel != null) _slotsLabel.text = $"+{next.cols * next.rows - curr.cols * curr.rows} slots";
            BuildMiniGrid(_currGrid, curr.cols, curr.rows, false, 0, 0);
            _newCells.Clear();
            BuildMiniGrid(_nextGrid, next.cols, next.rows, true, curr.cols, curr.rows);
            StopAllCoroutines();
            if (_newCells.Count > 0) StartCoroutine(BlinkNewCells());
            LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
            _reqMaterials?.Show(next.cost);
            if (_upgradeButton != null)
                _upgradeButton.interactable = MaterialStorage.Instance.HasEnoughAll(next.cost);
        }
        else
        {
            BuildMiniGrid(_maxGrid, config.levels[lvl].cols, config.levels[lvl].rows, false, 0, 0, ColNext);
            // Recreate label on top of cells
            if (_maxLabel != null) Destroy(_maxLabel);
            var maxLbl = new GameObject("MaxLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            maxLbl.layer = LayerMask.NameToLayer("UI");
            maxLbl.transform.SetParent(_maxGrid, false);
            var maxLblRt = maxLbl.GetComponent<RectTransform>();
            maxLblRt.anchorMin = Vector2.zero; maxLblRt.anchorMax = Vector2.one;
            maxLblRt.offsetMin = Vector2.zero; maxLblRt.offsetMax = Vector2.zero;
            var maxLblTmp = maxLbl.GetComponent<TMPro.TextMeshProUGUI>();
            maxLblTmp.text = "MAX LEVEL"; maxLblTmp.fontSize = 32f; maxLblTmp.fontStyle = FontStyles.Bold;
            maxLblTmp.color = new Color(1f, 0.85f, 0.2f);
            maxLblTmp.alignment = TMPro.TextAlignmentOptions.Center; maxLblTmp.raycastTarget = false;
            _maxLabel = maxLbl;
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

    private System.Collections.IEnumerator BlinkNewCells()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 2f; // speed
            float alpha = (Mathf.Sin(t) + 1f) / 2f; // 0..1
            Color c = Color.Lerp(
                new Color(ColNew.r * 0.4f, ColNew.g * 0.4f, ColNew.b * 0.4f),
                ColNew, alpha);
            foreach (var img in _newCells) if (img != null) img.color = c;
            yield return null;
        }
    }

    private void BuildMiniGrid(RectTransform container, int cols, int rows, bool isNext, int prevCols, int prevRows, Color? overrideColor = null)
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
                img.color = overrideColor ?? (isNew ? ColNew : (isNext ? ColNext : ColCurr));
                if (isNew) _newCells.Add(img);
                img.raycastTarget = false;
            }
    }

}