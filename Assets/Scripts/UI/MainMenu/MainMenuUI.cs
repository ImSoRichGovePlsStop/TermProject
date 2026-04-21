using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button ExitButton;


    private void Awake()
    {
        startButton.onClick.AddListener(OnStart);
        ExitButton.onClick.AddListener(OnExit);
    }

    private void OnStart()
    {
        int nextScene = SceneManager.GetActiveScene().buildIndex + 1;
        SceneManager.LoadScene(nextScene);
    }

    private void OnExit()
    {
        Application.Quit();
    }
}