using UnityEngine;


public class GroundLoot : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        Debug.Log("Item interacted");
    }
}
