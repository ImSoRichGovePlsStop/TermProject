using System.Collections;
using TMPro;
using UnityEngine;

public class DamageNumberUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text damageText;

    // Normal
    private float normalFontSize = 4f;
    private Color normalEnemyColor = new Color(1f, 1f, 1f, 1f);
    private Color normalPlayerColor = new Color(1f, 0f, 0f, 1f);

    // Player damage
    private float playerDamageFontSize = 5f;
    private float playerDamageLifetime = 1.2f;

    // Crit
    private float critFontSize = 5f;
    private Color critEnemyColor = new Color(1f, 0.85f, 0f, 1f);
    private Color critPlayerColor = new Color(1f, 0f, 0f, 1f);

    // Animation
    private float floatSpeed = 0.3f;
    private float lifetime = 0.8f;
    private float fadeDuration = 0.4f;

    // Heal
    private float healFontSize = 4f;
    private Color healColor = new Color(0.3f, 1f, 0.4f, 1f);

    private Camera cam;

    public void Init(float damage, bool isCrit, bool isPlayerDamage)
    {
        cam = Camera.main;

        string text = Mathf.CeilToInt(damage).ToString();
        if (isCrit) text += "!";

        damageText.text = text;
        damageText.fontSize = isPlayerDamage ? playerDamageFontSize : (isCrit ? critFontSize : normalFontSize);
        float actualLifetime = isPlayerDamage ? playerDamageLifetime : lifetime;

        if (isPlayerDamage)
            damageText.color = isCrit ? critPlayerColor : normalPlayerColor;
        else
            damageText.color = isCrit ? critEnemyColor : normalEnemyColor;

        StartCoroutine(Animate(actualLifetime));
    }

    public void InitMessage(string message, Color color, float fontSize = 3.5f, float duration = 1.4f)
    {
        cam = Camera.main;
        damageText.text = message;
        damageText.fontSize = fontSize;
        damageText.color = color;
        damageText.overflowMode = TextOverflowModes.Overflow;
        damageText.enableWordWrapping = false;
        StartCoroutine(Animate(duration));
    }

    public void InitHeal(float amount)
    {
        cam = Camera.main;
        damageText.text = "+" + Mathf.CeilToInt(amount).ToString();
        damageText.fontSize = healFontSize;
        damageText.color = healColor;
        StartCoroutine(Animate(lifetime));
    }

    private void LateUpdate()
    {
        if (cam != null)
            transform.rotation = cam.transform.rotation;
    }

    private IEnumerator Animate(float duration)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Color startColor = damageText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = startPos + Vector3.up * floatSpeed * elapsed;

            float fadeStart = duration - fadeDuration;
            if (elapsed > fadeStart)
            {
                float fadeProgress = (elapsed - fadeStart) / fadeDuration;
                Color c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                damageText.color = c;
            }
            yield return null;
        }
        Destroy(gameObject);
    }
}