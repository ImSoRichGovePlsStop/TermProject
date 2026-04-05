using UnityEngine;
using UnityEngine.SceneManagement;

public class EndPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        if (RunManager.Instance != null)
            RunManager.Instance.IsWin = true;

        var panel = FindFirstObjectByType<EndGameUI>(FindObjectsInactive.Include);
        Debug.Log("interacted");
        panel?.Show(isWin: true);
    }

    string IInteractable.GetPromptText()
    {
        return "[E] -> Complete Run";
    }
}