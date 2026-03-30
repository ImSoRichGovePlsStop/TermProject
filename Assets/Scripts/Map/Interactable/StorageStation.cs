using UnityEngine;

public class StorageStation : MonoBehaviour, IInteractable
{
    private UIManager uiManager;

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (uiManager == null) return;
        if (uiManager.IsStorageOpen)
            uiManager.CloseStorage();
        else
            uiManager.OpenStorage();
    }

    public string GetPromptText() => "[ E ]  Open Storage";
}
