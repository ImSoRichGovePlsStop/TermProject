using UnityEngine;



public class HealObject : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[ E ]  Heal";

    public void Interact(PlayerController playerController)
    {
        PlayerStats playerStats = playerController.GetComponent<PlayerStats>();

        if (playerStats != null)
        {
            playerStats.Heal(50);
        }

        Destroy(gameObject);
    }
}
