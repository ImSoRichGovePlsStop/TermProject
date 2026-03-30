using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StorageTooltipUI : MonoBehaviour
{
    public static StorageTooltipUI Instance { get; private set; }

    private RectTransform _rt;
    private Canvas _rootCanvas;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _infoText;
    private TextMeshProUGUI _costText;

    private void Awake()
    {
        Instance = this;
        _rt = GetComponent<RectTransform>();
        _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;

        _rt.pivot = new Vector2(0.5f, 0.5f);
        _rt.anchorMin = _rt.anchorMax = new Vector2(0.5f, 0.5f);

        var bg = gameObject.AddComponent<Image>();
        bg.color = new Color(0.02f, 0.02f, 0.02f, 0.97f);
        bg.raycastTarget = false;

        var layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 16, 16);
        layout.spacing = 6;
        layout.childControlWidth  = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth  = false;
        layout.childForceExpandHeight = false;

        var fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        _nameText = CreateText(30f, FontStyles.Bold,   Color.white);
        _infoText = CreateText(22f, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
        _costText = CreateText(22f, FontStyles.Normal, Color.yellow);

        gameObject.SetActive(false);
    }

    public void Show(MaterialData data, int count, RectTransform itemRect)
    {
        _nameText.text  = data.moduleName;
        _nameText.color = RarityColor(data.rarity);
        _infoText.text  = $"{data.rarity}   {count} / {data.maxStack}";

        int costVal = data.cost != null && data.cost.Length > 0 ? data.cost[0] : 0;
        if (costVal > 0)
        {
            _costText.text = $"Total cost: {costVal * count}g";
            _costText.gameObject.SetActive(true);
        }
        else
        {
            _costText.gameObject.SetActive(false);
        }

        if (_rootCanvas != null && itemRect != null)
        {
            Vector3[] corners = new Vector3[4];
            itemRect.GetWorldCorners(corners);
            var center = (corners[0] + corners[2]) * 0.5f;
            var screenPos = RectTransformUtility.WorldToScreenPoint(_rootCanvas.worldCamera, center);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvas.transform as RectTransform,
                screenPos,
                _rootCanvas.worldCamera,
                out Vector2 localPos);
            _rt.anchoredPosition = localPos;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private TextMeshProUGUI CreateText(float size, FontStyles style, Color color)
    {
        var go  = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize   = size;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Color RarityColor(Rarity r) => r switch
    {
        Rarity.Common   => new Color(0.75f, 0.75f, 0.75f),
        Rarity.Uncommon => new Color(0.30f, 0.80f, 0.30f),
        Rarity.Rare     => new Color(0.20f, 0.50f, 1.00f),
        Rarity.Epic     => new Color(0.65f, 0.25f, 0.90f),
        Rarity.GOD      => new Color(1.00f, 0.75f, 0.10f),
        _               => Color.white
    };
}
