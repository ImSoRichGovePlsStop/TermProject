using UnityEngine;
using TMPro;

public class PassiveTooltipUI : MonoBehaviour
{
    public static PassiveTooltipUI Instance;

    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void Show(PassiveNode node)
    {
        nameText.text = node.nodeName;
        descriptionText.text = node.description;
        costText.text = $"Cost: {node.Cost} pt";
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        Instance = null;
    }
}