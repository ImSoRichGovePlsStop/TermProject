using UnityEngine;
using UnityEngine.SceneManagement;

public class NextFloorPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        
        var player = GameObject.FindWithTag("Player");
        if (player != null) player.transform.position = new Vector3(-200f, -75f, -200f);

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (RunManager.Instance != null)
            RunManager.Instance.StartFloorTransition(sceneIndex);
        else
            SceneManager.LoadScene(sceneIndex);
    }

    string IInteractable.GetPromptText() => "[E] -> Go to next floor";
}