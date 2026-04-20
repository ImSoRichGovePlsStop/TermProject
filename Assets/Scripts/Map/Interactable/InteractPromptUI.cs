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
    [SerializeField] private float xOffset = 1.5f;
    [SerializeField] private float yOffset = 0f;

    private Transform     _target;
    private Transform     _player;
    private RectTransform _rt;
    private Camera        _cam;

    private void Awake()
    {
        _rt  = GetComponent<RectTransform>();
        _cam = Camera.main;
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null) _player = playerObj.transform;
        gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        if (_target == null) return;
        if (_cam == null) _cam = Camera.main;
        if (_player == null) _player = GameObject.FindWithTag("Player")?.transform;

        float side = (_player != null && _player.position.x > _target.position.x) ? -1f : 1f;

        float halfWidth = 0f;
        var col = _target.GetComponent<Collider>();
        if (col != null) halfWidth = col.bounds.extents.x;

        Vector3 worldPos = new Vector3(
            _target.position.x + (halfWidth + xOffset) * side,
            yOffset,
            _target.position.z);

        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
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
