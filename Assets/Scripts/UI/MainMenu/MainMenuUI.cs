using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [Header("Main Buttons")]
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button exitButton;

    [Header("Save Slot Screen")]
    [SerializeField] private SaveSlotScreenUI saveSlotScreen;
    [SerializeField] private SaveSlotUI[] slots = new SaveSlotUI[4];

    [Header("Delete Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;
    [SerializeField] private int confirmSlot = -1;

    private void Awake()
    {
        newGameButton.onClick.AddListener(OnPlay);
        exitButton.onClick.AddListener(OnExit);
        confirmYesButton.onClick.AddListener(OnConfirmYes);
        confirmNoButton.onClick.AddListener(OnConfirmNo);
        confirmPanel.SetActive(false);
    }

    // ── Play → show slot screen ───────────────────────────────────────────────

    private void OnPlay()
    {
        saveSlotScreen?.Show();
    }

    private void OnExit()
    {
        Application.Quit();
    }

    // ── Confirm overwrite (called by SaveSlotScreenUI delete flow if needed) ──

    private SaveSlotUI _pendingDeleteSlot;

    public void AskConfirmDelete(int slot)
    {
        _pendingDeleteSlot = slots[slot];
        confirmSlot = slot;
        confirmPanel.SetActive(true);
    }

    private void OnConfirmYes()
    {
        confirmPanel.SetActive(false);
        _pendingDeleteSlot?.ConfirmDelete();
        _pendingDeleteSlot = null;
        confirmSlot = -1;
    }

    private void OnConfirmNo()
    {
        confirmPanel.SetActive(false);
        confirmSlot = -1;
    }
}