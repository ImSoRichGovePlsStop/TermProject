using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelfDetonationIndicator : MonoBehaviour
{
    [Header("Child references — auto-found by name if left null")]
    [SerializeField] private Image fillCircle;
    [SerializeField] private TextMeshProUGUI keyLabel;

    [Header("Cooldown state — refilling after detonation")]
    [SerializeField] private Color cooldownColor = new Color(0.35f, 0.35f, 0.35f, 0.55f);

    [Header("Armed state — countdown running")]
    [SerializeField] private Color armedColorFull = new Color(1.00f, 0.35f, 0f, 0.90f);
    [SerializeField] private Color armedColorExpiring = new Color(1.00f, 0.00f, 0f, 0.90f);

    [Header("Colour shifts to expiring when this fraction of countdown remains (0–1)")]
    [SerializeField] private float expiryWarningFraction = 0.30f;

    [Header("World-space offset above player origin")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);

    public void Init()
    {
        if (fillCircle == null)
            fillCircle = transform.Find("Canvas/FillCircle")?.GetComponent<Image>();

        if (keyLabel == null)
            keyLabel = transform.Find("Canvas/KeyLabel")?.GetComponent<TextMeshProUGUI>();

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

        SetKeyVisible(false);
    }

    public void SetCooldown(float ratio)
    {
        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;
            fillCircle.color = cooldownColor;
        }
    }

    public void SetArmed(float ratio)
    {
        gameObject.SetActive(true);

        if (fillCircle != null)
        {
            fillCircle.fillAmount = ratio;

            float warningT = Mathf.InverseLerp(expiryWarningFraction, 0f, ratio);
            fillCircle.color = Color.Lerp(armedColorFull, armedColorExpiring, warningT);
        }

        SetKeyVisible(false);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void SetKeyVisible(bool visible)
    {
        if (keyLabel != null)
            keyLabel.gameObject.SetActive(visible);
    }
}