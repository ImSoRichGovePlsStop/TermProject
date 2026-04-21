using UnityEngine;
using UnityEngine.UI;

public class PoisonIndicator : MonoBehaviour
{
    [Header("Child references — auto-found by name if left null")]
    [SerializeField] private Image fillCircle;

    [Header("Color")]
    [SerializeField] private Color poisonColor = new Color(0.45f, 0.00f, 0.80f, 0.95f);

    [Header("World-space offset above enemy origin")]
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 0f);

    private enum Mode { Hidden, Stacking, Poisoned }

    private Mode _mode;
    private float _poisonEndTime;
    private float _poisonDuration;

    public void Init()
    {
        if (fillCircle == null)
            fillCircle = transform.Find("Canvas/FillCircle")?.GetComponent<Image>();

        transform.localPosition = localOffset;
        Hide();
    }

    private void Update()
    {
        if (_mode == Mode.Poisoned)
            TickPoison();
    }

    public void ShowStacking(int currentStacks, int required)
    {
        _mode = Mode.Stacking;
        gameObject.SetActive(true);

        if (fillCircle == null) return;

        fillCircle.fillAmount = Mathf.Clamp01((float)currentStacks / required);
        fillCircle.color = poisonColor;
    }

    public void ShowPoisoned(float duration)
    {
        _mode = Mode.Poisoned;
        _poisonDuration = duration;
        _poisonEndTime = Time.time + duration;

        gameObject.SetActive(true);

        if (fillCircle == null) return;

        fillCircle.fillAmount = 1f;
        fillCircle.color = poisonColor;
    }

    public void Hide()
    {
        _mode = Mode.Hidden;
        gameObject.SetActive(false);
    }

    private void TickPoison()
    {
        float remaining = _poisonEndTime - Time.time;
        if (remaining <= 0f)
        {
            Hide();
            return;
        }

        if (fillCircle == null) return;

        fillCircle.fillAmount = Mathf.Clamp01(remaining / _poisonDuration);
        fillCircle.color = poisonColor;
    }
}