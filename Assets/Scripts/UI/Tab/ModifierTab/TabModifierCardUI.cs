using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach to each card prefab spawned inside TabModifierUI.
/// Displays a modifier card's name, description, and scope badge.
/// </summary>
public class TabModifierCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI scopeText;
    [SerializeField] private Image           scopeBadgeBackground;

    public FloorModifierCard Data { get; private set; }

    public void Bind(FloorModifierCard card, Color scopeColor)
    {
        Data = card;

        if (cardNameText    != null) cardNameText.text    = card.displayName;
        if (descriptionText != null) descriptionText.text = card.description;

        if (scopeText != null)
        {
            scopeText.text  = card.scope == ModifierScope.WholeRun
                ? "WHOLE RUN"
                : $"THIS FLOOR ({CurrentFloorLabel()})";
            scopeText.color = scopeColor;
        }

        if (scopeBadgeBackground != null)
            scopeBadgeBackground.color = scopeColor;
    }

    static string CurrentFloorLabel()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool  = EnemyPoolManager.Instance;
        int fps   = pool != null ? pool.floorsPerSegment : 3;
        int seg        = (floor - 1) / fps + 1;
        int floorInSeg = (floor - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}
