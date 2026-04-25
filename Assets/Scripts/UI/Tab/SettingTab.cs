using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingTab : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button primaryButton;
    [SerializeField] private Button secondaryButton;

    [Header("Button Labels")]
    [SerializeField] private TextMeshProUGUI primaryLabel;
    [SerializeField] private TextMeshProUGUI secondaryLabel;

    [Header("Exit Confirm Popup (Run only)")]
    [SerializeField] private GameObject exitConfirmPanel;
    [SerializeField] private Button exitConfirmYes;
    [SerializeField] private Button exitConfirmNo;

    [Header("Fade")]
    [SerializeField] private CanvasGroup fadePanel;
    [SerializeField] private float fadeDuration = 0.5f;

    private bool _listenersAdded = false;

    private void Awake()
    {
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
        if (exitConfirmYes != null) exitConfirmYes.onClick.AddListener(() => StartCoroutine(FadeAndQuit()));
        if (exitConfirmNo != null) exitConfirmNo.onClick.AddListener(() => exitConfirmPanel?.SetActive(false));
    }

    private void OnEnable()
    {
        bool isInRun = SceneManager.GetActiveScene().buildIndex == 2;
        Debug.Log($"[SettingTab] Scene: {SceneManager.GetActiveScene().name} ({SceneManager.GetActiveScene().buildIndex}), isInRun: {isInRun}");

        primaryButton.onClick.RemoveAllListeners();
        secondaryButton.onClick.RemoveAllListeners();

        if (isInRun)
        {
            if (primaryLabel) primaryLabel.text = "Return to Hub";
            if (secondaryLabel) secondaryLabel.text = "Exit";
            primaryButton.onClick.AddListener(OnReturnToHub);
            secondaryButton.onClick.AddListener(OnExitRun);
        }
        else
        {
            if (primaryLabel) primaryLabel.text = "Return to Main Menu";
            if (secondaryLabel) secondaryLabel.text = "Exit";
            primaryButton.onClick.AddListener(OnReturnToMainMenu);
            secondaryButton.onClick.AddListener(OnExitHub);
        }
    }



    private void OnReturnToMainMenu()
    {
        SaveManager.Instance?.Save();
        FindAnyObjectByType<SceneTransitioner>()?.TransitionToSceneWithCleanup(0, () =>
        {
            if (GameManager.Instance != null)
                Destroy(GameManager.Instance.gameObject);
            var matStorage = FindAnyObjectByType<MaterialStorage>();
            if (matStorage != null) Destroy(matStorage.gameObject);
        });
    }

    private void OnExitHub()
    {
        SaveManager.Instance?.Save();
        StartCoroutine(FadeAndQuit());
    }



    private void OnReturnToHub()
    {
        var endGame = FindFirstObjectByType<EndGameUI>(FindObjectsInactive.Include);
        endGame?.Show(isWin: false);
    }

    private void OnExitRun()
    {
        if (exitConfirmPanel != null)
            exitConfirmPanel.SetActive(true);
        else
            StartCoroutine(FadeAndQuit());
    }



    private IEnumerator FadeAndQuit()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                fadePanel.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }
        }
        Application.Quit();
    }
}