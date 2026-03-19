using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModuleTooltipUI : MonoBehaviour
{
    public static ModuleTooltipUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityLevelText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;

    private GridUI currentBuffGridUI;
    private GridUI weaponGridUIRef;
    private GridUI bagGridUIRef;

    private static Color RarityColor(Rarity r)
    {
        switch (r)
        {
            case Rarity.Common: return new Color(0.75f, 0.75f, 0.75f);
            case Rarity.Uncommon: return new Color(0.30f, 0.80f, 0.30f);
            case Rarity.Rare: return new Color(0.20f, 0.50f, 1.00f);
            case Rarity.Epic: return new Color(0.65f, 0.25f, 0.90f);
            case Rarity.GOD: return new Color(1.00f, 0.75f, 0.10f);
            default: return Color.white;
        }
    }

    private void Awake()
    {
        Instance = this;

        // Style NameText
        nameText.fontSize = 22f;
        nameText.fontStyle = FontStyles.Bold;

        // Style RarityLevelText
        rarityLevelText.fontSize = 14f;
        rarityLevelText.color = new Color(0.7f, 0.7f, 0.7f);

        // Create divider
        var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        dividerGo.transform.SetParent(transform, false);
        dividerGo.transform.SetSiblingIndex(rarityLevelText.transform.GetSiblingIndex() + 1);

        var dividerRt = dividerGo.GetComponent<RectTransform>();
        dividerRt.sizeDelta = new Vector2(0f, 1f);

        var dividerImg = dividerGo.GetComponent<Image>();
        dividerImg.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        dividerImg.raycastTarget = false;

        var dividerLayout = dividerGo.AddComponent<LayoutElement>();
        dividerLayout.minHeight = 1f;
        dividerLayout.preferredHeight = 1f;
        dividerLayout.flexibleWidth = 1f;

        // Style DescriptionText
        descriptionText.fontSize = 15f;
        descriptionText.color = Color.white;

        // Style CostText
        costText.fontSize = 15f;
        costText.fontStyle = FontStyles.Bold;
        costText.color = Color.yellow;

        Hide();
    }

    public void Show(ModuleInstance inst, GridUI weaponGridUI, GridUI bagGridUI)
    {
        weaponGridUIRef = weaponGridUI;
        bagGridUIRef = bagGridUI;

        nameText.text = inst.Data.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        rarityLevelText.text = $"{inst.Rarity}  Lv.{inst.Level}";
        descriptionText.text = inst.Data.moduleEffect != null
            ? inst.Data.moduleEffect.GetDescription(inst.Rarity, inst.Level, inst.RuntimeState)
            : "";
        costText.text = $"Cost : {(int)inst.GetCostAtLevel()}";
        gameObject.SetActive(true);

        // Highlight buff cells
        if (inst.Data.isBuffAdjacent && inst.CurrentGrid != null)
        {
            var grid = inst.CurrentGrid == weaponGridUI.Data ? weaponGridUI : bagGridUI;
            currentBuffGridUI = grid;
            grid.HighlightBuffCells(inst, inst.Data.moduleColor);
        }
    }

    public void Show(MaterialInstance inst)
    {
        nameText.text  = inst.MaterialData.moduleName;
        nameText.color = RarityColor(inst.Rarity);
        rarityLevelText.text = $"{inst.Rarity}  {inst.StackCount}/{inst.MaxStack}";
        descriptionText.text = inst.Cost > 0
            ? $"Total cost: {inst.Cost * inst.StackCount}g"
            : "";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        if (currentBuffGridUI != null)
        {
            currentBuffGridUI.ClearBuffHighlights();
            currentBuffGridUI = null;
        }
    }
}