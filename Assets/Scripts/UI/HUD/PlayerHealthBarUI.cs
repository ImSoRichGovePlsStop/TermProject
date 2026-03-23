using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Bar")]
    [SerializeField] private RectTransform redFill;
    [SerializeField] private RectTransform darkRedFill;
    [SerializeField] private RectTransform shieldFill;

    [Header("Border")]
    [SerializeField] private GameObject redBorder;
    [SerializeField] private GameObject shieldBorder;

    [Header("Text")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text shieldText;

    private float barWidth;

    private void Start()
    {
        if (playerStats == null)
            playerStats = FindFirstObjectByType<PlayerStats>();

        barWidth = ((RectTransform)redFill.parent).rect.width;
    }

    private void Update()
    {
        if (playerStats == null) return;

        float currentHP = playerStats.CurrentHealth;
        float maxHP = playerStats.MaxHealth;
        float shield = playerStats.CurrentShield;

        if (maxHP <= 0f) return;

        float shieldStart = maxHP - Mathf.Min(shield, maxHP);
        float shieldRatio = Mathf.Min(shield, maxHP) / maxHP;
        float redRatio = Mathf.Min(currentHP, shieldStart) / maxHP;
        float darkRedRatio = Mathf.Max(0f, shieldStart - currentHP) / maxHP;

        SetWidth(redFill, redRatio);
        SetWidth(darkRedFill, darkRedRatio);
        SetWidth(shieldFill, shieldRatio);

        healthText.text = $"{Mathf.CeilToInt(currentHP)} / {Mathf.CeilToInt(maxHP)}";
        shieldText.text = shield > 0f ? Mathf.CeilToInt(shield).ToString() : "";

        if (shieldBorder != null)
            shieldBorder.SetActive(shield > 0f);
        if (redBorder != null)
            redBorder.SetActive(currentHP < maxHP);
    }

    private void SetWidth(RectTransform rect, float ratio)
    {
        var layout = rect.GetComponent<LayoutElement>();
        if (layout != null)
            layout.preferredWidth = barWidth * ratio;
        else
        {
            var size = rect.sizeDelta;
            size.x = barWidth * ratio;
            rect.sizeDelta = size;
        }
    }
}