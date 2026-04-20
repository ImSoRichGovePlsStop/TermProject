using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class StorageItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MaterialData _data;
    private MaterialInstance _tooltipInst;

    public void Init(MaterialData data, int count)
    {
        _data = data;
        gameObject.layer = LayerMask.NameToLayer("UI");

        _tooltipInst = new MaterialInstance(data);
        for (int i = 1; i < Mathf.Min(count, data.maxStack); i++)
            _tooltipInst.AddStack();

        var rt = GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Invisible raycast catcher
        var bg = gameObject.AddComponent<Image>();
        bg.color = Color.clear;
        bg.raycastTarget = true;

        Color rarityCol = SpriteOutlineUtility.RarityColor(data.rarity);
        float pad = 4f;

        // Grey background
        var bgGo = CreateGO("Background", transform);
        var bgRt = bgGo.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgGo.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.20f, 1f);

        // Rarity outline
        var outlineGo = CreateGO("RarityOutline", transform);
        var outlineRt = outlineGo.GetComponent<RectTransform>();
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.offsetMin = new Vector2(2f, 2f);
        outlineRt.offsetMax = new Vector2(-2f, -2f);
        outlineGo.AddComponent<Image>().color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.7f);

        // Inner dark inset
        var innerGo = CreateGO("Inner", transform);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(4f, 4f);
        innerRt.offsetMax = new Vector2(-4f, -4f);
        innerGo.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);

        // Icon with aspect ratio preserved
        if (data.icon != null)
        {
            var bound = data.GetBoundingSize();
            int maxSide = Mathf.Max(bound.x, bound.y);
            float pct = maxSide >= 3 ? 1.0f : maxSide == 2 ? 0.8f : 0.6f;

            var wrapGo = CreateGO("IconWrap", transform);
            var wrapRt = wrapGo.GetComponent<RectTransform>();
            wrapRt.anchorMin = new Vector2(0.5f, 0.5f);
            wrapRt.anchorMax = new Vector2(0.5f, 0.5f);
            wrapRt.pivot = new Vector2(0.5f, 0.5f);
            wrapRt.anchoredPosition = Vector2.zero;
            // sizeDelta will be set via ContentSizeFitter approach ? use % of inner via anchor offset
            // inner area = cell - 8px (4px each side from outline+inner)
            // We'll use a SizeDeltaFitter approach: set anchorMin/Max to % centered
            float half = (1f - pct) / 2f;
            wrapRt.anchorMin = new Vector2(half, half);
            wrapRt.anchorMax = new Vector2(1f - half, 1f - half);
            wrapRt.offsetMin = new Vector2(4f, 4f);
            wrapRt.offsetMax = new Vector2(-4f, -4f);

            var iconGo = CreateGO("Icon", wrapGo.transform);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var raw = iconGo.AddComponent<RawImage>();
            raw.texture = data.icon.texture;
            raw.color = Color.white;
            raw.raycastTarget = false;
            var arf = iconGo.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = (float)data.icon.texture.width / data.icon.texture.height;
        }
        else
        {
            var colorGo = CreateGO("Color", transform);
            var colorRt = colorGo.GetComponent<RectTransform>();
            colorRt.anchorMin = Vector2.zero;
            colorRt.anchorMax = Vector2.one;
            colorRt.offsetMin = new Vector2(pad + 6f, pad + 6f);
            colorRt.offsetMax = new Vector2(-pad - 6f, -pad - 6f);
            colorGo.AddComponent<Image>().color = data.moduleColor;
        }

        // Count text bottom-right
        if (count > 1)
        {
            var shadowGo = CreateGO("CountShadow", transform);
            var shadowRt = shadowGo.GetComponent<RectTransform>();
            shadowRt.anchorMin = Vector2.zero;
            shadowRt.anchorMax = Vector2.one;
            shadowRt.offsetMin = new Vector2(pad + 1.5f, pad - 1.5f);
            shadowRt.offsetMax = new Vector2(-pad + 1.5f, -pad - 1.5f);
            var shadowTmp = shadowGo.AddComponent<TextMeshProUGUI>();
            shadowTmp.text = count.ToString();
            shadowTmp.fontSize = 22f;
            shadowTmp.color = new Color(0f, 0f, 0f, 0.85f);
            shadowTmp.alignment = TextAlignmentOptions.BottomRight;
            shadowTmp.raycastTarget = false;

            var countGo = CreateGO("Count", transform);
            var countRt = countGo.GetComponent<RectTransform>();
            countRt.anchorMin = Vector2.zero;
            countRt.anchorMax = Vector2.one;
            countRt.offsetMin = new Vector2(pad, pad);
            countRt.offsetMax = new Vector2(-pad - 4f, -pad - 4f);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = count.ToString();
            countTmp.fontSize = 28f;
            countTmp.color = Color.white;
            countTmp.alignment = TextAlignmentOptions.BottomRight;
            countTmp.raycastTarget = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_tooltipInst == null) return;
        ModuleTooltipUI.Instance?.ShowForStorage(_tooltipInst, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ModuleTooltipUI.Instance?.Hide();
    }

    private static GameObject CreateGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }
}