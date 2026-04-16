using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPortal : MonoBehaviour,IInteractable
{

    public void Interact(PlayerController playerController)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) { Debug.LogWarning("[PlayerSpawner] Player not found!"); return; }
        player.transform.position = new Vector3(-200f, -75f, -200f);
        SceneManager.LoadScene(2);
    }

    string IInteractable.GetPromptText()
    {
        return "[E] -> Start";
    }
}