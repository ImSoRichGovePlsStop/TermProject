using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class HubStorageUI : MonoBehaviour
{
    [SerializeField] private BagGridUpgradeConfig bagGridConfig;
    [Header("Cheat")]
    [SerializeField] private int cheatAmount = 5;

    private const float ItemGap = 16f;
    private const int Columns = 4;
    private const float GridPad = 36f;

    public bool IsOpen { get; private set; }
    private bool _isShowingUpgrade;
    private bool _pendingScrollReset;
    private int _lastSwitchFrame = -1;

    private GameObject _panel;
    private GameObject _contentRoot;
    private ScrollRect _scroll;
    private Transform _content;
    private BagGridUpgradeUI _upgradePanel;
    private TextMeshProUGUI _switchLabel;

    private void Awake()
    {
        BuildPanel();
    }

    // Build

    private void BuildPanel()
    {
        // Root panel - 40% right side, full height
        _panel = new GameObject("StoragePanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(transform, false);
        _panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f, 0.97f);
        var panelRt = _panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.6f, 0f);
        panelRt.anchorMax = new Vector2(1f, 1f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;

        // Title - inside panel, top center
        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(_panel.transform, false);
        var titleRt = titleGo.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.offsetMin = new Vector2(0f, -56f);
        titleRt.offsetMax = new Vector2(0f, 0f);
        var titleTmp = titleGo.GetComponent<TextMeshProUGUI>();
        titleTmp.text = "Storage";
        titleTmp.fontSize = 30f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = Color.white;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.raycastTarget = false;

        // Switch button - top right, subtle
        var switchGo = new GameObject("SwitchButton", typeof(RectTransform), typeof(Image), typeof(Button));
        switchGo.transform.SetParent(_panel.transform, false);
        var switchRt = switchGo.GetComponent<RectTransform>();
        switchRt.anchorMin = new Vector2(1f, 1f);
        switchRt.anchorMax = new Vector2(1f, 1f);
        switchRt.pivot = new Vector2(1f, 1f);
        switchRt.sizeDelta = new Vector2(148f, 34f);
        switchRt.anchoredPosition = new Vector2(-10f, -11f);
        switchGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.08f);
        var switchBtn = switchGo.GetComponent<Button>();
        var sc = switchBtn.colors;
        sc.normalColor = new Color(1f, 1f, 1f, 0.08f);
        sc.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
        sc.pressedColor = new Color(1f, 1f, 1f, 0.28f);
        switchBtn.colors = sc;
        switchBtn.onClick.AddListener(OnSwitchButtonClicked);

        var lblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(switchGo.transform, false);
        var lblRt = lblGo.GetComponent<RectTransform>();
        lblRt.anchorMin = Vector2.zero;
        lblRt.anchorMax = Vector2.one;
        lblRt.offsetMin = Vector2.zero;
        lblRt.offsetMax = Vector2.zero;
        _switchLabel = lblGo.GetComponent<TextMeshProUGUI>();
        _switchLabel.text = "Upgrade Grid";
        _switchLabel.fontSize = 15f;
        _switchLabel.color = new Color(0.8f, 0.8f, 0.8f);
        _switchLabel.alignment = TextAlignmentOptions.Center;
        _switchLabel.raycastTarget = false;

        // Cheat button — top left
        var cheatGo = new GameObject("CheatButton", typeof(RectTransform), typeof(Image), typeof(Button));
        cheatGo.transform.SetParent(_panel.transform, false);
        var cheatRt = cheatGo.GetComponent<RectTransform>();
        cheatRt.anchorMin = new Vector2(0f, 1f);
        cheatRt.anchorMax = new Vector2(0f, 1f);
        cheatRt.pivot = new Vector2(0f, 1f);
        cheatRt.sizeDelta = new Vector2(120f, 34f);
        cheatRt.anchoredPosition = new Vector2(10f, -11f);
        cheatGo.GetComponent<Image>().color = new Color(0.6f, 0.2f, 0.2f, 0.5f);
        var cheatBtn = cheatGo.GetComponent<Button>();
        var cc = cheatBtn.colors;
        cc.normalColor = new Color(0.6f, 0.2f, 0.2f, 0.5f);
        cc.highlightedColor = new Color(0.8f, 0.3f, 0.3f, 0.7f);
        cc.pressedColor = new Color(0.4f, 0.1f, 0.1f, 0.8f);
        cheatBtn.colors = cc;
        cheatBtn.onClick.AddListener(SpawnCheatMaterials);
        var cheatLblGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        cheatLblGo.transform.SetParent(cheatGo.transform, false);
        var cheatLblRt = cheatLblGo.GetComponent<RectTransform>();
        cheatLblRt.anchorMin = Vector2.zero;
        cheatLblRt.anchorMax = Vector2.one;
        cheatLblRt.offsetMin = Vector2.zero;
        cheatLblRt.offsetMax = Vector2.zero;
        var cheatTmp = cheatLblGo.GetComponent<TextMeshProUGUI>();
        cheatTmp.text = "[Cheat] Fill";
        cheatTmp.fontSize = 14f;
        cheatTmp.color = new Color(1f, 0.6f, 0.6f);
        cheatTmp.alignment = TextAlignmentOptions.Center;
        cheatTmp.raycastTarget = false;
        // Thin divider under title
        var divGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divGo.transform.SetParent(_panel.transform, false);
        var divRt = divGo.GetComponent<RectTransform>();
        divRt.anchorMin = new Vector2(0f, 1f);
        divRt.anchorMax = new Vector2(1f, 1f);
        divRt.pivot = new Vector2(0.5f, 1f);
        divRt.offsetMin = new Vector2(16f, -58f);
        divRt.offsetMax = new Vector2(-16f, -56f);
        divGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

        // Content root
        _contentRoot = new GameObject("StorageContentRoot", typeof(RectTransform));
        _contentRoot.transform.SetParent(_panel.transform, false);
        var crRt = _contentRoot.GetComponent<RectTransform>();
        crRt.anchorMin = new Vector2(0f, 0f);
        crRt.anchorMax = new Vector2(1f, 1f);
        crRt.offsetMin = new Vector2(0f, 0f);
        crRt.offsetMax = new Vector2(0f, -60f);
        BuildScrollArea();

        // Upgrade panel
        var upgradeGo = new GameObject("BagGridUpgradePanel", typeof(RectTransform));
        upgradeGo.transform.SetParent(_panel.transform, false);
        var upgradeRt = upgradeGo.GetComponent<RectTransform>();
        upgradeRt.anchorMin = new Vector2(0f, 0f);
        upgradeRt.anchorMax = new Vector2(1f, 1f);
        upgradeRt.offsetMin = new Vector2(0f, 0f);
        upgradeRt.offsetMax = new Vector2(0f, -60f);
        _upgradePanel = upgradeGo.AddComponent<BagGridUpgradeUI>();
        _upgradePanel.Init(bagGridConfig, OnSwitchButtonClicked);
        upgradeGo.SetActive(false);

        _panel.SetActive(false);
    }

    private void BuildScrollArea()
    {
        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        scrollGo.transform.SetParent(_contentRoot.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(8f, 8f);
        scrollRt.offsetMax = new Vector2(-8f, 0f);

        var vpGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        vpGo.transform.SetParent(scrollGo.transform, false);
        var vpRt = vpGo.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        vpGo.GetComponent<Image>().color = Color.white;
        vpGo.GetComponent<Mask>().showMaskGraphic = false;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(vpGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;

        var grid = contentGo.AddComponent<GridLayoutGroup>();
        grid.spacing = new Vector2(ItemGap, ItemGap);
        grid.padding = new RectOffset((int)GridPad, (int)GridPad, (int)GridPad, (int)GridPad);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = Columns;

        // Calculate cell size after layout - use a coroutine via ContentRoot
        // For now set approximate, will be corrected on enable
        float approxPanelW = Screen.width * 0.4f;
        float cellSize = (approxPanelW - GridPad * 2f - ItemGap * (Columns - 1)) / Columns;
        grid.cellSize = new Vector2(cellSize, cellSize);

        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _content = contentGo.transform;

        _scroll = scrollGo.GetComponent<ScrollRect>();
        _scroll.viewport = vpRt;
        _scroll.content = contentRt;
        _scroll.horizontal = false;
        _scroll.vertical = true;
        _scroll.scrollSensitivity = 30f;
        _scroll.movementType = ScrollRect.MovementType.Clamped;
    }

    // Lifecycle

    private void Update()
    {
        if (IsOpen)
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame && _isShowingUpgrade) OnSwitchButtonClicked();
                if (kb.rightArrowKey.wasPressedThisFrame && !_isShowingUpgrade) OnSwitchButtonClicked();
            }
        }

        if (!_pendingScrollReset) return;
        Canvas.ForceUpdateCanvases();
        if (_scroll != null) _scroll.verticalNormalizedPosition = 1f;
        _pendingScrollReset = false;
    }

    // Public API

    public void Open()
    {
        IsOpen = true;
        _panel?.SetActive(true);
        UpdateCellSize();
        ShowStorageView();
        _pendingScrollReset = true;
    }

    private void UpdateCellSize()
    {
        if (_content == null) return;
        var grid = _content.GetComponent<GridLayoutGroup>();
        if (grid == null) return;
        Canvas.ForceUpdateCanvases();
        var scrollRt = _scroll?.GetComponent<RectTransform>();
        if (scrollRt == null) return;
        float w = scrollRt.rect.width;
        float cell = (w - GridPad * 2f - ItemGap * (Columns - 1)) / Columns;
        grid.cellSize = new Vector2(cell, cell);
    }

    public void Close()
    {
        IsOpen = false;
        _panel?.SetActive(false);
        _isShowingUpgrade = false;
        ModuleTooltipUI.Instance?.Hide();
    }

    public void OnSwitchButtonClicked()
    {
        if (Time.frameCount == _lastSwitchFrame) return;
        _lastSwitchFrame = Time.frameCount;
        if (_isShowingUpgrade) ShowStorageView();
        else ShowUpgradeView();
    }

    // Private

    private void ShowStorageView()
    {
        _isShowingUpgrade = false;
        _contentRoot?.SetActive(true);
        _upgradePanel?.gameObject.SetActive(false);
        if (_switchLabel != null) _switchLabel.text = "Upgrade Grid";
        Populate();
        _pendingScrollReset = true;
    }

    private void ShowUpgradeView()
    {
        Debug.Log($"[Storage] ShowUpgradeView, panel={_upgradePanel != null}");
        _isShowingUpgrade = true;
        _contentRoot?.SetActive(false);
        _upgradePanel?.gameObject.SetActive(true);
        if (_switchLabel != null) _switchLabel.text = "<";
        _upgradePanel?.Refresh();
    }

    private void Populate()
    {
        if (_content == null) return;
        foreach (Transform child in _content) Destroy(child.gameObject);
        var existing = _contentRoot.transform.Find("EmptyLabel");
        if (existing != null) Destroy(existing.gameObject);

        var all = MaterialStorage.Instance.GetAll();
        if (all.Count == 0)
        {
            var emptyGo = new GameObject("EmptyLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            emptyGo.transform.SetParent(_contentRoot.transform, false);
            var rt = emptyGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 60f);
            rt.anchoredPosition = Vector2.zero;
            var tmp = emptyGo.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "No materials";
            tmp.fontSize = 18f;
            tmp.color = new Color(0.5f, 0.5f, 0.5f);
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return;
        }

        foreach (var kvp in all)
        {
            var go = new GameObject("StorageItem", typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(_content, false);
            var item = go.AddComponent<StorageItemUI>();
            item.Init(kvp.Key, kvp.Value);
        }
    }

    private void SpawnCheatMaterials()
    {
        var allMaterials = Resources.LoadAll<MaterialData>("Materials");
        if (allMaterials == null || allMaterials.Length == 0)
        {
            Debug.LogWarning("[HubStorageUI] No MaterialData found in Resources/Materials/");
            return;
        }
        foreach (var mat in allMaterials)
        {
            if (mat == null) continue;
            MaterialStorage.Instance.Add(mat, cheatAmount);
        }
        Populate();
    }
}