using UnityEngine;
using TMPro;

public class PassiveScreenUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private PassiveTreeUI[] treeUIs; // 3 elements

    private WeaponPassiveManager manager;
    private WeaponPassiveData currentData;
    private bool isSetup = false;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (manager == null)
            manager = FindFirstObjectByType<WeaponPassiveManager>();
        panel.SetActive(false);
    }

    public void Open(WeaponPassiveData data)
    {
        IsOpen = true;
        panel.SetActive(true);

        if (!isSetup || currentData != data)
        {
            currentData = data;
            isSetup = false;
            ClearTrees();
        }

        if (!isSetup)
        {
            Setup(data);
            isSetup = true;
        }

        RefreshAll();
        RefreshPoints();
    }

    public void Close()
    {
        IsOpen = false;
        panel.SetActive(false);
        PassiveTooltipUI.Instance?.Hide();
    }

    private void ClearTrees()
    {
        foreach (var treeUI in treeUIs)
            treeUI.Clear();
    }

    private void Setup(WeaponPassiveData data)
    {
        for (int i = 0; i < treeUIs.Length; i++)
            treeUIs[i].Setup(data.trees[i], manager, this, data);
    }

    public void RefreshPoints()
    {
        pointsText.text = $"Points: {manager.GetState(currentData).availablePoints}";
    }

    public void OnResetHeld()
    {
        manager.ResetPassive(currentData);
        RefreshAll();
        RefreshPoints();
    }

    public void RefreshAll()
    {
        foreach (var treeUI in treeUIs)
            treeUI.RefreshAll();
    }
}