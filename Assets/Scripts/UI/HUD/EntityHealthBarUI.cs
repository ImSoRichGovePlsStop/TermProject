using System.Collections;
using UnityEngine;

public class EntityHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform fill;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Shield Bar")]
    [SerializeField] private RectTransform shieldFill;
    [SerializeField] private GameObject shieldBarRoot;

    [Header("Settings")]
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float fadeDuration = 0.3f;

    private HealthBase entity;
    private EnemyHealthBase enemyHealth;
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

        enemyHealth = healthBase as EnemyHealthBase;
        if (enemyHealth != null)
            enemyHealth.OnShieldChanged += UpdateShieldBar;

        if (shieldBarRoot != null)
            shieldBarRoot.SetActive(false);

        UpdateBar();
        UpdateShieldBar();
    }

    private void OnDestroy()
    {
        if (entity != null)
        {
            entity.OnDamageReceived -= OnDamageReceived;
            entity.OnDeath -= OnEntityDied;
        }
        if (enemyHealth != null)
            enemyHealth.OnShieldChanged -= UpdateShieldBar;
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
        if (entity == null) { Destroy(gameObject); return; }
        if (entity.IsDead) return;

        transform.rotation = cam.transform.rotation;
        transform.position = entity.transform.position + CalculateOffset(entity.transform.position);
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (entity == null) return;
        float ratio = entity.MaxHP > 0f ? entity.CurrentHP / entity.MaxHP : 0f;
        var size = fill.sizeDelta;
        size.x = barWidth * ratio;
        fill.sizeDelta = size;
    }

    private void UpdateShieldBar()
    {
        if (enemyHealth == null || shieldBarRoot == null || shieldFill == null) return;

        bool hasShield = enemyHealth.HasShield;
        shieldBarRoot.SetActive(hasShield);

        if (!hasShield) return;

        float ratio = enemyHealth.MaxShield > 0f
            ? enemyHealth.CurrentShield / enemyHealth.MaxShield
            : 0f;

        var size = shieldFill.sizeDelta;
        size.x = barWidth * ratio;
        shieldFill.sizeDelta = size;
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