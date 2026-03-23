using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    private UIManager _uiManager;


    public string GetPromptText() => "[ E ]  Upgrade Your Modules";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (_uiManager == null) { Debug.LogError("[UpgradeStation] UIManager not found!"); return; }
        _uiManager.OpenUpgrade(this);

    }
}