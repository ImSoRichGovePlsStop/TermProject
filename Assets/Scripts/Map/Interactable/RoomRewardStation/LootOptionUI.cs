using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class LootOptionUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform shapePreviewRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image backgroundImage;

    [Header("Colors")]
    [SerializeField] private Color normalColor   = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color hoverColor    = new Color(0.25f, 0.25f, 0.25f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.20f, 0.45f, 0.20f, 1f);

    private ModuleInstance _instance;
    private LootRewardUI   _rewardUI;

    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common   => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare     => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic     => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD      => new Color(1.00f, 0.75f, 0.10f),
        _               => Color.white
    };

    public void Init(ModuleInstance instance, LootRewardUI rewardUI)
    {
        _instance  = instance;
        _rewardUI  = rewardUI;

        if (backgroundImage != null)
            backgroundImage.color = normalColor;

        if (nameText != null)
        {
            nameText.text  = instance.Data.moduleName;
            nameText.color = RarityColor(instance.Rarity);
        }

        if (rarityText != null)
            rarityText.text = instance.Rarity.ToString();

        if (descriptionText != null && instance.Data.moduleEffect != null)
            descriptionText.text = instance.Data.moduleEffect.GetDescription(
                instance.Rarity, instance.Level, instance.RuntimeState);
        else if (descriptionText != null)
            descriptionText.text = "No effect";

        BuildShapePreview(instance.Data, instance.Rarity);
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        if (backgroundImage != null) backgroundImage.color = selectedColor;
        _rewardUI.OnOptionSelected(_instance);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (backgroundImage != null) backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData e)
    {
        if (backgroundImage != null) backgroundImage.color = normalColor;
    }

    private void BuildShapePreview(ModuleData data, Rarity rarity)
    {
        foreach (Transform child in shapePreviewRoot)
            Destroy(child.gameObject);

        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

        float sp = 2f;
        float borderSize = 2f;
        float scaleFactor = 0.9f;

        float maxExpectedUnits = 4f;

        float unitW = (shapePreviewRoot.rect.width + sp) / maxExpectedUnits - sp;
        float unitH = (shapePreviewRoot.rect.height + sp) / maxExpectedUnits - sp;
        float cs = Mathf.Min(unitW, unitH) * scaleFactor;

        shapePreviewRoot.sizeDelta = new Vector2(
            bound.x * (cs + sp) - sp,
            bound.y * (cs + sp) - sp);

        Color borderColor = RarityColor(rarity);

        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
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

            borderRt.sizeDelta = new Vector2(cs + bExtraLeft + bExtraRight, cs + bExtraTop + bExtraBottom);
            borderRt.anchoredPosition = new Vector2(cell.x * (cs + sp) - bExtraLeft, -cell.y * (cs + sp) + bExtraTop);

            var borderImg = borderGo.GetComponent<Image>();
            borderImg.color = borderColor;
            borderImg.raycastTarget = false;
        }

        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
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

            cellRt.sizeDelta = new Vector2(cs - borderSize * 2f + extraLeft + extraRight, cs - borderSize * 2f + extraTop + extraBottom);
            cellRt.anchoredPosition = new Vector2(cell.x * (cs + sp) + borderSize - extraLeft, -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (data.icon != null) cellImg.sprite = data.icon;
            cellImg.color = data.moduleColor;
            cellImg.raycastTarget = true;
        }
    }
}
