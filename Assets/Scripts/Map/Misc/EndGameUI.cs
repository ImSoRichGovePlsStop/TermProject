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
    [SerializeField] private TextMeshProUGUI floorText;
    [SerializeField] private Button continueButton;

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
            floorText.text = $"Floors Cleared: {run.CurrentFloor - 1}";
    }

    private void OnContinue()
    {
        // ── 1. Camera ───────────────────────────────────────────────────────
        CameraController.Instance?.RestoreCamera();

        // ── 2. Close all open UI panels FIRST so they can return items ──────
        //      (MergeUI.OnDisable returns merge input/output back to the bag)
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

        // ── 3. Clear inventory — bag first (materials → storage), then weapon ─
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

        // ── 4. Reset player state ───────────────────────────────────────────
        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.SetInvincible(false);
            playerStats.ResetModifiers();
            playerStats.ClearAllShields();
        }

        // Re-equip starting weapon — calls ApplyWeapon internally which properly
        // resets stats, animator override, passives, and weapon grid size.
        FindFirstObjectByType<WeaponEquip>()?.ResetToCurrentWeapon();

        // ── 5. Reset persistent managers ────────────────────────────────────
        FindFirstObjectByType<CurrencyManager>()?.ResetCoins();
        FindFirstObjectByType<MinimapManager>()?.Reset();
        RunManager.Instance?.ResetRun();   // also resets HealthStation & LuckStation internally

        // ── 6. Force-clear any stale UI panel state before scene load ────────
        UIManager.Instance?.ResetPanelState();

        // ── 7. Go back to hub scene ─────────────────────────────────────────
        gameObject.SetActive(false);
        SceneManager.LoadScene(1);
    }
}
