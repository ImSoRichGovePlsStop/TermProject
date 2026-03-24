using UnityEngine;
using UnityEngine.SceneManagement;

public class NextFloorPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 0;
        SceneManager.LoadScene(nextScene);
    }

    string IInteractable.GetPromptText()
    {
        return "[E] -> Go to next floor";
    }
}