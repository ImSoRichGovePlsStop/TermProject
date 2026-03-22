using TMPro;
using UnityEngine;

public class ShopTooltipUI : MonoBehaviour
{
    public static ShopTooltipUI Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI rarityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI priceText;

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
        Hide();
    }

    public void ShowModule(ModuleInstance inst, int price)
    {
        nameText.text = inst.Data.moduleName;
        nameText.color = RarityColor(inst.Rarity);

        rarityText.text = $"{inst.Rarity}  Lv.{inst.Level}";

        descriptionText.text = inst.Data.moduleEffect != null
            ? inst.Data.moduleEffect.GetDescription(inst.Rarity, inst.Level, inst.RuntimeState)
            : "";

        priceText.text = $"{price}g";

        gameObject.SetActive(true);
    }

    public void ShowMaterial(MaterialInstance inst, int price)
    {
        nameText.text = inst.MaterialData.moduleName;
        nameText.color = RarityColor(inst.Rarity);

        rarityText.text = $"{inst.Rarity}";

        descriptionText.text = $"Stack: {inst.StackCount}/{inst.MaxStack}";

        priceText.text = $"{price}g";

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}