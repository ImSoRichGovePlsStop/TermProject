using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPortal : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        SceneManager.LoadScene(2);
    }
}