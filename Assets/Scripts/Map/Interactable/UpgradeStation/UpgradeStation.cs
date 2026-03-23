using UnityEngine;

public class UpgradeStation : MonoBehaviour, IInteractable
{
    private UIManager _uiManager;
    private bool _used = false;

    public string GetPromptText() => "[ E ]  Upgrade Your Modules";

    private void Start()
    {
        _uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (_used) return;
        if (_uiManager == null) { Debug.LogError("[UpgradeStation] UIManager not found!"); return; }
        _used = true;
        _uiManager.OpenUpgrade(this);
    }
}