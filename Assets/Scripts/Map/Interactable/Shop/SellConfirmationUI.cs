using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SellConfirmationUI : MonoBehaviour
{
    public static SellConfirmationUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI sellPriceText;
    [SerializeField] private RectTransform shapePreviewRoot;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private ModuleInstance _pendingInstance;

    private void Awake()
    {
        Instance = this;
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(OnCancel);
        gameObject.SetActive(false);
    }

    private static int CalcSellPrice(ModuleInstance inst)
    {
        float sellMult = RunManager.Instance != null ? RunManager.Instance.EffectiveSellMultiplier : 1f;
        if (inst is MaterialInstance mat)
            return Mathf.RoundToInt(mat.Cost * mat.StackCount * sellMult);
        return Mathf.RoundToInt(inst.Data.cost[(int)inst.Rarity] * 0.5f * sellMult);
    }

    public void Show(ModuleInstance inst, Vector2 screenPos)
    {
        _pendingInstance = inst;

        string label = inst is MaterialInstance m && m.StackCount > 1
            ? $"{inst.Data.moduleName} ×{m.StackCount}"
            : inst.Data.moduleName;
        itemNameText.text = $"Selling: {label}";
        sellPriceText.text = $"Selling for: {CalcSellPrice(inst)} coins";

        BuildShapePreview(inst.Data, inst.Rarity);


        GetComponent<RectTransform>().position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _pendingInstance = null;
        ClearPreview();
        gameObject.SetActive(false);
    }

    private void OnConfirm()
    {
        if (_pendingInstance == null) return;

        int sellPrice = CalcSellPrice(_pendingInstance);
        CurrencyManager.Instance?.AddCoins(sellPrice);

        var grid = _pendingInstance.CurrentGrid;
        grid?.Remove(_pendingInstance);

        var ui = _pendingInstance.UIElement as MonoBehaviour;
        if (ui != null) Object.Destroy(ui.gameObject);

        Hide();
    }

    private void OnCancel()
    {
        Hide();
    }

    private void ClearPreview()
    {
        foreach (Transform child in shapePreviewRoot)
            Destroy(child.gameObject);
    }

    private void BuildShapePreview(ModuleData data, Rarity rarity)
    {
        ClearPreview();

        if (data is MaterialData matData)
        {
            BuildMaterialPreview(matData);
            return;
        }

        float sp = 2f;
        float borderSize = 2f;
        float scaleFactor = 0.9f;
        float maxExpectedUnits = 4f;

        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

        float previewSize = shapePreviewRoot.rect.width;
        float unitW = (previewSize + sp) / maxExpectedUnits - sp;
        float cs = unitW * scaleFactor;

        float shapeW = bound.x * (cs + sp) - sp;
        float shapeH = bound.y * (cs + sp) - sp;
        float xOffset = (previewSize - shapeW) / 2f;
        float yOffset = -(shapePreviewRoot.rect.height - shapeH) / 2f;
        Color borderColor = RarityColor(rarity);

        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}",
                                          typeof(RectTransform), typeof(Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(shapePreviewRoot, false);
            borderRt.pivot = new Vector2(0f, 1f);
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(0f, 1f);

            bool bHasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool bHasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool bHasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool bHasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float bExtraRight = bHasRight ? borderSize + sp : 0f;
            float bExtraLeft = bHasLeft ? borderSize + sp : 0f;
            float bExtraBottom = bHasBottom ? borderSize + sp : 0f;
            float bExtraTop = bHasTop ? borderSize + sp : 0f;

            borderRt.sizeDelta = new Vector2(
                cs + bExtraLeft + bExtraRight,
                cs + bExtraTop + bExtraBottom);

            borderRt.anchoredPosition = new Vector2(
                 cell.x * (cs + sp) - bExtraLeft + xOffset,
                -cell.y * (cs + sp) + bExtraTop + yOffset);

            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;
        }

        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}",
            typeof(RectTransform), typeof(Image));
            var cellRt = go.GetComponent<RectTransform>();
            cellRt.SetParent(shapePreviewRoot, false);
            cellRt.pivot = new Vector2(0f, 1f);
            cellRt.anchorMin = new Vector2(0f, 1f);
            cellRt.anchorMax = new Vector2(0f, 1f);

            bool hasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool hasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool hasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool hasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float extraRight = hasRight ? borderSize + sp : 0f;
            float extraLeft = hasLeft ? borderSize + sp : 0f;
            float extraBottom = hasBottom ? borderSize + sp : 0f;
            float extraTop = hasTop ? borderSize + sp : 0f;

            cellRt.sizeDelta = new Vector2(
                cs - borderSize * 2f + extraLeft + extraRight,
                cs - borderSize * 2f + extraTop + extraBottom);

            cellRt.anchoredPosition = new Vector2(
                 cell.x * (cs + sp) + borderSize - extraLeft + xOffset,
                -cell.y * (cs + sp) - borderSize + extraTop + yOffset);

            var cellImg = go.GetComponent<Image>();
            if (data.icon != null) cellImg.sprite = data.icon;
            cellImg.color = data.moduleColor;
            cellImg.raycastTarget = false;
        }
    }

    private void BuildMaterialPreview(MaterialData data)
    {
        var bound = data.GetBoundingSize();
        float sp = 3f;
        float availableWidth  = shapePreviewRoot.rect.width  - 2;
        float availableHeight = shapePreviewRoot.rect.height - 2;
        float cs = Mathf.Min((availableWidth  + sp) / bound.x - sp,
                             (availableHeight + sp) / bound.y - sp);

        shapePreviewRoot.sizeDelta = new Vector2(bound.x * (cs + sp) - sp,
                                                 bound.y * (cs + sp) - sp);

        if (data.icon != null)
        {
            var tex = data.icon.texture;

            // Rarity outline border
            float pixelsPerUnit = tex.width / (cs * bound.x);
            int thickness = Mathf.Max(1, Mathf.RoundToInt(4f * pixelsPerUnit));
            var outlineTex = SpriteOutlineUtility.GetOrCreate(
                tex, SpriteOutlineUtility.RarityColor(data.rarity), thickness);

            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(RawImage));
            var borderRt  = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(shapePreviewRoot, false);
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = borderRt.offsetMax = Vector2.zero;
            var borderRaw = borderGo.GetComponent<RawImage>();
            borderRaw.texture = outlineTex;
            borderRaw.raycastTarget = false;

            // Icon
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(RawImage));
            var iconRt  = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(shapePreviewRoot, false);
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = iconRt.offsetMax = Vector2.zero;
            var iconRaw = iconGo.GetComponent<RawImage>();
            iconRaw.texture = tex;
            iconRaw.raycastTarget = false;
        }
        else
        {
            // Fallback: solid colour rectangle
            var go  = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rt  = go.GetComponent<RectTransform>();
            rt.SetParent(shapePreviewRoot, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<UnityEngine.UI.Image>();
            img.color = data.moduleColor;
            img.raycastTarget = false;

            // Border overlay using rarity colour
            var borderGo = new GameObject("Border", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(shapePreviewRoot, false);
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(-3, -3);
            borderRt.offsetMax = new Vector2(3, 3);
            borderRt.SetAsFirstSibling();
            var borderImg = borderGo.GetComponent<UnityEngine.UI.Image>();
            borderImg.color = SpriteOutlineUtility.RarityColor(data.rarity);
            borderImg.raycastTarget = false;
        }
    }

    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD => new Color(1.00f, 0.75f, 0.10f),
        _ => Color.white
    };
}
