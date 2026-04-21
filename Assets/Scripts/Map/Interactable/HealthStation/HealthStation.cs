using UnityEngine;

public class HealthStation : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[E]  Health Station";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Medic",
        description = "Upgrade your vitality.",
        actionText  = "Talk",
        cost        = null
    };
    public void Interact(PlayerController playerController)
    {
        var ui = UIManager.Instance;
        if (ui == null) return;
        if (ui.IsHealthStationOpen) ui.CloseHealthStation();
        else ui.OpenHealthStation();
    }
}
