using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ModuleTooltipUI : MonoBehaviour
{
    public static ModuleTooltipUI Instance { get; private set; }

    private RectTransform rt;
    private Canvas rootCanvas;
    private VerticalLayoutGroup mainLayout;
    private LayoutElement mainPanelLayout;

    private TextMeshProUGUI nameText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI rarityText;
    private TextMeshProUGUI costText;

    private GameObject statContainer;
    private readonly List<(TextMeshProUGUI desc, TextMeshProUGUI value)> statRows
        = new List<(TextMeshProUGUI, TextMeshProUGUI)>();

    private GameObject passiveContainer;
    private readonly List<(TextMeshProUGUI value, TextMeshProUGUI label, TextMeshProUGUI sublabel)> passiveBoxes
        = new List<(TextMeshProUGUI, TextMeshProUGUI, TextMeshProUGUI)>();
    private List<(string normal, string expanded)> passiveBoxTexts
        = new List<(string, string)>();
    private GameObject statDivider;
    private GameObject footerContainer;

    private GameObject detailPanel;
    private TextMeshProUGUI seeDetailsLabel;
    private GameObject buffGridContainer;

    private ModuleInstance currentInst;
    private bool isDetailExpanded;
    private bool isTabExpanded;
    private GridUI currentBuffGridUI;
    private PlayerStats playerStats;

    // Buff expand state
    private string statDescNormal;
    private string statDescExpanded;
    private TextMeshProUGUI statDescRef;

    // Level buff expand state
    private string levelTextNormal;
    private string levelTextExpanded;

    // Rarity buff expand state
    private string rarityTextNormal;
    private string rarityTextExpanded;

    private static Color RarityColor(Rarity r) => SpriteOutlineUtility.RarityColor(r);

    private void Awake()
    {
        Instance = this;
        rt = GetComponent<RectTransform>();
        playerStats = FindFirstObjectByType<PlayerStats>();
        rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        BuildMainPanel();
        BuildDetailPanel();

        gameObject.SetActive(false);
    }

    private void BuildMainPanel()
    {
        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.07f, 1f);
        bg.raycastTarget = false;

        var le = gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = 600f;
        le.flexibleWidth = 0f;
        mainPanelLayout = le;

        var fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        mainLayout = gameObject.AddComponent<VerticalLayoutGroup>();
        mainLayout.padding = new RectOffset(0, 0, 0, 0);
        mainLayout.spacing = 0;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = true;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;

        var headerSection = CreateSection("HeaderSection", new Color(0.12f, 0.12f, 0.12f, 1f), 20, 20, 16, 14);

        var headerRow = CreateHorizontalRow("HeaderRow", 6f, headerSection);
        nameText = CreateTMP(headerRow, 32f, FontStyles.Bold, Color.white, TextAlignmentOptions.Left, flexibleWidth: 1f);
        levelText = CreateTMP(headerRow, 20f, FontStyles.Normal, new Color(0.75f, 0.75f, 0.75f), TextAlignmentOptions.Right, preferredWidth: 80f);

        var subRow = CreateHorizontalRow("SubHeaderRow", 6f, headerSection);
        rarityText = CreateTMP(subRow, 22f, FontStyles.Normal, new Color(0.6f, 0.6f, 0.6f), TextAlignmentOptions.Left, flexibleWidth: 1f);
        costText = CreateTMP(subRow, 22f, FontStyles.Normal, Color.yellow, TextAlignmentOptions.Right, preferredWidth: 90f);

        statDivider = CreateDivider(gameObject);

        var statSection = CreateSection("StatSection", new Color(0.05f, 0.05f, 0.05f, 1f), 20, 20, 16, 14);
        statContainer = statSection;
        statSection.GetComponent<VerticalLayoutGroup>().spacing = 12;

        // Passive section (hidden by default)
        passiveContainer = new GameObject("PassiveContainer", typeof(RectTransform));
        passiveContainer.transform.SetParent(transform, false);
        var passiveVlg = passiveContainer.AddComponent<VerticalLayoutGroup>();
        passiveVlg.padding = new RectOffset(0, 0, 0, 0);
        passiveVlg.spacing = 0;
        passiveVlg.childControlWidth = true;
        passiveVlg.childControlHeight = true;
        passiveVlg.childForceExpandWidth = true;
        passiveVlg.childForceExpandHeight = false;
        passiveContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        passiveContainer.SetActive(false);

        // Footer section (for obtain sources etc.)
        footerContainer = new GameObject("FooterContainer", typeof(RectTransform));
        footerContainer.transform.SetParent(transform, false);
        var footerImg = footerContainer.AddComponent<Image>();
        footerImg.color = new Color(0.09f, 0.09f, 0.09f, 1f);
        footerImg.raycastTarget = false;
        var footerVlg = footerContainer.AddComponent<VerticalLayoutGroup>();
        footerVlg.padding = new RectOffset(0, 0, 0, 0);
        footerVlg.spacing = 0;
        footerVlg.childControlWidth = true;
        footerVlg.childControlHeight = true;
        footerVlg.childForceExpandWidth = true;
        footerVlg.childForceExpandHeight = false;

        footerContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        footerContainer.SetActive(false);
    }

    private GameObject CreateSection(string name, Color bgColor, int padLeft, int padRight, int padTop, int padBot)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var img = go.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = false;

        var layout = go.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(padLeft, padRight, padTop, padBot);
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    private void BuildDetailPanel()
    {
        detailPanel = new GameObject("DetailPanel", typeof(RectTransform));
        detailPanel.transform.SetParent(transform.parent, false);

        var detailRt = detailPanel.GetComponent<RectTransform>();
        detailRt.pivot = new Vector2(0f, 1f);
        detailRt.anchorMin = new Vector2(0f, 1f);
        detailRt.anchorMax = new Vector2(0f, 1f);

        var bg = detailPanel.AddComponent<Image>();
        bg.color = new Color(0.07f, 0.07f, 0.07f, 1f);
        bg.raycastTarget = false;

        var fitter = detailPanel.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var layout = detailPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        seeDetailsLabel = CreateTMP(detailPanel, 18f, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f), TextAlignmentOptions.Center);
        seeDetailsLabel.text = "See Details\n[TAB]";
        seeDetailsLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        buffGridContainer = new GameObject("BuffRangeSection", typeof(RectTransform));
        buffGridContainer.transform.SetParent(detailPanel.transform, false);

        var sectionLayout = buffGridContainer.AddComponent<VerticalLayoutGroup>();
        sectionLayout.spacing = 8;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        buffGridContainer.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var titleGo = new GameObject("Title", typeof(RectTransform));
        titleGo.transform.SetParent(buffGridContainer.transform, false);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text = "Buff Range:";
        titleTmp.fontSize = 16f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color = new Color(0.6f, 0.6f, 0.6f);
        titleTmp.alignment = TextAlignmentOptions.Left;
        titleTmp.raycastTarget = false;

        var buffDescGo = new GameObject("BuffDesc", typeof(RectTransform));
        buffDescGo.transform.SetParent(buffGridContainer.transform, false);
        var buffDescTmp = buffDescGo.AddComponent<TextMeshProUGUI>();
        buffDescTmp.text = "";
        buffDescTmp.fontSize = 16f;
        buffDescTmp.fontStyle = FontStyles.Normal;
        buffDescTmp.color = new Color(0.75f, 0.75f, 0.75f);
        buffDescTmp.alignment = TextAlignmentOptions.Left;
        buffDescTmp.raycastTarget = false;

        var gridGo = new GameObject("Grid", typeof(RectTransform));
        gridGo.transform.SetParent(buffGridContainer.transform, false);
        var glg = gridGo.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(24f, 24f);
        glg.spacing = new Vector2(1f, 1f);
        glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        glg.childAlignment = TextAnchor.UpperCenter;
        gridGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        buffGridContainer.SetActive(false);
        detailPanel.SetActive(false);
    }

    private void Update()
    {
        if (currentInst == null) return;

        bool holdingTab = Keyboard.current != null && Keyboard.current[Key.Tab].isPressed;

        // Detail panel expand (buff module)
        if (detailPanel != null && detailPanel.activeSelf && holdingTab != isDetailExpanded)
        {
            isDetailExpanded = holdingTab;
            seeDetailsLabel.gameObject.SetActive(!isDetailExpanded);
            buffGridContainer.SetActive(isDetailExpanded);
        }

        // Expand all ^ indicators together on Tab
        if (holdingTab != isTabExpanded)
        {
            isTabExpanded = holdingTab;
            if (statDescRef != null && statDescNormal != null)
                statDescRef.text = isTabExpanded ? statDescExpanded : statDescNormal;
            if (levelTextNormal != null)
                levelText.text = isTabExpanded ? levelTextExpanded : levelTextNormal;
            if (rarityTextNormal != null)
                rarityText.text = isTabExpanded ? rarityTextExpanded : rarityTextNormal;
            for (int i = 0; i < passiveBoxes.Count && i < passiveBoxTexts.Count; i++)
            {
                var (normal, expanded) = passiveBoxTexts[i];
                passiveBoxes[i].value.text = isTabExpanded ? expanded : normal;
            }
        }
    }

    public void Show(ModuleInstance inst, GridUI weaponGridUI = null, GridUI bagGridUI = null, GridUI envGridUI = null, DiscardGridUI discardGridUIRef = null, GridUI inputGridUI = null)
    {
        currentInst = inst;
        isDetailExpanded = false;
        isTabExpanded = false;

        mainPanelLayout.preferredWidth = 600f;
        statDescNormal = null;
        statDescExpanded = null;
        statDescRef = null;
        levelTextNormal = null;
        levelTextExpanded = null;
        rarityTextNormal = null;
        rarityTextExpanded = null;

        var effect = inst.Data.moduleEffect;
        var state = inst.RuntimeState;

        nameText.text = inst.Data.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        bool hasLevelBuff = state.buffedLevel != 0 && state.buffedLevel != inst.Level;
        if (inst.Level > 0 || hasLevelBuff)
        {
            int displayBase = inst.Level;
            int displayBuffed = hasLevelBuff ? state.buffedLevel : inst.Level;
            levelTextNormal = hasLevelBuff ? $"Lv.{displayBuffed}<color=#88FF88>\u25B2</color>" : $"Lv.{displayBase}";
            levelTextExpanded = hasLevelBuff ? $"<color=#666666><s>Lv.{displayBase}</s></color> Lv.{displayBuffed}" : $"Lv.{displayBase}";
        }
        else
        {
            levelTextNormal = "";
            levelTextExpanded = "";
        }
        levelText.text = levelTextNormal;

        bool hasRarityBuff = effect != null
            && state.buffRarity != 0
            && state.buffRarity != inst.Rarity
            && System.Array.Exists(state.baseRarity, v => v > 0);
        if (hasRarityBuff)
        {
            string arrow = state.buffRarity > inst.Rarity
                ? "<color=#88FF88>\u25B2</color>"
                : "<color=#FF4444>\u25BC</color>";
            rarityTextNormal = $"{state.buffRarity} {arrow}";
            rarityTextExpanded = $"<color=#666666><s>{inst.Rarity}</s></color> {state.buffRarity}";
        }
        else
        {
            rarityTextNormal = inst.Rarity.ToString();
            rarityTextExpanded = inst.Rarity.ToString();
        }
        rarityText.text = rarityTextNormal;
        costText.text = $"{(int)inst.GetCostAtLevel()}   <sprite=0>";

        PopulateStats(inst);
        PopulatePassive(inst);
        ClearFooter();

        bool isBuff = inst.Data.isBuffAdjacent;
        detailPanel.SetActive(isBuff);
        if (isBuff)
        {
            seeDetailsLabel.gameObject.SetActive(true);
            buffGridContainer.SetActive(false);
            BuildBuffGrid(inst);

            var buffDescTmp = buffGridContainer.transform.Find("BuffDesc")?.GetComponent<TextMeshProUGUI>();
            if (buffDescTmp != null)
                buffDescTmp.text = !string.IsNullOrEmpty(inst.Data.buffDescription) ? inst.Data.buffDescription : "";
        }

        if (isBuff && inst.CurrentGrid != null)
        {
            GridUI grid = null;
            if (inst.CurrentGrid == weaponGridUI?.Data) grid = weaponGridUI;
            else if (inst.CurrentGrid == bagGridUI?.Data) grid = bagGridUI;
            else if (inst.CurrentGrid == envGridUI?.Data) grid = envGridUI;
            else if (inst.CurrentGrid == inputGridUI?.Data) grid = inputGridUI;
            else if (inst.CurrentGrid == discardGridUIRef?.DiscardGrid) grid = discardGridUIRef?.GridUI;
            currentBuffGridUI = grid;
            grid?.HighlightBuffCells(inst, inst.Data.moduleColor);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        StartCoroutine(PositionNextFrame(inst));
    }

    public void ShowForStorage(MaterialInstance inst, RectTransform anchor)
    {
        currentInst = null;
        isDetailExpanded = false;
        isTabExpanded = false;

        mainPanelLayout.preferredWidth = 400f;
        nameText.text = inst.MaterialData.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        levelText.text = "";
        rarityText.text = inst.Rarity.ToString();
        costText.text = "";

        ClearStatRows();
        ClearPassive();
        ClearFooter();

        bool hasDesc = !string.IsNullOrEmpty(inst.MaterialData.materialDescription);
        if (hasDesc) AddDescRow().text = inst.MaterialData.materialDescription;
        statContainer.SetActive(hasDesc);
        statDivider.SetActive(hasDesc);

        if (inst.MaterialData.obtainSources != null && inst.MaterialData.obtainSources.Length > 0)
        {
            footerContainer.SetActive(true);
            CreateDivider(footerContainer);
            var textWrapper = new GameObject("FooterTextPadding", typeof(RectTransform));
            textWrapper.transform.SetParent(footerContainer.transform, false);
            var vlg = textWrapper.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 10, 14);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            var sourceTmp = new GameObject("ObtainText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            sourceTmp.transform.SetParent(textWrapper.transform, false);
            sourceTmp.text = "Obtain from: " + string.Join(", ", inst.MaterialData.obtainSources);
            sourceTmp.fontSize = 22f;
            sourceTmp.color = new Color(0.55f, 0.55f, 0.55f);
            sourceTmp.alignment = TextAlignmentOptions.Left;
            sourceTmp.raycastTarget = false;
            sourceTmp.textWrappingMode = TextWrappingModes.Normal;
            textWrapper.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        detailPanel.SetActive(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        StartCoroutine(PositionAtRectNextFrame(anchor));
    }

    public void Show(MaterialInstance inst)
    {
        currentInst = null;
        isDetailExpanded = false;
        isTabExpanded = false;

        mainPanelLayout.preferredWidth = 400f;
        nameText.text = inst.MaterialData.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        levelText.text = $"{inst.StackCount}/{inst.MaxStack}";
        rarityText.text = inst.Rarity.ToString();
        costText.text = inst.Cost > 0 ? $"{inst.Cost * inst.StackCount}  <sprite=0>" : "";

        ClearStatRows();
        ClearPassive();
        ClearFooter();

        // Description
        bool hasDesc = !string.IsNullOrEmpty(inst.MaterialData.materialDescription);
        if (hasDesc)
            AddDescRow().text = inst.MaterialData.materialDescription;

        statContainer.SetActive(hasDesc);
        statDivider.SetActive(hasDesc);

        // Obtain sources footer
        if (inst.MaterialData.obtainSources != null && inst.MaterialData.obtainSources.Length > 0)
        {
            footerContainer.SetActive(true);
            CreateDivider(footerContainer);

            var textWrapper = new GameObject("FooterTextPadding", typeof(RectTransform));
            textWrapper.transform.SetParent(footerContainer.transform, false);

            var vlg = textWrapper.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 10, 14);
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            var sourceTmp = new GameObject("ObtainText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            sourceTmp.transform.SetParent(textWrapper.transform, false);

            sourceTmp.text = "Obtain from: " + string.Join(", ", inst.MaterialData.obtainSources);
            sourceTmp.fontSize = 22f;
            sourceTmp.color = new Color(0.55f, 0.55f, 0.55f);
            sourceTmp.alignment = TextAlignmentOptions.Left;
            sourceTmp.raycastTarget = false;
            sourceTmp.textWrappingMode = TextWrappingModes.Normal;

            textWrapper.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
        detailPanel.SetActive(false);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        StartCoroutine(PositionNextFrameMaterial(inst));
    }

    private IEnumerator PositionNextFrameMaterial(MaterialInstance inst)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (inst.UIElement != null)
            PositionTooltip(inst);
        transform.SetAsLastSibling();
    }
    public void ShowAtRect(ModuleInstance inst, RectTransform anchor)
    {
        Show(inst);
        StartCoroutine(PositionAtRectNextFrame(anchor));
    }

    public void ShowNextLevelAtRect(ModuleInstance inst, RectTransform anchor)
    {
        Show(inst);

        int nextLevel = inst.Level + 1;
        levelTextNormal = $"Lv.{nextLevel}<color=#88FF88>\u25B2</color>";
        levelTextExpanded = $"<color=#666666><s>Lv.{inst.Level}</s></color> Lv.{nextLevel}";
        levelText.text = levelTextNormal;

        var effect = inst.Data.moduleEffect;
        if (effect != null && statDescRef != null)
        {
            var emptyState = new ModuleRuntimeState();
            // Temporarily unequip if active so playerStats reflects base (without this module)
            bool wasActive = inst.RuntimeState.isActive;
            if (wasActive) effect.Unequip(playerStats, inst.Rarity, inst.Level, inst.RuntimeState);
            var (curLabel, curBefore, curAfter, format) = effect.GetStatPreview(inst.Rarity, inst.Level, emptyState, playerStats);
            var (nextLabel, nextBefore, nextAfter, _) = effect.GetStatPreview(inst.Rarity, nextLevel, emptyState, playerStats);
            // Re-equip to restore state
            if (wasActive) effect.Equip(playerStats, inst.Rarity, inst.Level, inst.RuntimeState);

            if (curLabel != null && nextLabel != null && curLabel != nextLabel)
            {
                int spaceIdx = curLabel.IndexOf(' ');
                string curNum = spaceIdx >= 0 ? curLabel.Substring(0, spaceIdx) : curLabel;
                string nextNum = spaceIdx >= 0 ? nextLabel.Substring(0, nextLabel.IndexOf(' ')) : nextLabel;
                string statName = spaceIdx >= 0 ? nextLabel.Substring(nextLabel.IndexOf(' ') + 1) : "";

                statDescNormal = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> {nextNum} <color=#88FF88>\u25B2</color> {statName}";
                statDescExpanded = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> <color=#666666><s>{curNum}</s></color> {nextNum} {statName}";
                statDescRef.text = statDescNormal;
            }

            if (statRows.Count > 0 && nextBefore >= 0f && nextAfter >= 0f)
            {
                bool isPercent = format.EndsWith("%");
                string fmt = format.Replace("%", "");
                string beforeStr = isPercent ? $"{nextBefore.ToString(fmt)}%" : nextBefore.ToString(fmt);
                string afterStr = isPercent ? $"{nextAfter.ToString(fmt)}%" : nextAfter.ToString(fmt);
                statRows[0].value.text = $"<color=#888888>{beforeStr} <voffset=0.05em>\u2192</voffset></color> {afterStr}";
            }
        }

        costText.text = $"{(int)new ModuleInstance(inst.Data, inst.Rarity, nextLevel).GetCostAtLevel()}  <sprite=0>";

        PopulatePassiveCompare(inst);
        StartCoroutine(PositionAtRectNextFrame(anchor));
    }

    private IEnumerator PositionAtRectNextFrame(RectTransform anchor)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        PositionTooltipAtRect(anchor);
        transform.SetAsLastSibling();
        if (detailPanel != null) detailPanel.transform.SetAsLastSibling();
    }

    private void PositionTooltipAtRect(RectTransform anchor)
    {
        if (rootCanvas == null) return;

        var canvasRt = rootCanvas.GetComponent<RectTransform>();
        float canvasH = canvasRt.rect.height;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
            RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[2]) * 0.5f),
            cam, out Vector2 center);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
            RectTransformUtility.WorldToScreenPoint(cam, corners[1]),
            cam, out Vector2 tl);

        float tooltipW = rt.rect.width;
        float tooltipH = rt.rect.height;
        float halfH = canvasH * 0.5f;
        float gap = 9f;
        float edgeGap = 20f;

        rt.pivot = new Vector2(0f, 1f);
        float xPos = tl.x - tooltipW - gap;
        float yPos = center.y + tooltipH * 0.5f;
        yPos = Mathf.Clamp(yPos, -halfH + tooltipH + edgeGap, halfH - edgeGap);
        rt.anchoredPosition = new Vector2(xPos, yPos);

        if (detailPanel != null && detailPanel.activeSelf)
        {
            var detailRt = detailPanel.GetComponent<RectTransform>();
            detailRt.anchorMin = rt.anchorMin;
            detailRt.anchorMax = rt.anchorMax;
            detailRt.pivot = new Vector2(1f, 1f);
            detailRt.anchoredPosition = new Vector2(xPos - gap, yPos);
        }
    }

    public void Hide()
    {
        currentInst = null;
        isDetailExpanded = false;
        isTabExpanded = false;
        gameObject.SetActive(false);
        detailPanel.SetActive(false);

        if (currentBuffGridUI != null)
        {
            currentBuffGridUI.ClearBuffHighlights();
            currentBuffGridUI = null;
        }
    }

    private void PopulateStats(ModuleInstance inst)
    {
        ClearStatRows();
        statDescNormal = null;
        statDescExpanded = null;
        statDescRef = null;

        var effect = inst.Data.moduleEffect;
        bool hasContent = false;

        if (effect != null)
        {
            // Description with bold keywords
            if (!string.IsNullOrEmpty(inst.Data.description))
            {
                string desc = inst.Data.description;
                if (effect.BoldKeywords != null)
                    foreach (var kw in effect.BoldKeywords)
                        desc = desc.Replace(kw, $"<b>{kw}</b>");
                AddDescRow().text = desc;
                hasContent = true;
            }

            var (leftLabel, before, after, format) = effect.GetStatPreview(inst.Rarity, inst.Level, inst.RuntimeState, playerStats);
            if (leftLabel != null)
            {
                hasContent = true;
                var row = AddStatRow();
                statDescRef = row.desc;

                // Build left side
                var (unbuffedStat, buffedStat) = effect.GetBaseModuleStat(inst.Rarity, inst.Level, inst.RuntimeState);
                bool hasAnyBuff = inst.RuntimeState.isActive && unbuffedStat >= 0f && buffedStat != unbuffedStat;
                if (hasAnyBuff)
                {
                    var (unbuffedLabel, _, _, _) = effect.GetStatPreview(inst.Rarity, inst.Level, new ModuleRuntimeState(), playerStats);
                    int spaceIdx = unbuffedLabel.IndexOf(' ');
                    string unbuffedNum = spaceIdx >= 0 ? unbuffedLabel.Substring(0, spaceIdx) : unbuffedLabel;
                    string buffedNum = spaceIdx >= 0 ? leftLabel.Substring(0, leftLabel.IndexOf(' ')) : leftLabel;
                    string statName = spaceIdx >= 0 ? leftLabel.Substring(leftLabel.IndexOf(' ') + 1) : "";
                    string arrow = buffedStat > unbuffedStat
                        ? "<color=#88FF88>\u25B2</color>"
                        : "<color=#FF4444>\u25BC</color>";
                    statDescNormal = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> {buffedNum} {arrow} {statName}";
                    statDescExpanded = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> <color=#666666><s>{unbuffedNum}</s></color> {buffedNum} {statName}";
                    row.desc.text = statDescNormal;
                }
                else
                {
                    row.desc.text = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> {leftLabel}";
                }

                // Right side
                if (before >= 0f && after >= 0f)
                {
                    bool isPercentFormat = format.EndsWith("%");
                    string fmt = format.Replace("%", "");
                    string beforeStr = isPercentFormat ? $"{before.ToString(fmt)}%" : before.ToString(fmt);
                    string afterStr = isPercentFormat ? $"{after.ToString(fmt)}%" : after.ToString(fmt);
                    row.value.text = $"<color=#888888>{beforeStr} <voffset=0.05em>\u2192</voffset></color> {afterStr}";
                }
                else
                {
                    row.value.text = "";
                }
            }
        }

        // Show/hide stat section and its divider
        statContainer.SetActive(hasContent);
        statDivider.SetActive(hasContent);
    }

    private void BuildBuffGrid(ModuleInstance inst)
    {
        var gridGo = buffGridContainer.transform.Find("Grid");
        if (gridGo == null) return;

        foreach (Transform child in gridGo)
            Destroy(child.gameObject);

        var shapeCells = inst.Data.GetShapeCells(inst.Rotation);
        var buffCells = inst.Data.GetBuffCells(inst.Rotation);

        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in shapeCells)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }
        foreach (var c in buffCells)
        {
            if (c.x < minX) minX = c.x; if (c.x > maxX) maxX = c.x;
            if (c.y < minY) minY = c.y; if (c.y > maxY) maxY = c.y;
        }

        int cols = (maxX - minX + 1) + 2;
        int rows = (maxY - minY + 1) + 2;

        var glg = gridGo.GetComponent<GridLayoutGroup>();
        glg.constraintCount = cols;

        var shapeSet = new System.Collections.Generic.HashSet<Vector2Int>();
        var buffSet = new System.Collections.Generic.HashSet<Vector2Int>();
        foreach (var c in shapeCells)
            shapeSet.Add(new Vector2Int(c.x - minX + 1, c.y - minY + 1));
        foreach (var c in buffCells)
            buffSet.Add(new Vector2Int(c.x - minX + 1, c.y - minY + 1));

        Color moduleColor = inst.Data.moduleColor;
        Color buffColor = new Color(moduleColor.r, moduleColor.g, moduleColor.b, 0.3f);
        Color emptyColor = new Color(0.15f, 0.15f, 0.15f, 1f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var cell = new Vector2Int(x, y);
                var go = new GameObject($"cell_{x}_{y}", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(gridGo, false);

                var img = go.GetComponent<Image>();
                img.raycastTarget = false;
                img.color = shapeSet.Contains(cell) ? moduleColor
                          : buffSet.Contains(cell) ? buffColor
                          : emptyColor;
            }
        }
    }

    private IEnumerator PositionNextFrame(ModuleInstance inst)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        PositionTooltip(inst);
        transform.SetAsLastSibling();
        if (detailPanel != null) detailPanel.transform.SetAsLastSibling();
    }

    private void PositionTooltip(ModuleInstance inst)
    {
        if (rootCanvas == null || inst.UIElement == null) return;

        var canvasRt = rootCanvas.GetComponent<RectTransform>();
        float canvasH = canvasRt.rect.height;
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;

        var moduleRt = inst.UIElement.GetComponent<RectTransform>();
        if (moduleRt == null) return;

        Vector3[] corners = new Vector3[4];
        moduleRt.GetWorldCorners(corners);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
            RectTransformUtility.WorldToScreenPoint(cam, (corners[0] + corners[2]) * 0.5f),
            cam, out Vector2 moduleCenter);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
            RectTransformUtility.WorldToScreenPoint(cam, corners[2]),
            cam, out Vector2 moduleTR);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt,
            RectTransformUtility.WorldToScreenPoint(cam, corners[1]),
            cam, out Vector2 moduleTL);

        float tooltipW = rt.rect.width;
        float tooltipH = rt.rect.height;
        float halfH = canvasH * 0.5f;
        float gap = 9f;
        float edgeGap = 20f;

        bool showRight = moduleCenter.x < 0f;
        rt.pivot = new Vector2(0f, 1f);

        float xPos = showRight ? moduleTR.x + gap : moduleTL.x - tooltipW - gap;
        float yPos = moduleCenter.y + tooltipH * 0.5f;
        yPos = Mathf.Clamp(yPos, -halfH + tooltipH + edgeGap, halfH - edgeGap);

        rt.anchoredPosition = new Vector2(xPos, yPos);

        if (detailPanel != null && detailPanel.activeSelf)
        {
            var detailRt = detailPanel.GetComponent<RectTransform>();
            detailRt.anchorMin = rt.anchorMin;
            detailRt.anchorMax = rt.anchorMax;
            if (showRight)
            {
                detailRt.pivot = new Vector2(0f, 1f);
                detailRt.anchoredPosition = new Vector2(xPos + tooltipW + gap, yPos);
            }
            else
            {
                detailRt.pivot = new Vector2(1f, 1f);
                detailRt.anchoredPosition = new Vector2(xPos - gap, yPos);
            }
        }
    }

    private TextMeshProUGUI AddDescRow()
    {
        var go = new GameObject("DescRow", typeof(RectTransform));
        go.transform.SetParent(statContainer.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 22f;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = new Color(0.7f, 0.7f, 0.7f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.raycastTarget = false;
        tmp.richText = true;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        go.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return tmp;
    }

    private (TextMeshProUGUI desc, TextMeshProUGUI value) AddStatRow()
    {
        var rowGo = CreateHorizontalRow("StatRow", 8f, statContainer);
        var hl = rowGo.GetComponent<HorizontalLayoutGroup>();
        hl.childControlHeight = true;
        var desc = CreateTMP(rowGo, 24f, FontStyles.Normal, new Color(0.85f, 0.85f, 0.85f), TextAlignmentOptions.Left, flexibleWidth: 1f);
        var val = CreateTMP(rowGo, 24f, FontStyles.Normal, new Color(1f, 0.9f, 0.4f), TextAlignmentOptions.Right, preferredWidth: 140f);
        val.richText = true;
        statRows.Add((desc, val));
        return (desc, val);
    }


    private void ClearStatRows()
    {
        foreach (Transform child in statContainer.transform)
            Destroy(child.gameObject);
        statRows.Clear();
    }

    private void ClearPassive()
    {
        foreach (Transform child in passiveContainer.transform)
            Destroy(child.gameObject);
        passiveBoxes.Clear();
        passiveBoxTexts.Clear();
        passiveContainer.SetActive(false);
    }

    private void ClearFooter()
    {
        foreach (Transform child in footerContainer.transform)
            Destroy(child.gameObject);
        footerContainer.SetActive(false);
    }

    private void PopulatePassiveCompare(ModuleInstance inst)
    {
        var effect = inst.Data.moduleEffect;
        if (effect == null) return;

        var layout = effect.GetPassiveLayout();
        if (layout == ModuleEffect.PassiveLayout.None) return;

        var emptyState = new ModuleRuntimeState();
        var currentEntries = effect.GetPassiveEntries(inst.Rarity, inst.Level, emptyState);
        var nextEntries = effect.GetPassiveEntries(inst.Rarity, inst.Level + 1, emptyState);

        if (currentEntries == null || nextEntries == null) return;

        for (int i = 0; i < passiveBoxes.Count && i < nextEntries.Length && i < currentEntries.Length; i++)
        {
            var cur = currentEntries[i];
            var next = nextEntries[i];

            bool changed = cur.value != next.value;
            string normalText = changed
                ? $"{next.value}<color=#88FF88>\u25B2</color>"
                : next.value;
            string expandedText = changed
                ? $"<color=#666666><s>{cur.value}</s></color> {next.value}"
                : next.value;

            passiveBoxes[i].value.text = normalText;
            passiveBoxTexts[i] = (normalText, expandedText);
        }
    }

    private void PopulatePassive(ModuleInstance inst)
    {
        ClearPassive();

        var effect = inst.Data.moduleEffect;
        if (effect == null) return;

        var layout = effect.GetPassiveLayout();
        if (layout == ModuleEffect.PassiveLayout.None) return;

        var entries = effect.GetPassiveEntries(inst.Rarity, inst.Level, inst.RuntimeState);
        if (entries == null || entries.Length == 0) return;

        passiveContainer.SetActive(true);

        // Divider
        CreateDivider(passiveContainer);

        // "Passive" label
        var passiveLabelSection = new GameObject("PassiveLabelSection", typeof(RectTransform));
        passiveLabelSection.transform.SetParent(passiveContainer.transform, false);
        var plsImg = passiveLabelSection.AddComponent<Image>();
        plsImg.color = new Color(0.1f, 0.1f, 0.18f, 1f);
        plsImg.raycastTarget = false;
        var plsVlg = passiveLabelSection.AddComponent<VerticalLayoutGroup>();
        plsVlg.padding = new RectOffset(20, 20, 8, 8);
        plsVlg.childControlWidth = true;
        plsVlg.childControlHeight = true;
        plsVlg.childForceExpandWidth = true;
        plsVlg.childForceExpandHeight = false;
        passiveLabelSection.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var passiveLabelGo = new GameObject("PassiveLabel", typeof(RectTransform));
        passiveLabelGo.transform.SetParent(passiveLabelSection.transform, false);
        var passiveLabelTmp = passiveLabelGo.AddComponent<TextMeshProUGUI>();
        passiveLabelTmp.text = "Passive";
        passiveLabelTmp.fontSize = 20f;
        passiveLabelTmp.fontStyle = FontStyles.Italic;
        passiveLabelTmp.color = new Color(0.7f, 0.7f, 1f);
        passiveLabelTmp.alignment = TextAlignmentOptions.Left;
        passiveLabelTmp.raycastTarget = false;

        // Passive description + boxes
        var bodySection = new GameObject("PassiveBodySection", typeof(RectTransform));
        bodySection.transform.SetParent(passiveContainer.transform, false);
        var bodyImg = bodySection.AddComponent<Image>();
        bodyImg.color = new Color(0.07f, 0.07f, 0.1f, 1f);
        bodyImg.raycastTarget = false;
        var bodyVlg = bodySection.AddComponent<VerticalLayoutGroup>();
        bodyVlg.padding = new RectOffset(20, 20, 10, 12);
        bodyVlg.spacing = 10;
        bodyVlg.childControlWidth = true;
        bodyVlg.childControlHeight = true;
        bodyVlg.childForceExpandWidth = true;
        bodyVlg.childForceExpandHeight = false;
        bodySection.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Passive description
        string passiveDesc = effect.PassiveDescription;
        if (!string.IsNullOrEmpty(passiveDesc))
        {
            var descGo = new GameObject("PassiveDesc", typeof(RectTransform));
            descGo.transform.SetParent(bodySection.transform, false);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = passiveDesc;
            descTmp.fontSize = 20f;
            descTmp.fontStyle = FontStyles.Normal;
            descTmp.color = new Color(0.75f, 0.75f, 0.75f);
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.raycastTarget = false;
            descTmp.textWrappingMode = TextWrappingModes.Normal;
            descGo.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        // Box row
        var boxSection = new GameObject("BoxSection", typeof(RectTransform));
        boxSection.transform.SetParent(bodySection.transform, false);
        var boxHlg = boxSection.AddComponent<HorizontalLayoutGroup>();
        boxHlg.padding = new RectOffset(0, 0, 0, 0);
        boxHlg.spacing = 8;
        boxHlg.childControlWidth = true;
        boxHlg.childControlHeight = true;
        boxHlg.childForceExpandWidth = true;  // maintain ratio regardless of content
        boxHlg.childForceExpandHeight = true;  // equal height
        boxSection.AddComponent<LayoutElement>().flexibleWidth = 1f;
        boxSection.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        float[] weights = layout switch
        {
            ModuleEffect.PassiveLayout.Single => new[] { 1f },
            ModuleEffect.PassiveLayout.TwoEqual => new[] { 1f, 1f },
            ModuleEffect.PassiveLayout.TwoNarrowWide => new[] { 0.5f, 1f },
            _ => new[] { 1f }
        };

        for (int i = 0; i < entries.Length && i < weights.Length; i++)
        {
            var entry = entries[i];
            var (valueTmp, labelTmp, sublabelTmp) = CreatePassiveBox(boxSection, weights[i]);

            // Build value text (normal / expanded)
            string arrow = entry.isBuffed ? "<color=#88FF88>\u25B2</color>" : "";
            string normalText = $"{entry.value}{arrow}";
            string expandedText = entry.isBuffed
                ? $"<color=#666666><s>{entry.unbuffedValue}</s></color> {entry.value}"
                : entry.value;

            valueTmp.text = normalText;
            labelTmp.text = entry.label;
            sublabelTmp.text = entry.sublabel ?? "";
            sublabelTmp.gameObject.SetActive(!string.IsNullOrEmpty(entry.sublabel));

            passiveBoxes.Add((valueTmp, labelTmp, sublabelTmp));
            passiveBoxTexts.Add((normalText, expandedText));
        }
    }

    private (TextMeshProUGUI value, TextMeshProUGUI label, TextMeshProUGUI sublabel) CreatePassiveBox(GameObject parent, float flexWeight)
    {
        var box = new GameObject("PassiveBox", typeof(RectTransform));
        box.transform.SetParent(parent.transform, false);

        var boxImg = box.AddComponent<Image>();
        boxImg.color = new Color(0.12f, 0.12f, 0.15f, 1f);
        boxImg.raycastTarget = false;

        var le = box.AddComponent<LayoutElement>();
        le.preferredWidth = 0f;
        le.flexibleWidth = flexWeight;

        var vlg = box.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 2;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.MiddleCenter;

        var valueGo = new GameObject("Value", typeof(RectTransform));
        valueGo.transform.SetParent(box.transform, false);
        var valueTmp = valueGo.AddComponent<TextMeshProUGUI>();
        valueTmp.fontSize = 26f;
        valueTmp.fontStyle = FontStyles.Bold;
        valueTmp.color = Color.white;
        valueTmp.alignment = TextAlignmentOptions.Center;
        valueTmp.raycastTarget = false;
        valueTmp.richText = true;
        valueTmp.overflowMode = TextOverflowModes.Ellipsis;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(box.transform, false);
        var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
        labelTmp.fontSize = 18f;
        labelTmp.fontStyle = FontStyles.Normal;
        labelTmp.color = new Color(0.7f, 0.7f, 0.7f);
        labelTmp.alignment = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        var sublabelGo = new GameObject("Sublabel", typeof(RectTransform));
        sublabelGo.transform.SetParent(box.transform, false);
        var sublabelTmp = sublabelGo.AddComponent<TextMeshProUGUI>();
        sublabelTmp.fontSize = 16f;
        sublabelTmp.fontStyle = FontStyles.Italic;
        sublabelTmp.color = new Color(0.5f, 0.5f, 0.5f);
        sublabelTmp.alignment = TextAlignmentOptions.Center;
        sublabelTmp.raycastTarget = false;

        return (valueTmp, labelTmp, sublabelTmp);
    }

    private GameObject CreateHorizontalRow(string name, float spacing, GameObject parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent != null ? parent.transform : transform, false);

        var hl = go.AddComponent<HorizontalLayoutGroup>();
        hl.spacing = spacing;
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;

        go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return go;
    }

    private GameObject CreateDivider(GameObject parent)
    {
        var go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent.transform, false);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.4f, 1f);
        img.raycastTarget = false;

        var le = go.AddComponent<LayoutElement>();
        le.minHeight = 2f;
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;

        return go;
    }

    private TextMeshProUGUI CreateTMP(GameObject parent, float fontSize, FontStyles style,
        Color color, TextAlignmentOptions alignment,
        float flexibleWidth = -1f, float preferredWidth = -1f)
    {
        var go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        if (flexibleWidth >= 0f || preferredWidth >= 0f)
        {
            var le = go.AddComponent<LayoutElement>();
            if (flexibleWidth >= 0f) le.flexibleWidth = flexibleWidth;
            if (preferredWidth >= 0f) le.preferredWidth = preferredWidth;
        }

        return tmp;
    }
}