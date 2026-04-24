using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Single save slot card. Spawned/managed by SaveSlotScreenUI.</summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TextMeshProUGUI slotLabel;
    [SerializeField] private TextMeshProUGUI highestFloorValue;
    [SerializeField] private TextMeshProUGUI totalRunsValue;
    [SerializeField] private TextMeshProUGUI totalTimeValue;
    [SerializeField] private TextMeshProUGUI lastSavedValue;
    [SerializeField] private GameObject statsContainer;
    [SerializeField] private GameObject emptyLabel;
    [SerializeField] private Button deleteButton;

    private int _slot;
    private System.Action<int> _onSelect;

    public void Init(int slot, System.Action<int> onSelect)
    {
        _slot = slot;
        _onSelect = onSelect;

        if (slotLabel != null) slotLabel.text = $"SAVE {slot + 1}";
        GetComponent<Button>().onClick.AddListener(() => _onSelect?.Invoke(_slot));
        if (deleteButton != null)
            deleteButton.onClick.AddListener(OnDelete);

        Refresh();
    }

    public void Refresh()
    {
        var data = SaveManager.Instance?.ReadSlot(_slot);
        bool hasSave = data != null;

        if (statsContainer != null) statsContainer.SetActive(hasSave);
        if (emptyLabel != null) emptyLabel.SetActive(!hasSave);
        if (deleteButton != null) deleteButton.gameObject.SetActive(hasSave);

        if (!hasSave) return;

        if (highestFloorValue != null) highestFloorValue.text = FormatFloor(data.highestFloor);
        if (totalRunsValue != null) totalRunsValue.text = data.totalRuns.ToString();
        if (totalTimeValue != null) totalTimeValue.text = FormatTime(data.totalTime);
        if (lastSavedValue != null) lastSavedValue.text = string.IsNullOrEmpty(data.lastSaved) ? "—" : data.lastSaved;
    }

    private void OnDelete()
    {
        FindAnyObjectByType<MainMenuUI>()?.AskConfirmDelete(_slot);
    }

    public void ConfirmDelete()
    {
        SaveManager.Instance?.DeleteSlot(_slot);
        Refresh();
    }

    private static string FormatFloor(int floor)
    {
        if (floor <= 0) return "—";
        int f = (floor - 1) / 3 + 1;
        int r = (floor - 1) % 3 + 1;
        return $"{f}-{r}";
    }

    private static string FormatTime(float t)
    {
        int h = (int)(t / 3600);
        int m = (int)(t % 3600 / 60);
        int s = (int)(t % 60);
        return h > 0 ? $"{h}:{m:D2}:{s:D2}" : $"{m:D2}:{s:D2}";
    }
}