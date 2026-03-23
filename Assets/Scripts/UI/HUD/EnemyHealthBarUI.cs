using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2.2f, 0f);

    private EnemyHealth enemy;
    private Camera cam;
    private float barWidth;
    private Coroutine hideCoroutine;
    private Coroutine fadeCoroutine;

    public void Init(EnemyHealth enemyHealth, Vector3 barOffset, Vector3 barScale)
    {
        enemy = enemyHealth;
        enemy.OnDamageReceived += OnDamageReceived;
        offset = barOffset;
        transform.localScale = new Vector3(
            transform.localScale.x * barScale.x,
            transform.localScale.y * barScale.y,
            transform.localScale.z * barScale.z
        );

        cam = Camera.main;
        barWidth = ((RectTransform)fill.parent).rect.width;

        canvasGroup.alpha = 0f;
        UpdateBar();
    }

    private void OnDestroy()
    {
        if (enemy != null)
            enemy.OnDamageReceived -= OnDamageReceived;
    }

    private void LateUpdate()
    {
        if (enemy == null) return;

        transform.rotation = cam.transform.rotation;
        transform.position = enemy.transform.position + offset;

        UpdateBar();
    }

    private void UpdateBar()
    {
        if (enemy == null) return;

        float ratio = enemy.MaxHP > 0f ? enemy.CurrentHP / enemy.MaxHP : 0f;
        var size = fill.sizeDelta;
        size.x = barWidth * ratio;
        fill.sizeDelta = size;
    }

    private void OnDamageReceived()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(1f));

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(0f));
    }

    private IEnumerator FadeTo(float target)
    {
        float start = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = target;
    }
}