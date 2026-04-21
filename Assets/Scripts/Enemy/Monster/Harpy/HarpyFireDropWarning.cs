using System.Collections;
using UnityEngine;

public class HarpyFireDropWarning : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Transform visualScale;
    [SerializeField] private float visualRadiusStart = 0.1f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Colors")]
    [SerializeField] private Color windUpColorStart = new Color(1f, 0.5f, 0f, 0.3f);
    [SerializeField] private Color windUpColorEnd = new Color(1f, 0.1f, 0f, 0.7f);
    [SerializeField] private Color damageColor = new Color(0.8f, 0f, 0f, 1f);
    [SerializeField] private float damagePhaseDuration = 0.1f;

    [Header("Fire Field")]
    private GameObject fireFieldPrefab;

    private float damage;
    private float radius;
    private float warningDuration;
    private LayerMask targetLayers;
    private HealthBase attacker;
    private Renderer visualRenderer;

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    private float fireFieldDuration;
    private float fireFieldTickInterval;
    private float fireFieldDamagePerTick;

    public void Initialize(float dmg, float rad, float duration, LayerMask targets, HealthBase atk, GameObject fireField, float fieldDuration, float fieldTickInterval, float fieldDamagePerTick)
    {
        damage = dmg;
        radius = rad;
        warningDuration = duration;
        targetLayers = targets;
        attacker = atk;
        fireFieldPrefab = fireField;
        fireFieldDuration = fieldDuration;
        fireFieldTickInterval = fieldTickInterval;
        fireFieldDamagePerTick = fieldDamagePerTick;

        if (visualScale != null)
            visualRenderer = visualScale.GetComponent<Renderer>();

        SetColor(windUpColorStart);
        StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        float windUpDuration = warningDuration - damagePhaseDuration;
        float visualRadiusEnd = radius * 2f;
        float elapsed = 0f;

        while (elapsed < windUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / windUpDuration;

            if (visualScale != null)
            {
                float s = Mathf.Lerp(visualRadiusStart, visualRadiusEnd, t);
                visualScale.localScale = new Vector3(s, visualScale.localScale.y, s);
            }

            SetColor(Color.Lerp(windUpColorStart, windUpColorEnd, t));
            yield return null;
        }

        SetColor(damageColor);
        yield return new WaitForSeconds(damagePhaseDuration);

        DealDamage();
        SpawnFireField();
        Destroy(gameObject);
    }

    private void DealDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers);
        foreach (var hit in hits)
        {
            var ps = hit.GetComponent<PlayerStats>() ?? hit.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, attacker); continue; }

            var hb = hit.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
        }
    }

    private void SpawnFireField()
    {
        if (fireFieldPrefab == null) return;
        GameObject go = Instantiate(fireFieldPrefab, transform.position, Quaternion.identity);
        go.GetComponent<HarpyFireField>()?.Init(radius, fireFieldDamagePerTick, fireFieldDuration, fireFieldTickInterval);
    }

    private void SetColor(Color color)
    {
        if (visualRenderer != null)
            visualRenderer.material.color = color;
    }
}