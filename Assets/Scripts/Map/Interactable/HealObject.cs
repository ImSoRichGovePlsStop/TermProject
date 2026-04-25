using UnityEngine;

public class HealObject : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[ E ]  Heal";
    public InteractInfo GetInteractInfo() => new InteractInfo
    {
        name        = "Healing Crystal",
        description = "Suffused with healing energy, restoring <color=#88FF88>50%</color> of your missing HP.",
        actionText  = "Consume",
        cost        = null
    };

    public void Interact(PlayerController playerController)
    {
        PlayerStats playerStats = playerController.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            float missing = playerStats.MaxHealth - playerStats.CurrentHealth;
            playerStats.Heal(missing * 0.5f);
        }

        Destroy(gameObject);
    }
}
