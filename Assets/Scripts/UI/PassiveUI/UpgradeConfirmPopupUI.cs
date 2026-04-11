using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeConfirmPopupUI : MonoBehaviour
{
    public static UpgradeConfirmPopupUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform requirementContainer;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private System.Action pendingConfirm;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel.SetActive(false);
    }

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirm);
        cancelButton.onClick.AddListener(Hide);
        SetNoWrap(cancelButton);
        SetNoWrap(confirmButton);
    }

    public void Show(WeaponData weapon, int nextLevel, System.Action onConfirm)
    {
        pendingConfirm = onConfirm;
        titleText.text = $"Upgrade to Lv.{nextLevel}?";

        foreach (Transform child in requirementContainer)
            Destroy(child.gameObject);

        var cost = weapon.GetLevelUpCost(nextLevel);
        bool allMet = true;

        if (cost?.materials != null && cost.materials.Length > 0)
        {
            foreach (var req in cost.materials)
            {
                if (req.material == null) continue;
                int have = GetStock(req.material);
                if (have < req.count) allMet = false;
                CreateRow($"{req.material.moduleName}  x{req.count}", $"Have: {have}",
                    have >= req.count ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f));
            }
        }
        else
        {
            CreateRow("No materials required", "", Color.white);
        }

        confirmButton.interactable = allMet;
        var colors = confirmButton.colors;
        colors.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        confirmButton.colors = colors;
        panel.SetActive(true);
    }

    public void Hide()
    {
        pendingConfirm = null;
        panel.SetActive(false);
    }

    private void OnConfirm()
    {
        var cb = pendingConfirm;
        Hide();
        cb?.Invoke();
    }

    private void CreateRow(string leftText, string rightText, Color rightColor)
    {
        var row = new GameObject("row", typeof(RectTransform));
        row.transform.SetParent(requirementContainer, false);
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 36;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.spacing = 12;
        hlg.childControlWidth = true;
        hlg.childForceExpandWidth = true;
        hlg.childControlHeight = false;
        hlg.childForceExpandHeight = false;

        AddText(row.transform, leftText, Color.white, TextAlignmentOptions.Left);
        if (!string.IsNullOrEmpty(rightText))
            AddText(row.transform, rightText, rightColor, TextAlignmentOptions.Right);
    }

    private void AddText(Transform parent, string text, Color color, TextAlignmentOptions alignment)
    {
        var obj = new GameObject("text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 26;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void SetNoWrap(Button button)
    {
        var tmp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private int GetStock(MaterialData data)
    {
        var all = MaterialStorage.Instance.GetAll();
        return all.TryGetValue(data, out int count) ? count : 0;
    }
}
