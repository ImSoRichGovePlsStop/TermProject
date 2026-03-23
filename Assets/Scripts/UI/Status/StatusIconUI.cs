using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatusIconUI : MonoBehaviour
{
    [Header("Borders")]
    [SerializeField] private Image outerBorder;
    [SerializeField] private Image innerBorderFill;

    [Header("Main Icon")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image stackDarkSweep;

    [Header("Text")]
    [SerializeField] private TMP_Text stackText;

    public void UpdateUI(StatusEntry entry)
    {
        outerBorder.color = entry.outerBorderType == StatusBorderType.Gold
            ? new Color(1f, 0.82f, 0.2f) : Color.white;

        innerBorderFill.gameObject.SetActive(entry.isInnerBorderVisible);
        innerBorderFill.fillAmount = entry.innerBorderFill;

        iconImage.sprite = entry.icon;
        iconImage.canvasRenderer.SetAlpha(entry.stackCount > 0 || entry.innerBorderFill > 0 ? 1f : 0.4f);

        stackDarkSweep.fillAmount = entry.stackExpireFill;

        stackText.text = entry.stackCount > 0 ? entry.stackCount.ToString() : "";
    }
}