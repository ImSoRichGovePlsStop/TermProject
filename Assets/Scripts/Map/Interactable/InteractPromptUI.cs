using TMPro;
using UnityEngine;

public class InteractPromptUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject      descriptionRow;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private GameObject      costRow;
    [SerializeField] private TextMeshProUGUI actionText;

    [Header("Positioning")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);

    private Transform     _target;
    private RectTransform _rt;
    private Camera        _cam;

    private void Awake()
    {
        _rt  = GetComponent<RectTransform>();
        _cam = Camera.main;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (_cam == null) _cam = Camera.main;

        Vector3 screenPos = _cam.WorldToScreenPoint(_target.position + worldOffset);
        if (screenPos.z < 0f)
        {
            gameObject.SetActive(false);
            return;
        }
        _rt.position = screenPos;
    }

    public void Show(InteractInfo info, Transform target)
    {
        _target = target;

        if (nameText   != null) nameText.text   = info.name;
        if (actionText != null) actionText.text = $"[E]  {info.actionText}";

        bool hasDesc = !string.IsNullOrEmpty(info.description);
        descriptionRow?.SetActive(hasDesc);
        if (hasDesc && descriptionText != null)
            descriptionText.text = info.description;

        bool hasCost = info.cost.HasValue;
        costRow?.SetActive(hasCost);
        if (hasCost && costText != null)
            costText.text = $"$ {info.cost.Value}";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        _target = null;
        gameObject.SetActive(false);
    }
}
