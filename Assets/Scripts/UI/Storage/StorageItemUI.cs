using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StorageItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform shapePreviewRoot;

    private MaterialData _data;
    private int _count;

    private void Awake()
    {
        var img = GetComponent<Image>();
        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = Color.clear;
        }
        img.raycastTarget = true;
    }

    public void Init(MaterialData data, int count)
    {
        _data = data;
        _count = count;
        BuildShapePreview(data, data.rarity);
        BuildStackText(count);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_data == null) return;
        GetTooltip()?.Show(_data, _count, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GetTooltip()?.Hide();
    }

    private StorageTooltipUI GetTooltip()
    {
        if (StorageTooltipUI.Instance != null) return StorageTooltipUI.Instance;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return null;
        var root = canvas.rootCanvas;
        var go = new GameObject("StorageTooltip", typeof(RectTransform));
        go.transform.SetParent(root.transform, false);
        return go.AddComponent<StorageTooltipUI>();
    }

    private void BuildShapePreview(MaterialData data, Rarity rarity)
    {
        foreach (Transform child in shapePreviewRoot)
            Destroy(child.gameObject);

        float cs = 60f;
        float sp = 3f;
        float borderSize = 2f;

        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

        shapePreviewRoot.anchorMin = new Vector2(0.5f, 0.5f);
        shapePreviewRoot.anchorMax = new Vector2(0.5f, 0.5f);
        shapePreviewRoot.pivot = new Vector2(0.5f, 0.5f);
        shapePreviewRoot.anchoredPosition = Vector2.zero;
        shapePreviewRoot.sizeDelta = new Vector2(
            bound.x * (cs + sp) - sp,
            bound.y * (cs + sp) - sp);

        if (data.icon != null)
        {
            var borderGo = new GameObject("BorderGlow", typeof(RectTransform), typeof(RawImage));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(shapePreviewRoot, false);
            borderRt.pivot = new Vector2(0.5f, 0.5f);
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = Vector2.zero;
            borderRt.offsetMax = Vector2.zero;

            var tex = data.icon.texture;
            float pixelsPerUnit = tex.width / (cs * bound.x);
            int thickness = Mathf.Max(1, Mathf.RoundToInt(borderSize * pixelsPerUnit));
            var outlineTex = SpriteOutlineUtility.GetOrCreate(tex, SpriteOutlineUtility.RarityColor(rarity), thickness);

            var raw = borderGo.GetComponent<RawImage>();
            raw.texture = outlineTex;
            raw.color = Color.white;
            raw.uvRect = new Rect(0, 0, 1, 1);
            raw.raycastTarget = false;
        }
        else
        {
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
            cellImg.color = data.icon != null ? Color.clear : data.moduleColor;
            cellImg.raycastTarget = false;
        }

        if (data.icon != null)
        {
            var iconGo = new GameObject("IconOverlay", typeof(RectTransform), typeof(RawImage));
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.SetParent(shapePreviewRoot, false);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;

            var raw = iconGo.GetComponent<RawImage>();
            raw.texture = data.icon.texture;
            raw.color = Color.white;
            raw.uvRect = new Rect(0, 0, 1, 1);
            raw.raycastTarget = false;
        }
    }

    private void BuildStackText(int count)
    {
        if (count <= 1) return;

        var stackGo = new GameObject("StackText", typeof(RectTransform), typeof(TextMeshProUGUI));
        var stackRt = stackGo.GetComponent<RectTransform>();
        stackRt.SetParent(shapePreviewRoot, false);
        stackRt.anchorMin = Vector2.zero;
        stackRt.anchorMax = Vector2.zero;
        stackRt.pivot = new Vector2(1f, 0f);
        stackRt.anchoredPosition = new Vector2(shapePreviewRoot.sizeDelta.x, 0f);
        stackRt.sizeDelta = new Vector2(shapePreviewRoot.sizeDelta.x, 24f);

        var tmp = stackGo.GetComponent<TextMeshProUGUI>();
        tmp.text = count.ToString();
        tmp.fontSize = 24f;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.raycastTarget = false;

        var shadowGo = new GameObject("StackTextShadow", typeof(RectTransform), typeof(TextMeshProUGUI));
        var shadowRt = shadowGo.GetComponent<RectTransform>();
        shadowRt.SetParent(shapePreviewRoot, false);
        shadowRt.anchorMin = stackRt.anchorMin;
        shadowRt.anchorMax = stackRt.anchorMax;
        shadowRt.pivot = stackRt.pivot;
        shadowRt.anchoredPosition = stackRt.anchoredPosition + new Vector2(1.5f, -1.5f);
        shadowRt.sizeDelta = stackRt.sizeDelta;
        shadowGo.transform.SetSiblingIndex(stackGo.transform.GetSiblingIndex());

        var shadowTmp = shadowGo.GetComponent<TextMeshProUGUI>();
        shadowTmp.text = count.ToString();
        shadowTmp.fontSize = 24f;
        shadowTmp.color = new Color(0f, 0f, 0f, 0.8f);
        shadowTmp.alignment = TextAlignmentOptions.BottomRight;
        shadowTmp.raycastTarget = false;
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