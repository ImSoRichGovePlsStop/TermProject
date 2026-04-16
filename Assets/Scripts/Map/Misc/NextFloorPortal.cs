using UnityEngine;
using UnityEngine.SceneManagement;

public class NextFloorPortal : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController playerController)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogWarning("[NextFloorPortal] Player not found!"); return; }

        player.transform.position = new Vector3(-200f, -75f, -200f);

        int nextScene = SceneManager.GetActiveScene().buildIndex;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowFloorTransition(nextScene);
        else
            SceneManager.LoadScene(nextScene);
    }

    string IInteractable.GetPromptText() => "[E] -> Go to next floor";
}