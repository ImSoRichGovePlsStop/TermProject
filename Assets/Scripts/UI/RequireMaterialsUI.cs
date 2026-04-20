using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RequiredMaterialsUI : MonoBehaviour
{
    [SerializeField] private float cardSize = 48f;
    [SerializeField] private float cardSpacing = 16f;
    [SerializeField] private bool showHeader = true;

    private static readonly Color ColOk = new Color(0.63f, 0.88f, 0.56f);
    private static readonly Color ColBad = new Color(0.88f, 0.35f, 0.35f);

    private Transform _iconRow;
    private bool _built = false;

    private void Awake() { Build(); }
    private void OnEnable() { LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>()); }

    private void Build()
    {
        if (_built) return;

        var rt = GetComponent<RectTransform>() ?? gameObject.AddComponent<RectTransform>();

        var vlg = gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.spacing = 6f;
        vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
        vlg.childControlHeight = false; vlg.childForceExpandHeight = false;

        // Header
        if (showHeader)
        {
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
            headerGo.layer = LayerMask.NameToLayer("UI");
            headerGo.transform.SetParent(transform, false);
            headerGo.AddComponent<LayoutElement>().preferredHeight = 22f;
            var tmp = headerGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "REQUIRED MATERIALS";
            tmp.fontSize = 18f; tmp.fontStyle = FontStyles.Bold;
            tmp.color = new Color(0.7f, 0.7f, 0.7f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
        }

        // Icon row
        var rowGo = new GameObject("IconRow", typeof(RectTransform));
        rowGo.layer = LayerMask.NameToLayer("UI");
        rowGo.transform.SetParent(transform, false);
        rowGo.AddComponent<LayoutElement>().preferredHeight = cardSize + 22f;
        _iconRow = rowGo.transform;
        var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = cardSpacing;
        hlg.childControlWidth = false; hlg.childForceExpandWidth = false;
        hlg.childControlHeight = false; hlg.childForceExpandHeight = false;

        _built = true;
    }

    public void Show(MaterialRequirement[] reqs)
    {
        if (!_built) Build();
        if (_iconRow == null) return;

        foreach (Transform child in _iconRow) Destroy(child.gameObject);
        if (reqs == null || reqs.Length == 0) return;

        foreach (var req in reqs)
        {
            if (req.material == null) continue;
            int have = MaterialStorage.Instance.GetAll().TryGetValue(req.material, out var v) ? v : 0;
            Color col = have >= req.count ? ColOk : ColBad;
            Color rarity = SpriteOutlineUtility.RarityColor(req.material.rarity);

            // Card
            var card = new GameObject("Card", typeof(RectTransform));
            card.layer = LayerMask.NameToLayer("UI");
            card.transform.SetParent(_iconRow, false);
            var cardCsf = card.AddComponent<ContentSizeFitter>();
            cardCsf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            cardCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cardVlg = card.AddComponent<VerticalLayoutGroup>();
            cardVlg.childAlignment = TextAnchor.UpperCenter;
            cardVlg.spacing = -8f;
            cardVlg.padding = new RectOffset(2, 2, 2, -36);
            cardVlg.childControlWidth = false; cardVlg.childForceExpandWidth = false;
            cardVlg.childControlHeight = false; cardVlg.childForceExpandHeight = false;

            // Icon box
            var itemGo = MakeGO("Item", card.transform);
            itemGo.AddComponent<LayoutElement>().preferredWidth =
            itemGo.GetComponent<LayoutElement>().preferredHeight = cardSize;

            MakeStretch("Background", itemGo.transform).AddComponent<Image>()
                .color = new Color(0.18f, 0.18f, 0.20f, 1f);

            var outlineRt = MakeOffset("Outline", itemGo.transform, 2f).GetComponent<RectTransform>();
            outlineRt.gameObject.AddComponent<Image>().color =
                new Color(rarity.r, rarity.g, rarity.b, 0.7f);

            MakeOffset("Inner", itemGo.transform, 4f).AddComponent<Image>()
                .color = new Color(0.12f, 0.12f, 0.14f, 1f);

            if (req.material.icon != null)
            {
                var bound = req.material.GetBoundingSize();
                int maxSide = Mathf.Max(bound.x, bound.y);
                float pct = maxSide >= 3 ? 1f : maxSide == 2 ? 0.8f : 0.6f;
                float half = (1f - pct) / 2f;

                var wrapGo = MakeGO("IconWrap", itemGo.transform);
                var wrapRt = wrapGo.GetComponent<RectTransform>();
                wrapRt.anchorMin = new Vector2(half, half);
                wrapRt.anchorMax = new Vector2(1f - half, 1f - half);
                wrapRt.offsetMin = new Vector2(2f, 2f);
                wrapRt.offsetMax = new Vector2(-2f, -2f);

                var iconGo = MakeGO("Icon", wrapGo.transform);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
                iconRt.offsetMin = Vector2.zero; iconRt.offsetMax = Vector2.zero;
                var raw = iconGo.AddComponent<RawImage>();
                raw.texture = req.material.icon.texture;
                raw.color = Color.white; raw.raycastTarget = false;
                var arf = iconGo.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                arf.aspectRatio = (float)req.material.icon.texture.width / req.material.icon.texture.height;
            }

            // Count text
            var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
            countGo.layer = LayerMask.NameToLayer("UI");
            countGo.transform.SetParent(card.transform, false);
            countGo.AddComponent<LayoutElement>().preferredHeight = 18f;
            var countTmp = countGo.GetComponent<TextMeshProUGUI>();
            countTmp.text = $"{have}/{req.count}";
            countTmp.fontSize = 16f; countTmp.color = col;
            countTmp.alignment = TextAlignmentOptions.Center;
            countTmp.raycastTarget = false;
        }
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeStretch(string name, Transform parent)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    private static GameObject MakeOffset(string name, Transform parent, float inset)
    {
        var go = MakeGO(name, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
        return go;
    }
}