using UnityEngine;


public class GroundLoot : MonoBehaviour, IInteractable
{
    public void Interact(Interactor interactor)
    {
        Debug.Log("Item interacted");
    }
}
