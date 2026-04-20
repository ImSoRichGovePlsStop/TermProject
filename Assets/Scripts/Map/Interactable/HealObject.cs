using UnityEngine;



public class HealObject : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[ E ]  Heal";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name = "Healing Crystal",
        description = "Suffused with healing energy, restoring <color=#88FF88>20%</color> of your maximum HP.",
        actionText = "Consume",
        cost = null
    };

    public void Interact(PlayerController playerController)
    {
        PlayerStats playerStats = playerController.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.HealPercent(0.2f);
        }

        Destroy(gameObject);
    }
}