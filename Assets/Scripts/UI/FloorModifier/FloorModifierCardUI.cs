using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to the FloorModifierCard prefab root.
/// Call Bind() to populate, subscribe to OnPicked before showing.
/// </summary>
public class FloorModifierCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI scopeText;
    [SerializeField] private Image           scopeBadgeBackground;
    [SerializeField] private Button          selectButton;

    [Header("Scope Badge Colors")]
    [SerializeField] private Color wholeRunColor  = new Color(0.85f, 0.6f, 0.1f);
    [SerializeField] private Color nextFloorColor = new Color(0.2f, 0.6f, 0.9f);

    public event Action<FloorModifierCardUI> OnPicked;
    public FloorModifierCard Data { get; private set; }

    void Awake()
    {
        selectButton.onClick.AddListener(() => OnPicked?.Invoke(this));
    }

    public void Bind(FloorModifierCard card)
    {
        Data = card;

        if (cardNameText != null)    cardNameText.text    = card.displayName;
        if (descriptionText != null) descriptionText.text = card.description;

        if (scopeText != null)
        {
            bool isWholeRun  = card.scope == ModifierScope.WholeRun;
            Color badgeColor = isWholeRun ? wholeRunColor : nextFloorColor;
            scopeText.text   = isWholeRun ? "WHOLE RUN" : "THIS FLOOR";
            scopeText.color  = badgeColor;
            if (scopeBadgeBackground != null) scopeBadgeBackground.color = badgeColor;
        }
    }

    public void SetInteractable(bool interactable) => selectButton.interactable = interactable;
}
