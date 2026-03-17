using UnityEngine;

public class ShopStation : MonoBehaviour, IInteractable
{
    public string GetPromptText() => "[ E ]  Open Shop";

    public void Interact(PlayerController playerController)
    {
        Debug.Log("Opening Shop");

    }






}
