using System.Collections;
using UnityEngine;

public class DisableOnEnemyDeath : MonoBehaviour
{
    [SerializeField] private float fadeOutDuration = 1f;

    private Light pointLight;

    private void Start()
    {
        pointLight = GetComponent<Light>();
        var health = GetComponentInParent<EnemyHealthBase>();
        if (health != null)
            health.OnDeath += () => StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FadeOutRoutine()
    {
        float startIntensity = pointLight != null ? pointLight.intensity : 0f;
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            if (pointLight != null) pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}