using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private RectTransform shapePreviewRoot;

    private TestModuleEntry _entry;
    private ShopUI _shopUI;
    private int _entryIndex;
    private bool _purchased = false;

    public void Init(TestModuleEntry entry, ShopUI shopUI, int entryIndex)
    {
        _entry = entry;
        _shopUI = shopUI;
        _entryIndex = entryIndex;

        if (nameText != null)
            nameText.text = entry.data.moduleName;

        BuildShapePreview(entry.data, entry.rarity);
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (_purchased) return;
        _shopUI.BeginShopDrag(_entry, e, this);
    }

    public void OnDrag(PointerEventData e)
    {
        if (_purchased) return;
        _shopUI.UpdateShopDrag(e);
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (_purchased) return;
        _shopUI.EndShopDrag(e);
    }

    public void MarkPurchased()
    {
        if (_purchased) return;
        _purchased = true;
        _shopUI.RegisterSold(_entryIndex);

        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0.4f;
        cg.blocksRaycasts = false;

        if (nameText != null)
            nameText.text = $"{_entry.data.moduleName} [SOLD]";
    }

    private void BuildShapePreview(ModuleData data, Rarity rarity)
    {
        foreach (Transform child in shapePreviewRoot)
            Destroy(child.gameObject);

        float cs = 20f;
        float sp = 2f;
        float borderSize = 2f;

        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

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