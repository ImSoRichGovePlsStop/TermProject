using UnityEngine;



public class HealObject : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[ E ]  Heal";

    public void Interact(PlayerController playerController)
    {
        GameObject player = GameObject.FindWithTag("Player");
        PlayerStats playerStatus =  player.GetComponent<PlayerStats>();

        playerStatus.Heal(50);
    }
}
