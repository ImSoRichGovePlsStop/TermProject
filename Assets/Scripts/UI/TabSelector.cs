using UnityEngine;
using UnityEngine.UI;

public class TabSelector : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    private int _currentIndex = 0;

    private void Awake()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int idx = i;
            tabButtons[i].onClick.AddListener(() => SelectTab(idx));
        }
        SelectTab(1);
    }

    private void OnEnable()
    {
        SelectTab(1);
    }

    public void SelectTab(int index)
    {
        _currentIndex = index;
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        for (int i = 0; i < tabButtons.Length; i++)
        {
            var colors = tabButtons[i].colors;
            colors.normalColor = i == index ? selectedColor : normalColor;
            tabButtons[i].colors = colors;
        }
    }
}