using System.Collections;
using UnityEngine;

public class WarlockAOEWarning : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Visual")]
    [SerializeField] private Transform visualScale;
    [SerializeField] private float visualRadiusStart = 0.2f;
    [SerializeField][Range(0f, 1f)] private float expandRatio = 0.6f;

    [Header("Colors")]
    [SerializeField] private Color windUpColorStart = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private Color windUpColorEnd = new Color(1f, 0.3f, 0f, 0.6f);
    [SerializeField] private Color damageColor = new Color(0.5f, 0f, 0f, 1f);
    [SerializeField] private float damagePhaseDuration = 0.05f;

    private float damage;
    private float aoeRadius;
    private float warningDuration;
    private LayerMask targetLayers;
    private Renderer visualRenderer;

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit groundHit, 20f, groundLayer))
            pos.y = groundHit.point.y + groundOffset;
        transform.position = pos;
    }

    public void Initialize(float dmg, float radius, float duration, LayerMask layers)
    {
        damage = dmg;
        aoeRadius = radius;
        warningDuration = duration;
        targetLayers = layers;

        if (visualScale != null)
            visualRenderer = visualScale.GetComponent<Renderer>();

        SetColor(windUpColorStart);
        StartCoroutine(WarningRoutine(aoeRadius * 2f));
    }

    private IEnumerator WarningRoutine(float visualRadiusEnd)
    {
        float windUpDuration = warningDuration - damagePhaseDuration;
        float elapsed = 0f;

        while (elapsed < windUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windUpDuration;

            if (visualScale != null)
            {
                float expandT = Mathf.Clamp01(t / expandRatio);
                float s = Mathf.Lerp(visualRadiusStart, visualRadiusEnd, expandT);
                visualScale.localScale = new Vector3(s, visualScale.localScale.y, s);
            }

            SetColor(Color.Lerp(windUpColorStart, windUpColorEnd, t));
            yield return null;
        }

        // Damage phase
        SetColor(damageColor);
        yield return new WaitForSeconds(damagePhaseDuration);

        DealDamage();
        Destroy(gameObject);
    }

    private void SetColor(Color color)
    {
        if (visualRenderer != null)
            visualRenderer.material.color = color;
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, targetLayers);
        foreach (var hit in hits)
        {
            var ps = hit.GetComponent<PlayerStats>() ?? hit.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage); continue; }

            var hb = hit.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}