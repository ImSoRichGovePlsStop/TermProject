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
        {
            floorText.text = $"Floors Cleared: {run.CurrentFloor - 1}";
        }
    }

    private void OnContinue()
    {
        var inv = InventoryManager.Instance;
        if (inv != null)
        {
            var bagModules = inv.BagGrid.GetAllModules().ToList();
            foreach (var module in bagModules)
            {
                if (module is MaterialInstance mat)
                    MaterialStorage.Instance.Add(mat.MaterialData, mat.StackCount);
                inv.BagGrid.Remove(module);
            }

            foreach (var module in inv.WeaponGrid.GetAllModules().ToList())
                inv.WeaponGrid.Remove(module);

            inv.UpgradeWeaponGrid(1, 1);
        }

        var weaponEquip = FindFirstObjectByType<WeaponEquip>();
        weaponEquip?.Unequip();

        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.SetInvincible(false);
            playerStats.ResetModifiers();
            playerStats.ApplyDefault();
            playerStats.ClearAllShields();
        }

        // reset currency
        var wallet = FindFirstObjectByType<CurrencyManager>();
        wallet?.ResetCoins();

        // reset and destroy run manager
        if (RunManager.Instance != null)
        {
            RunManager.Instance.ResetRun();
            Destroy(RunManager.Instance.gameObject);
        }
        FindFirstObjectByType<CurrencyManager>()?.ResetCoins();
        FindFirstObjectByType<MinimapManager>()?.Reset();

        gameObject.SetActive(false);
        SceneManager.LoadScene(1);
    }
}