using System.Collections.Generic;
using UnityEngine;

public class HarpyShockwave : MonoBehaviour
{
    [SerializeField] private float startRadius = 0.5f;
    [SerializeField] private float maxRadius = 4f;
    [SerializeField] private float expandSpeed = 5f;
    [SerializeField] private float ringWidth = 0.6f;
    [SerializeField] private LayerMask hitMask;

    private float damage = 10f;
    private float currentRadius;
    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();
    private ShockwaveVFX vfx;

    private void Awake()
    {
        currentRadius = startRadius;
    }

    public void Init(float damage)
    {
        this.damage = damage;
        vfx = GetComponent<ShockwaveVFX>();
    }

    private void Update()
    {
        currentRadius += expandSpeed * Time.deltaTime;

        float innerRadius = currentRadius - ringWidth * 0.5f;
        float outerRadius = currentRadius + ringWidth * 0.5f;

        vfx?.UpdateRadius(currentRadius);

        Collider[] hits = Physics.OverlapSphere(transform.position, outerRadius, hitMask);
        foreach (var col in hits)
        {
            if (alreadyHit.Contains(col.gameObject)) continue;

            float dist = Vector3.Distance(
                new Vector3(col.transform.position.x, transform.position.y, col.transform.position.z),
                transform.position
            );

            if (dist < innerRadius || dist > outerRadius) continue;

            alreadyHit.Add(col.gameObject);

            var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage); continue; }

            var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
        }

        if (currentRadius >= maxRadius)
            Destroy(gameObject);
    }
}