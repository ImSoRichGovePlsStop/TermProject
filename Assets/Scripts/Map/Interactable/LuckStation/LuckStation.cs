using UnityEngine;

public class LuckStation : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[E]  Luck Station";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Merchant",
        description = "Upgrade your economic supplies.",
        actionText  = "Talk",
        cost        = null
    };
    public void Interact(PlayerController playerController)
    {
        var ui = UIManager.Instance;
        if (ui == null) return;
        if (ui.IsLuckStationOpen) ui.CloseLuckStation();
        else ui.OpenLuckStation();
    }
}
