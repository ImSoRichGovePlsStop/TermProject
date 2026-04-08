using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingTab : MonoBehaviour
{

    [SerializeField] private Button HubButton;
    [SerializeField] private Button ExitButton;


    private void Awake()
    {
        HubButton.onClick.AddListener(OnHub);
        ExitButton.onClick.AddListener(OnExit);
    }


    private void OnHub()
    {
        var panel = FindFirstObjectByType<EndGameUI>(FindObjectsInactive.Include);
        panel?.Show(isWin: false);
    }

    private void OnExit()
    {
        Application.Quit();
    }
}
