using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;

    [Header("New Game Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button     confirmYesButton;
    [SerializeField] private Button     confirmNoButton;

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnNewGame);
        continueButton.onClick.AddListener(OnContinue);
        exitButton.onClick.AddListener(OnExit);

        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(OnConfirmNo);

        confirmPanel.SetActive(false);
    }

    private void Start()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();
        continueButton.interactable = hasSave;
    }


    private void OnNewGame()
    {
        bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();
        if (hasSave)
            confirmPanel.SetActive(true); // ask before overwriting
        else
            StartNewGame();
    }

    private void OnContinue()
    {
        FindAnyObjectByType<SceneTransitioner>().TransitionToScene(1);
    }


    private void OnExit()
    {
        Application.Quit();
    }



    private void OnConfirmYes()
    {
        confirmPanel.SetActive(false);
        SaveManager.Instance?.DeleteSave();
        StartNewGame();
    }

    private void OnConfirmNo()
    {
        confirmPanel.SetActive(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void StartNewGame()
    {
        FindAnyObjectByType<SceneTransitioner>().TransitionToScene(1);
    }
}
