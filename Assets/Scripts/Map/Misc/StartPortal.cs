using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextScene < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(nextScene);
        else
            Debug.LogWarning("[StartPortal] No next scene in build settings.");
    }
}