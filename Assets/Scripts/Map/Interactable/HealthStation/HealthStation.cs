using UnityEngine;

public class HealthStation : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[E]  Health Station";
    public void Interact(PlayerController playerController)
    {
        var ui = UIManager.Instance;
        if (ui == null) return;
        if (ui.IsHealthStationOpen) ui.CloseHealthStation();
        else ui.OpenHealthStation();
    }
}
