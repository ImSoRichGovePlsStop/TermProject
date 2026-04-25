using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndGameUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private Transform materialRow1;
    [SerializeField] private Transform materialRow2;
    [SerializeField] private Transform materialRow3;
    [SerializeField] private Button continueButton;

    [Header("Material Card Settings")]
    [SerializeField] private float cardSize = 100f;

    private bool won;

    private void Awake()
    {
        continueButton.onClick.AddListener(OnContinue);
    }

    public void Show(bool isWin)
    {
        won = isWin;
        gameObject.SetActive(true);
        titleText.text = isWin ? "Victory!" : "Defeated";

        var run = RunManager.Instance;
        if (run != null)
        {
            run.TotalRuns++;
            run.StopTimer();
            timerText.text = $"Total Time: {FormatTime(run.RunTime)}";
            killText.text = $"Total Kill: {run.TotalEnemyKilled}";
        }

        PopulateMaterials();
    }

    private string FormatTime(float t)
    {
        int h = (int)(t / 3600);
        int m = (int)(t % 3600 / 60);
        int s = (int)(t % 60);
        return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
    }

    private void PopulateMaterials()
    {
        foreach (Transform child in materialRow1) Destroy(child.gameObject);
        foreach (Transform child in materialRow2) Destroy(child.gameObject);
        foreach (Transform child in materialRow3) Destroy(child.gameObject);

        var inv = InventoryManager.Instance;
        if (inv == null) return;

        var materials = inv.BagGrid.GetAllModules()
            .OfType<MaterialInstance>()
            .GroupBy(m => m.MaterialData)
            .Select(g => (data: g.Key, count: g.Sum(m => m.StackCount)))
            .ToList();

        var rows = new[] { materialRow1, materialRow2, materialRow3 };
        for (int i = 0; i < materials.Count; i++)
        {
            int rowIdx = i / 6;
            if (rowIdx >= rows.Length) break;
            SpawnCard(materials[i].data, materials[i].count, rows[rowIdx]);
        }

        materialRow2.gameObject.SetActive(materials.Count > 6);
        materialRow3.gameObject.SetActive(materials.Count > 12);
    }

    private void SpawnCard(MaterialData data, int count, Transform parent)
    {
        var card = new GameObject("MatCard", typeof(RectTransform));
        card.layer = LayerMask.NameToLayer("UI");
        card.transform.SetParent(parent, false);

        var rt = card.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cardSize, cardSize);

        // Invisible raycast bg
        var bg = card.AddComponent<Image>();
        bg.color = Color.clear;
        bg.raycastTarget = false;

        Color rarityCol = SpriteOutlineUtility.RarityColor(data.rarity);

        // Grey background
        MakeLayer("Background", card.transform, Vector2.zero, Vector2.one, 0, 0)
            .AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.20f, 1f);

        // Rarity outline
        MakeLayer("RarityOutline", card.transform, Vector2.zero, Vector2.one, 2, -2)
            .AddComponent<Image>().color = new Color(rarityCol.r, rarityCol.g, rarityCol.b, 0.7f);

        // Inner dark
        MakeLayer("Inner", card.transform, Vector2.zero, Vector2.one, 4, -4)
            .AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.14f, 1f);

        // Icon
        if (data.icon != null)
        {
            var bound = data.GetBoundingSize();
            int maxSide = Mathf.Max(bound.x, bound.y);
            float pct = maxSide >= 3 ? 1f : maxSide == 2 ? 0.8f : 0.6f;
            float half = (1f - pct) / 2f;

            var wrap = new GameObject("IconWrap", typeof(RectTransform));
            wrap.layer = LayerMask.NameToLayer("UI");
            wrap.transform.SetParent(card.transform, false);
            var wrapRt = wrap.GetComponent<RectTransform>();
            wrapRt.anchorMin = new Vector2(half, half);
            wrapRt.anchorMax = new Vector2(1f - half, 1f - half);
            wrapRt.offsetMin = new Vector2(4f, 4f);
            wrapRt.offsetMax = new Vector2(-4f, -4f);

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(RawImage));
            icon.layer = LayerMask.NameToLayer("UI");
            icon.transform.SetParent(wrap.transform, false);
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero; iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero; iconRt.offsetMax = Vector2.zero;
            var raw = icon.GetComponent<RawImage>();
            raw.texture = data.icon.texture;
            raw.color = Color.white; raw.raycastTarget = false;
            var arf = icon.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            arf.aspectRatio = (float)data.icon.texture.width / data.icon.texture.height;
        }

        // Count text — bottom right
        var countGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        countGo.layer = LayerMask.NameToLayer("UI");
        countGo.transform.SetParent(card.transform, false);
        var countRt = countGo.GetComponent<RectTransform>();
        countRt.anchorMin = Vector2.zero; countRt.anchorMax = Vector2.one;
        countRt.offsetMin = new Vector2(6f, 6f); countRt.offsetMax = new Vector2(-6f, -6f);
        var countTmp = countGo.GetComponent<TextMeshProUGUI>();
        countTmp.text = count.ToString();
        countTmp.fontSize = 20f;
        countTmp.color = Color.white;
        countTmp.alignment = TextAlignmentOptions.BottomRight;
        countTmp.raycastTarget = false;
    }

    private static GameObject MakeLayer(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float inset, float outset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(outset, outset);
        return go;
    }

    private void OnContinue()
    {
        var transitioner = FindAnyObjectByType<SceneTransitioner>();
        if (transitioner != null)
            transitioner.TransitionToSceneWithCleanup(1, OnCleanup);
        else
        {
            OnCleanup();
            SceneManager.LoadScene(1);
        }
    }

    private void OnCleanup()
    {
        // 1. Camera
        CameraController.Instance?.RestoreCamera();
        // 2. Close all open UI panels
        var ui = UIManager.Instance;
        if (ui != null)
        {
            if (ui.IsMergeOpen) ui.CloseMerge();
            if (ui.IsShopOpen) ui.CloseShop();
            if (ui.IsInventoryOpen) ui.ToggleInventory();
            ui.isInBattle = false;
        }
        ModuleTooltipUI.Instance?.Hide();
        DiscardGridUI.Instance?.ForceHide();
        // 3. Clear inventory
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            foreach (var module in inv.BagGrid.GetAllModules().ToList())
            {
                if (module is MaterialInstance mat)
                    MaterialStorage.Instance?.Add(mat.MaterialData, mat.StackCount);
                inv.DeleteModule(module);
            }
            foreach (var module in inv.WeaponGrid.GetAllModules().ToList())
                inv.DeleteModule(module);
        }
        // 4. Reset player state
        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.SetInvincible(false);
            playerStats.ResetModifiers();
            playerStats.ClearAllShields();
        }
        FindFirstObjectByType<WeaponEquip>()?.ResetToCurrentWeapon();
        // 5. Reset persistent managers
        FindFirstObjectByType<CurrencyManager>()?.ResetCoins();
        FindFirstObjectByType<MinimapManager>()?.Reset();
        // 6. Reset panel state
        UIManager.Instance?.ResetPanelState();
        // 7. Save before reset
        SaveManager.Instance?.Save();
        // 8. Reset run
        RunManager.Instance?.ResetRun();
        gameObject.SetActive(false);
    }
}