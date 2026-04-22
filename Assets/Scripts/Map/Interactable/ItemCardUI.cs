using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum CardMode { Reward, Shop, Upgrade }

public class ItemCardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private RectTransform shapePreviewRoot;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private CardMode _mode;
    private ModuleInstance _inst;

    // Reward
    private LootRewardUI _rewardUI;

    // Shop
    private ShopUI _shopUI;
    private int _entryIndex;
    private bool _purchased = false;
    private int _price;

    // Upgrade
    private UpgradeStationUI _upgradeUI;

    // --- Init ---

    public void InitReward(ModuleInstance inst, LootRewardUI rewardUI)
    {
        _mode = CardMode.Reward;
        _inst = inst;
        _rewardUI = rewardUI;

        if (priceText != null) priceText.gameObject.SetActive(false);
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);

        SetNameText(inst);
        BuildShapePreview(inst.Data, inst.Rarity);
    }

    public void InitUpgrade(ModuleInstance inst, UpgradeStationUI upgradeUI)
    {
        _mode = CardMode.Upgrade;
        _inst = inst;
        _upgradeUI = upgradeUI;

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = $"Lv.{inst.Level} <voffset=0.05em>\u2192</voffset> Lv.{inst.Level + 1}";
        }
        if (buyButton != null) buyButton.gameObject.SetActive(false);
        if (statusText != null) statusText.gameObject.SetActive(false);

        SetNameText(inst);
        BuildShapePreview(inst.Data, inst.Rarity);
    }

    public void InitShop(TestModuleEntry entry, ShopUI shopUI, int entryIndex)
    {
        _mode = CardMode.Shop;
        _inst = new ModuleInstance(entry.data, entry.rarity, entry.level);
        _shopUI = shopUI;
        _entryIndex = entryIndex;
        int baseCost = entry.data.cost[(int)entry.rarity];
        float discount = RunManager.Instance != null ? RunManager.Instance.EffectiveShopDiscount : 0f;
        _price = Mathf.RoundToInt(baseCost * (1f - discount));

        if (statusText != null) statusText.gameObject.SetActive(false);

        SetNameText(_inst);

        if (priceText != null)
        {
            priceText.gameObject.SetActive(true);
            priceText.text = discount > 0f
                ? $"<color=#666666><s>{baseCost}  <sprite=0></s></color>  {_price}  <sprite=0>"
                : $"{_price}  <sprite=0>";
        }

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(true);
            buyButton.onClick.AddListener(OnBuyClicked);
        }

        BuildShapePreview(entry.data, entry.rarity);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged += OnCoinsChanged;

        RefreshAffordability();
    }

    private void SetNameText(ModuleInstance inst)
    {
        if (nameText == null) return;
        nameText.text = inst.Data.moduleName;
        nameText.color = SpriteOutlineUtility.RarityColor(inst.Rarity);
    }

    private void OnDestroy()
    {
        if (_mode == CardMode.Shop && CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged;
    }

    // --- Reward ---

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left) return;
        ModuleTooltipUI.Instance?.Hide();

        if (_mode == CardMode.Reward)
            _rewardUI.OnOptionSelected(_inst);
        else if (_mode == CardMode.Upgrade)
            _upgradeUI.OnOptionSelected(_inst);
    }

    // --- Shop ---

    private void OnCoinsChanged(int _) => RefreshAffordability();

    private void RefreshAffordability()
    {
        if (_purchased || buyButton == null) return;
        bool canAfford = CurrencyManager.Instance != null && CurrencyManager.Instance.Coins >= _price;
        buyButton.interactable = canAfford;
    }

    private void OnBuyClicked()
    {
        if (_purchased) return;
        if (CurrencyManager.Instance == null || CurrencyManager.Instance.Coins < _price) return;

        var inventoryUI = FindFirstObjectByType<InventoryUI>(FindObjectsInactive.Include);
        if (inventoryUI == null) return;

        var spawned = inventoryUI.SpawnModule(_inst.Data, _inst.Rarity, _inst.Level);
        if (spawned == null)
        {
            ShowStatus("Inventory full!");
            return;
        }

        CurrencyManager.Instance.TrySpend(_price);
        MarkPurchased();
    }

    private void ShowStatus(string msg)
    {
        if (statusText == null) return;
        statusText.gameObject.SetActive(true);
        statusText.text = msg;
    }

    public void MarkPurchased()
    {
        if (_purchased) return;
        _purchased = true;
        _shopUI.RegisterSold(_entryIndex);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnCoinsChanged -= OnCoinsChanged;

        if (buyButton != null)
        {
            buyButton.interactable = false;
            var btnText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "Sold";
        }

        if (nameText != null) nameText.color = Color.gray;
        if (priceText != null) priceText.color = Color.gray;

        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0.5f;
    }

    // --- Tooltip ---

    public void OnPointerEnter(PointerEventData e)
    {
        if (_mode == CardMode.Shop && _purchased) return;
        if (_mode == CardMode.Upgrade)
            ModuleTooltipUI.Instance?.ShowNextLevelAtRect(_inst, GetComponent<RectTransform>());
        else
            ModuleTooltipUI.Instance?.ShowAtRect(_inst, GetComponent<RectTransform>());
    }

    public void OnPointerExit(PointerEventData e)
    {
        ModuleTooltipUI.Instance?.Hide();
    }

    // --- Shape Preview ---

    private void BuildShapePreview(ModuleData data, Rarity rarity)
    {
        foreach (Transform child in shapePreviewRoot)
            Destroy(child.gameObject);

        var shapeCells = data.GetShapeCells();
        var bound = data.GetBoundingSize();

        float sp = 2f;
        float borderSize = 2f;
        float scaleFactor = 0.9f;
        float maxExpectedUnits = 4f;

        var le = shapePreviewRoot.GetComponent<LayoutElement>();
        float previewSize = (le != null && le.preferredWidth > 0) ? le.preferredWidth : 100f;
        float unitW = (previewSize + sp) / maxExpectedUnits - sp;
        float cs = unitW * scaleFactor;

        float shapeW = bound.x * (cs + sp) - sp;
        float shapeH = bound.y * (cs + sp) - sp;
        shapePreviewRoot.sizeDelta = new Vector2(previewSize, shapeH);

        float xOffset = (previewSize - shapeW) / 2f;
        Color borderColor = SpriteOutlineUtility.RarityColor(rarity);

        foreach (var cell in shapeCells)
        {
            var borderGo = new GameObject($"border_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
            var borderRt = borderGo.GetComponent<RectTransform>();
            borderRt.SetParent(shapePreviewRoot, false);
            borderRt.pivot = new Vector2(0f, 1f);
            borderRt.anchorMin = new Vector2(0f, 1f);
            borderRt.anchorMax = new Vector2(0f, 1f);

            bool bHasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool bHasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool bHasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool bHasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float bExtraRight = bHasRight ? borderSize + sp : 0f;
            float bExtraLeft = bHasLeft ? borderSize + sp : 0f;
            float bExtraBottom = bHasBottom ? borderSize + sp : 0f;
            float bExtraTop = bHasTop ? borderSize + sp : 0f;

            borderRt.sizeDelta = new Vector2(cs + bExtraLeft + bExtraRight, cs + bExtraTop + bExtraBottom);
            borderRt.anchoredPosition = new Vector2(cell.x * (cs + sp) - bExtraLeft + xOffset, -cell.y * (cs + sp) + bExtraTop);
            borderGo.GetComponent<Image>().color = borderColor;
            borderGo.GetComponent<Image>().raycastTarget = false;
        }

        foreach (var cell in shapeCells)
        {
            var go = new GameObject($"cell_{cell.x}_{cell.y}", typeof(RectTransform), typeof(Image));
            var cellRt = go.GetComponent<RectTransform>();
            cellRt.SetParent(shapePreviewRoot, false);
            cellRt.pivot = new Vector2(0f, 1f);
            cellRt.anchorMin = new Vector2(0f, 1f);
            cellRt.anchorMax = new Vector2(0f, 1f);

            bool hasRight = shapeCells.Contains(new Vector2Int(cell.x + 1, cell.y));
            bool hasLeft = shapeCells.Contains(new Vector2Int(cell.x - 1, cell.y));
            bool hasBottom = shapeCells.Contains(new Vector2Int(cell.x, cell.y + 1));
            bool hasTop = shapeCells.Contains(new Vector2Int(cell.x, cell.y - 1));

            float extraRight = hasRight ? borderSize + sp : 0f;
            float extraLeft = hasLeft ? borderSize + sp : 0f;
            float extraBottom = hasBottom ? borderSize + sp : 0f;
            float extraTop = hasTop ? borderSize + sp : 0f;

            cellRt.sizeDelta = new Vector2(cs - borderSize * 2f + extraLeft + extraRight, cs - borderSize * 2f + extraTop + extraBottom);
            cellRt.anchoredPosition = new Vector2(cell.x * (cs + sp) + borderSize - extraLeft + xOffset, -cell.y * (cs + sp) - borderSize + extraTop);

            var cellImg = go.GetComponent<Image>();
            if (data.icon != null) cellImg.sprite = data.icon;
            cellImg.color = data.moduleColor;
            cellImg.raycastTarget = false;
        }
    }
}