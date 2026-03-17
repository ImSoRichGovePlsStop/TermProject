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

    public void Show(ModuleInstance inst, Vector2 screenPos)
    {
        _pendingInstance = inst;

        itemNameText.text = "Selling: "+ inst.Data.moduleName;
        int sellPrice = Mathf.RoundToInt(inst.Data.cost[(int)inst.Rarity] * 0.5f);
        sellPriceText.text = $"Selling for: {sellPrice} coins";

        BuildShapePreview(inst.Data, inst.Rarity);

        
        GetComponent<RectTransform>().position = screenPos;

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

        int sellPrice = Mathf.RoundToInt(_pendingInstance.Data.cost[(int)_pendingInstance.Rarity] * 0.5f);
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

        float sp = 3f;
        float borderSize = 3f;


        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

        float availableWidth = shapePreviewRoot.rect.width-2;
        float availableHeight = shapePreviewRoot.rect.height-2;

        float csFromWidth = (availableWidth + sp) / bound.x - sp;
        float csFromHeight = (availableHeight + sp) / bound.y - sp;
        float cs = Mathf.Min(csFromWidth, csFromHeight); 

        shapePreviewRoot.sizeDelta = new Vector2(
            bound.x * (cs + sp) - sp,
            bound.y * (cs + sp) - sp);

        shapePreviewRoot.sizeDelta = new Vector2(
            bound.x * (cs + sp) - sp,
            bound.y * (cs + sp) - sp);

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
                 cell.x * (cs + sp) - bExtraLeft,
                -cell.y * (cs + sp) + bExtraTop);

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
                 cell.x * (cs + sp) + borderSize - extraLeft,
                -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (data.icon != null) cellImg.sprite = data.icon;
            cellImg.color = data.moduleColor;
            cellImg.raycastTarget = false;
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