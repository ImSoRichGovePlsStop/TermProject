using System.Collections;
using UnityEngine;

public class WarlockAOEWarning : MonoBehaviour
{
    [Header("Warning")]
    [SerializeField] private float warningDuration = 1f;
    [SerializeField] private float aoeRadius = 2f;

    [Header("Visual")]
    [SerializeField] private Transform visualScale;
    [SerializeField] private float visualRadiusStart = 0.2f;
    [SerializeField] private float visualRadiusEnd = 1f;
    [SerializeField][Range(0f, 1f)] private float expandRatio = 0.6f;

    private float damage;
    private LayerMask targetLayers;
    private bool initialized;

    // Called by EliteWarlockController for AoE Smash / Ultimate
    public void Initialize(float dmg, float radius, float duration, LayerMask layers)
    {
        damage = dmg;
        aoeRadius = radius;
        warningDuration = duration;
        targetLayers = layers;
        initialized = true;
        StartCoroutine(WarningRoutine());
    }

    // Called by WarlockProjectile explosion (no layer needed, uses defaults)
    public void TriggerImmediate(float dmg, float radius)
    {
        damage = dmg;
        aoeRadius = radius;
        initialized = true;
        DealDamage();
        Destroy(gameObject);
    }

    private IEnumerator WarningRoutine()
    {
        float elapsed = 0f;
        while (elapsed < warningDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / warningDuration;

            if (visualScale != null)
            {
                float expandT = Mathf.Clamp01(t / expandRatio);
                float scale = Mathf.Lerp(visualRadiusStart, visualRadiusEnd, expandT);
                visualScale.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        DealDamage();
        Destroy(gameObject);
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