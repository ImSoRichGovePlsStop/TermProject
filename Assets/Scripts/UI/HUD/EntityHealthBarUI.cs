using System.Collections;
using UnityEngine;

public class EntityHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float fadeDuration = 0.3f;

    private HealthBase entity;
    private EnemyHealth legacyEnemy;
    private Camera cam;
    private float barWidth;
    private float heightOffset;
    private Coroutine hideCoroutine;
    private Coroutine fadeCoroutine;

    public void Init(HealthBase healthBase, float height, Vector3 scale)
    {
        entity = healthBase;
        heightOffset = height;
        entity.OnDamageReceived += OnDamageReceived;
        entity.OnDeath += OnEntityDied;

        transform.localScale = new Vector3(
            transform.localScale.x * scale.x,
            transform.localScale.y * scale.y,
            transform.localScale.z * scale.z
        );

        cam = Camera.main;
        barWidth = ((RectTransform)fill.parent).rect.width;
        canvasGroup.alpha = 0f;
        UpdateBar();
    }

    public void InitLegacy(EnemyHealth enemyHealth, Vector3 barOffset, Vector3 barScale)
    {
        legacyEnemy = enemyHealth;
        heightOffset = barOffset.y;
        legacyEnemy.OnDamageReceived += OnDamageReceived;

        transform.localScale = new Vector3(
            transform.localScale.x * barScale.x,
            transform.localScale.y * barScale.y,
            transform.localScale.z * barScale.z
        );

        cam = Camera.main;
        barWidth = ((RectTransform)fill.parent).rect.width;
        canvasGroup.alpha = 0f;
        UpdateBarLegacy();
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnDamageReceived -= OnDamageReceived;
            entity.OnDeath -= OnEntityDied;
        }
        if (legacyEnemy != null)
            legacyEnemy.OnDamageReceived -= OnDamageReceived;
    }

    private void OnEntityDied()
    {
        Destroy(gameObject);
    }

    private Vector3 CalculateOffset(Vector3 entityPos)
    {
        float tiltRad = cam.transform.eulerAngles.x * Mathf.Deg2Rad;
        float zOffset = heightOffset * Mathf.Tan(tiltRad);
        return new Vector3(0f, heightOffset, zOffset);
    }

    private void LateUpdate()
    {
        if (entity != null)
        {
            if (entity.IsDead) return;
            transform.rotation = cam.transform.rotation;
            transform.position = entity.transform.position + CalculateOffset(entity.transform.position);
            UpdateBar();
        }
        else if (legacyEnemy != null)
        {
            if (legacyEnemy.IsDead) { Destroy(gameObject); return; }
            transform.rotation = cam.transform.rotation;
            transform.position = legacyEnemy.transform.position + CalculateOffset(legacyEnemy.transform.position);
            UpdateBarLegacy();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void UpdateBar()
    {
        if (entity == null) return;
        float ratio = entity.MaxHP > 0f ? entity.CurrentHP / entity.MaxHP : 0f;
        var size = fill.sizeDelta;
        size.x = barWidth * ratio;
        fill.sizeDelta = size;
    }

    private void UpdateBarLegacy()
    {
        if (legacyEnemy == null) return;
        float ratio = legacyEnemy.MaxHP > 0f ? legacyEnemy.CurrentHP / legacyEnemy.MaxHP : 0f;
        var size = fill.sizeDelta;
        size.x = barWidth * ratio;
        fill.sizeDelta = size;
    }

    private void OnDamageReceived(float damage, bool isCrit)
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