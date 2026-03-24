using UnityEngine;
using UnityEngine.SceneManagement;

public class EndPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {

    }

    string IInteractable.GetPromptText()
    {
        return "[E] -> Complete Run";
    }
}
