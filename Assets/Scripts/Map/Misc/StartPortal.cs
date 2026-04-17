using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        // Move player out of bounds so no room triggers fire before SpawnRoom repositions them
        var player = GameObject.FindWithTag("Player");
        if (player != null) player.transform.position = new Vector3(-200f, -75f, -200f);

        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;

        if (RunManager.Instance != null)
            RunManager.Instance.StartFloorTransition(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }

    string IInteractable.GetPromptText() => "[E] -> Start Run";
}
