using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

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
        if (won)
        {
            var inv = InventoryManager.Instance;
            if (inv != null)
                foreach (var module in inv.BagGrid.GetAllModules())
                    if (module is MaterialInstance mat)
                        MaterialStorage.Instance.Add(mat.MaterialData, mat.StackCount);
        }

        var playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats != null)
        {
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

        gameObject.SetActive(false);
        SceneManager.LoadScene(1);
    }
}
