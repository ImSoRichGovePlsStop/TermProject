using UnityEngine;



public class HealObject : MonoBehaviour, IInteractable
{
    public void Interact(Interactor interactor)
    {
        GameObject player = GameObject.FindWithTag("Player");
        PlayerStats playerStatus =  player.GetComponent<PlayerStats>();

        playerStatus.Heal(50);
    }
}
