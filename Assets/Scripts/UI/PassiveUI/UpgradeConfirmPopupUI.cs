using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeConfirmPopupUI : MonoBehaviour
{
    public static UpgradeConfirmPopupUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Transform effectContainer;
    [SerializeField] private RequiredMaterialsUI reqMaterialsUI;
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

        // Show upgrade effects
        if (effectContainer != null)
        {
            foreach (Transform child in effectContainer) Destroy(child.gameObject);

            int points = WeaponLevelManager.Instance?.GetPointsForLevel(nextLevel) ?? 0;
            var currGrid = weapon.GetGridSize(nextLevel - 1);
            var nextGrid = weapon.GetGridSize(nextLevel);
            bool gridChanged = nextGrid != currGrid;

            if (points > 0)
                AddEffectText($"+{points} Passive Point{(points > 1 ? "s" : "")}", new Color(0.5f, 0.9f, 1f));
            if (gridChanged)
                AddEffectText($"Weapon Grid {currGrid.x}×{currGrid.y} \u2192 {nextGrid.x}×{nextGrid.y}", new Color(1f, 0.85f, 0.2f));
        }

        var cost = weapon.GetLevelUpCost(nextLevel);
        bool allMet = true;

        if (cost?.materials != null && cost.materials.Length > 0)
        {
            reqMaterialsUI?.Show(cost.materials);
            foreach (var req in cost.materials)
            {
                if (req.material == null) continue;
                if (GetStock(req.material) < req.count) allMet = false;
            }
        }
        else
        {
            reqMaterialsUI?.Show(null);
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

    private void AddEffectText(string text, Color color)
    {
        if (effectContainer == null) return;
        var go = new GameObject("EffectText", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(effectContainer, false);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;
        le.preferredWidth = 400f;
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = 20f;
        tmp.color = color; tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget = false;
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