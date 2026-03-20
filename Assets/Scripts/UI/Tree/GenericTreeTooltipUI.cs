using UnityEngine;
using TMPro;

public class GenericTreeTooltipUI : MonoBehaviour
{
    public static GenericTreeTooltipUI Instance;

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Canvas canvas;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        nameText.fontSize = 20;
        nameText.fontStyle = FontStyles.Bold;

        descriptionText.fontSize = 14;
        descriptionText.fontStyle = FontStyles.Normal;
        descriptionText.color = new Color(0.88f, 0.88f, 0.88f);

        costText.fontSize = 14;
        costText.fontStyle = FontStyles.Bold;
        costText.color = new Color(1f, 0.82f, 0.2f);
    }

    public void Show(GenericTreeNode node, RectTransform nodeRect, bool anchorLeft = false, Color nameColor = default)
    {
        nameText.text = node.nodeName;
        nameText.color = nameColor == default ? new Color(0.4f, 0.85f, 1f) : nameColor;

        descriptionText.text = node.description;
        costText.text = $"Cost: {node.cost} pt";
        panel.SetActive(true);

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, nodeRect.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            screenPos,
            canvas.worldCamera,
            out Vector2 localPos
        );

        float offsetX = anchorLeft ? -nodeRect.rect.width * 2.5f : nodeRect.rect.width * 2.5f;
        float offsetY = -nodeRect.rect.height * 0.6f;
        tooltipRect.anchoredPosition = localPos + new Vector2(offsetX, offsetY);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}