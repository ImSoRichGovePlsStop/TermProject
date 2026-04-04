using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireField : MonoBehaviour
{
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private Color fieldColor = new Color(1f, 0.3f, 0f, 0.25f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAlphaMin = 0.1f;
    [SerializeField] private float pulseAlphaMax = 0.4f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundOffset = 0.02f;

    private float radius;
    private float damage;
    private float duration;
    private LayerMask enemyMask;
    private Renderer fieldRenderer;

    public void Init(float radius, float damage, float duration, LayerMask enemyMask)
    {
        this.radius = radius;
        this.damage = damage;
        this.duration = duration;
        this.enemyMask = enemyMask;

        transform.localScale = new Vector3(radius * 2f, transform.localScale.y, radius * 2f);

        var r = GetComponentInChildren<Renderer>();
        if (r != null)
        {
            fieldRenderer = r;
            fieldRenderer.material = new Material(fieldRenderer.material);
            fieldRenderer.material.color = fieldColor;
        }

        StartCoroutine(LifetimeRoutine());
        StartCoroutine(DamageRoutine());
    }

    private void Update()
    {
        if (fieldRenderer != null)
        {
            float alpha = Mathf.Lerp(pulseAlphaMin, pulseAlphaMax,
                (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f);
            Color c = fieldColor;
            c.a = alpha;
            fieldRenderer.material.color = c;
        }

        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    private IEnumerator DamageRoutine()
    {
        var wait = new WaitForSeconds(tickInterval);
        while (true)
        {
            yield return wait;
            var hits = CombatUtility.FindAround<HealthBase>(transform.position, radius, enemyMask);
            foreach (var h in hits)
            {
                if (h == null || h.IsDead) continue;
                h.TakeDamage(damage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}