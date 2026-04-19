using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SiphonBuffIndicator : MonoBehaviour
{
    [Header("Child references — auto-found by name if left null")]
    [SerializeField] private Image fillCircle;
    [SerializeField] private TextMeshProUGUI countText;

    [Header("Fill colours")]
    [SerializeField] private Color fillColorFull = new Color(0.60f, 0.00f, 1.00f, 0.90f);
    [SerializeField] private Color fillColorExpiring = new Color(1.00f, 0.20f, 0.20f, 0.90f);

    [Header("Colour shifts to expiring when this fraction of time remains (0–1)")]
    [SerializeField] private float expiryWarningFraction = 0.25f;

    [Header("World-space offset above player origin")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 2.2f, 0f);

    private float _duration;
    private float _elapsed;
    private bool _running;

    public void Init(int enemyCount, float duration)
    {
        _duration = Mathf.Max(duration, 0.01f);
        _elapsed = 0f;
        _running = true;

        if (fillCircle == null)
            fillCircle = transform.Find("Canvas/FillCircle")?.GetComponent<Image>();

        if (countText == null)
            countText = transform.Find("Canvas/CountText")?.GetComponent<TextMeshProUGUI>();

        if (fillCircle != null)
        {
            fillCircle.fillAmount = 1f;
            fillCircle.color = fillColorFull;
        }

        if (countText != null)
            countText.text = enemyCount.ToString();

        transform.localPosition = localOffset;
    }

    private void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;

        float remaining = Mathf.Clamp01(1f - (_elapsed / _duration));

        if (fillCircle != null)
        {
            fillCircle.fillAmount = remaining;

            float warningT = Mathf.InverseLerp(expiryWarningFraction, 0f, remaining);
            fillCircle.color = Color.Lerp(fillColorFull, fillColorExpiring, warningT);
        }

        if (_elapsed >= _duration)
        {
            _running = false;
            Destroy(gameObject);
        }
    }
}