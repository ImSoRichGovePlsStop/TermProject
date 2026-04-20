using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SiphonBuffIndicator : MonoBehaviour
{
    [Header("Child references — auto-found by name if left null")]
    [SerializeField] private Image fillCircle;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Cooldown state")]
    [SerializeField] private Color cooldownColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);

    [Header("Buff active state")]
    [SerializeField] private Color buffColorFull = new Color(0.60f, 0.00f, 1.00f, 0.90f);
    [SerializeField] private Color buffColorExpiring = new Color(1.00f, 0.20f, 0.20f, 0.90f);

    [Header("Colour shifts to expiring when this fraction of buff time remains (0–1)")]
    [SerializeField] private float expiryWarningFraction = 0.25f;

    [Header("World-space offset above player origin")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);

    public void Init()
    {
        if (fillCircle == null)
            fillCircle = transform.Find("Canvas/FillCircle")?.GetComponent<Image>();

        if (countText == null)
            countText = transform.Find("Canvas/CountText")?.GetComponent<TextMeshProUGUI>();

        transform.localPosition = localOffset;

        Hide();
    }

    public void ShowCooldown(float ratio)
    {
        gameObject.SetActive(true);

        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;
            fillCircle.color = cooldownColor;
        }

        SetCountVisible(false);
    }

    public void SetCooldown(float ratio)
    {
        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;
            fillCircle.color = cooldownColor;
        }
    }

    public void ShowBuff(float ratio, int enemyCount)
    {
        gameObject.SetActive(true);

        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;

            float warningT = Mathf.InverseLerp(expiryWarningFraction, 0f, ratio);
            fillCircle.color = Color.Lerp(buffColorFull, buffColorExpiring, warningT);
        }

        if (countText != null)
        {
            countText.gameObject.SetActive(true);
            countText.text = enemyCount.ToString();
        }
    }

    public void SetBuff(float ratio)
    {
        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;

            float warningT = Mathf.InverseLerp(expiryWarningFraction, 0f, ratio);
            fillCircle.color = Color.Lerp(buffColorFull, buffColorExpiring, warningT);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetCountVisible(bool visible)
    {
        if (countText != null)
            countText.gameObject.SetActive(visible);
    }
}