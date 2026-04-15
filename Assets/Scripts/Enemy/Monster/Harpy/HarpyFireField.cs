using System.Collections;
using UnityEngine;

public class HarpyFireField : MonoBehaviour
{
    [SerializeField] private Color fieldColorStart = new Color(1f, 0.4f, 0f, 0.4f);
    [SerializeField] private Color fieldColorEnd = new Color(1f, 0.1f, 0f, 0.1f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAlphaMin = 0.15f;
    [SerializeField] private float pulseAlphaMax = 0.5f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private LayerMask targetLayers;

    private float tickInterval;
    private float duration;
    private float radius;
    private float damagePerTick;
    private Renderer fieldRenderer;
    private float elapsed = 0f;

    public void Init(float radius, float damagePerTick, float duration, float tickInterval)
    {
        this.radius = radius;
        this.damagePerTick = damagePerTick;
        this.duration = duration;
        this.tickInterval = tickInterval;

        transform.localScale = new Vector3(radius * 2f, transform.localScale.y, radius * 2f);

        var r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            fieldRenderer = r;
            fieldRenderer.material = new Material(fieldRenderer.material);
            fieldRenderer.material.color = fieldColorStart;
        }

        StartCoroutine(DamageRoutine());
        StartCoroutine(LifetimeRoutine());
    }

    private void Update()
    {
        elapsed += Time.deltaTime;

        if (fieldRenderer != null)
        {
            float lifeRatio = elapsed / duration;
            Color baseColor = Color.Lerp(fieldColorStart, fieldColorEnd, lifeRatio);
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            baseColor.a = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax, pulse) * (1f - lifeRatio * 0.5f);
            fieldRenderer.material.color = baseColor;
        }

        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    private IEnumerator DamageRoutine()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            yield return wait;
            Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);
            foreach (var col in hits)
            {
                var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
                if (ps != null && !ps.IsDead) { ps.TakeDamage(damagePerTick, null); continue; }

                var hb = col.GetComponentInParent<HealthBase>();
                if (hb != null && !hb.IsDead) hb.TakeDamage(damagePerTick);
            }
        }
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }
}