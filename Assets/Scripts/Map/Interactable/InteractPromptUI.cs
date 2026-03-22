using UnityEngine;
using TMPro;

public class InteractPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI promptText;

    public void Show(string text)
    {
        gameObject.SetActive(true);
        promptText.text = text;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}