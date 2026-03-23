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
        if (entry.outerBorderType == StatusBorderType.Gold)
        {
            outerBorder.color = new Color(1f, 0.75f, 0f);
        }
        else if (entry.outerBorderType == StatusBorderType.Red)
        {
            outerBorder.color = Color.red;
        }
        else
        {
            outerBorder.color = new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        innerBorderFill.gameObject.SetActive(entry.showInnerBorder);
        innerBorderFill.fillAmount = entry.innerFill;
        innerBorderFill.fillClockwise = entry.innerFillClockwise;

        iconImage.sprite = entry.icon;
        iconImage.canvasRenderer.SetAlpha(entry.isActive ? 1f : 0.4f);

        stackDarkSweep.fillAmount = entry.sweepFill;
        stackDarkSweep.fillClockwise = entry.sweepClockwise;

        stackText.text = entry.count > 0 ? entry.count.ToString() : "";
    }
}