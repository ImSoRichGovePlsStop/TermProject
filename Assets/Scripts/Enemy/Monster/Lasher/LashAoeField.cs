using System.Collections;
using UnityEngine;

public class LashAoeField : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private Transform visualScale;

    [Header("Colors")]
    [SerializeField] private Color fadeInColor = new Color(1f, 0.4f, 0f, 0f);
    [SerializeField] private Color fullColor = new Color(1f, 0.4f, 0f, 0.8f);
    [SerializeField] private Color fadeOutColor = new Color(1f, 0.4f, 0f, 0f);

    private Renderer visualRenderer;

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    public void Initialize(float radius, float damage, float startDelay, float fadeInDuration, float stayDuration, float fadeOutDuration, LayerMask targetLayers, HealthBase attacker, GameObject vfxPrefab, float vfxBaseRadius, float vfxOffsetY, float vfxOffsetZMult)
    {
        if (visualScale != null)
        {
            visualRenderer = visualScale.GetComponent<Renderer>();
            visualScale.localScale = new Vector3(radius * 2f, visualScale.localScale.y, radius * 2f);
        }
        SetColor(fadeInColor);
        StartCoroutine(FieldRoutine(radius, damage, startDelay, fadeInDuration, stayDuration, fadeOutDuration, targetLayers, attacker, vfxPrefab, vfxBaseRadius, vfxOffsetY, vfxOffsetZMult));
    }

    private IEnumerator FieldRoutine(float radius, float damage, float startDelay, float fadeInDuration, float stayDuration, float fadeOutDuration, LayerMask targetLayers, HealthBase attacker, GameObject vfxPrefab, float vfxBaseRadius, float vfxOffsetY, float vfxOffsetZMult)
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            SetColor(Color.Lerp(fadeInColor, fullColor, elapsed / fadeInDuration));
            yield return null;
        }
        SetColor(fullColor);

        // Stay
        yield return new WaitForSeconds(stayDuration);

        // Deal damage + spawn VFX
        DealDamage(radius, damage, targetLayers, attacker);
        if (vfxPrefab != null)
        {
            Vector3 vfxPos = transform.position + Vector3.up * vfxOffsetY + Vector3.forward * radius * vfxOffsetZMult;
            var vfx = Instantiate(vfxPrefab, vfxPos, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * (radius / vfxBaseRadius);
        }

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            SetColor(Color.Lerp(fullColor, fadeOutColor, elapsed / fadeOutDuration));
            yield return null;
        }

        Destroy(gameObject);
    }

    private void DealDamage(float radius, float damage, LayerMask targetLayers, HealthBase attacker)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);
        foreach (var col in hits)
        {
            var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, attacker); continue; }
            var hb = col.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
        }
    }

    private void SetColor(Color color)
    {
        if (visualRenderer != null)
            visualRenderer.material.color = color;
    }
}