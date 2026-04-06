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

    private TextMeshProUGUI nameText;
    private TextMeshProUGUI levelText;
    private TextMeshProUGUI rarityText;
    private TextMeshProUGUI costText;

    private GameObject statContainer;
    private readonly List<(TextMeshProUGUI desc, TextMeshProUGUI value)> statRows
        = new List<(TextMeshProUGUI, TextMeshProUGUI)>();

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
        le.preferredWidth = 500f;
        le.flexibleWidth = 0f;

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

        CreateDivider(gameObject);

        var statSection = CreateSection("StatSection", new Color(0.05f, 0.05f, 0.05f, 1f), 20, 20, 14, 14);
        statContainer = statSection;
        statSection.GetComponent<VerticalLayoutGroup>().spacing = 4;
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
        }
    }

    public void Show(ModuleInstance inst, GridUI weaponGridUI = null, GridUI bagGridUI = null, GridUI envGridUI = null, DiscardGridUI discardGridUIRef = null, GridUI inputGridUI = null)
    {
        currentInst = inst;
        isDetailExpanded = false;
        isTabExpanded = false;
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
            levelTextNormal = hasLevelBuff ? $"Lv.{displayBuffed} <color=#88FF88>\u25B2</color>" : $"Lv.{displayBase}";
            levelTextExpanded = hasLevelBuff ? $"<color=#666666><s>Lv.{displayBase}</s></color> Lv.{displayBuffed}" : $"Lv.{displayBase}";
        }
        else
        {
            levelTextNormal = "";
            levelTextExpanded = "";
        }
        levelText.text = levelTextNormal;
        // Rarity buff indicator ? check baseRarity array to confirm buff is still active
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
        costText.text = $"$ {(int)inst.GetCostAtLevel()}";

        PopulateStats(inst);

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

    public void Show(MaterialInstance inst)
    {
        currentInst = null;
        nameText.text = inst.MaterialData.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        levelText.text = "";
        rarityText.text = $"{inst.Rarity}  {inst.StackCount}/{inst.MaxStack}";
        costText.text = inst.Cost > 0 ? $"$ {inst.Cost * inst.StackCount}" : "";

        ClearStatRows();
        detailPanel.SetActive(false);
        gameObject.SetActive(true);
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
        if (effect == null) return;

        // Description with bold keywords
        if (!string.IsNullOrEmpty(inst.Data.description))
        {
            string desc = inst.Data.description;
            if (effect.BoldKeywords != null)
                foreach (var kw in effect.BoldKeywords)
                    desc = desc.Replace(kw, $"<b>{kw}</b>");
            AddDescRow().text = desc;
        }

        var (leftLabel, before, after, format) = effect.GetTooltipStats(inst.Rarity, inst.Level, inst.RuntimeState, playerStats);
        if (leftLabel == null) return;

        var row = AddStatRow();
        statDescRef = row.desc;

        // Build left side (with buff indicator if needed)
        var (unbuffedStat, buffedStat) = effect.GetBaseModuleStat(inst.Rarity, inst.Level, inst.RuntimeState);
        bool hasAnyBuff = inst.RuntimeState.isActive && unbuffedStat >= 0f && buffedStat != unbuffedStat;
        if (hasAnyBuff)
        {
            var (unbuffedLabel, _, _, _) = effect.GetTooltipStats(inst.Rarity, inst.Level, new ModuleRuntimeState(), playerStats);
            // strip the stat label from unbuffedLabel to get just the number part e.g. "+50%" from "+50% Damage"
            int spaceIdx = unbuffedLabel.IndexOf(' ');
            string unbuffedNum = spaceIdx >= 0 ? unbuffedLabel.Substring(0, spaceIdx) : unbuffedLabel;
            string buffedNum = spaceIdx >= 0 ? leftLabel.Substring(0, leftLabel.IndexOf(' ')) : leftLabel;
            string statName = spaceIdx >= 0 ? leftLabel.Substring(leftLabel.IndexOf(' ') + 1) : "";
            string arrow = buffedStat > unbuffedStat
                ? "<color=#88FF88>\u25B2</color>"
                : "<color=#FF4444>\u25BC</color>";
            statDescNormal = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> {leftLabel} {arrow}";
            statDescExpanded = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> <color=#666666><s>{unbuffedNum}</s></color> {buffedNum} {statName}";
            row.desc.text = statDescNormal;
        }
        else
        {
            row.desc.text = $"<size=55%><voffset=0.25em>\u25B6</voffset></size> {leftLabel}";
        }

        // Build right side
        if (before < 0f || after < 0f)
        {
            row.value.text = "";
        }
        else
        {
            bool isPercentFormat = format.EndsWith("%");
            string fmt = format.Replace("%", "");
            string beforeStr = isPercentFormat ? $"{before.ToString(fmt)}%" : before.ToString(fmt);
            string afterStr = isPercentFormat ? $"{after.ToString(fmt)}%" : after.ToString(fmt);
            row.value.text = $"<color=#888888>{beforeStr} \U0001F862</color> {afterStr}";
        }
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
        hl.childControlHeight = false;
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

    private void CreateDivider(GameObject parent)
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