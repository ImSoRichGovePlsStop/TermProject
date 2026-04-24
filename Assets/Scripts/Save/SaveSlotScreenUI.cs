using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen save slot selector shown when player clicks Play on main menu.
/// Holds 4 SaveSlotUI cards side by side.
/// </summary>
public class SaveSlotScreenUI : MonoBehaviour
{
    [Header("Slots")]
    [SerializeField] private SaveSlotUI[] slots = new SaveSlotUI[4];

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    [Header("New Game Confirm")]
    [SerializeField] private GameObject newGameConfirmPanel;
    [SerializeField] private Button newGameConfirmYes;
    [SerializeField] private Button newGameConfirmNo;

    private int _pendingSlot = -1;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (newGameConfirmYes != null)
            newGameConfirmYes.onClick.AddListener(OnNewGameConfirmYes);
        if (newGameConfirmNo != null)
            newGameConfirmNo.onClick.AddListener(() => newGameConfirmPanel?.SetActive(false));
        if (newGameConfirmPanel != null)
            newGameConfirmPanel.SetActive(false);

        for (int i = 0; i < slots.Length; i++)
        {
            int idx = i;
            slots[i]?.Init(idx, OnSlotSelected);
        }

        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        foreach (var slot in slots)
            slot?.Refresh();
    }

    private void OnSlotSelected(int slot)
    {
        if (!SaveManager.Instance.HasSave(slot))
        {
            bool anySlotHasSave = false;
            for (int i = 0; i < 4; i++)
                if (SaveManager.Instance.HasSave(i)) { anySlotHasSave = true; break; }

            if (!anySlotHasSave)
            {
                StartSlot(slot);
                return;
            }

            _pendingSlot = slot;
            newGameConfirmPanel?.SetActive(true);
        }
        else
        {
            StartSlot(slot);
        }
    }

    private void OnNewGameConfirmYes()
    {
        newGameConfirmPanel?.SetActive(false);
        if (_pendingSlot >= 0) StartSlot(_pendingSlot);
        _pendingSlot = -1;
    }

    private void StartSlot(int slot)
    {
        SaveManager.Instance?.SelectSlot(slot);
        FindAnyObjectByType<SceneTransitioner>()?.TransitionToScene(1);
    }
}