using UnityEngine;

public class WarlockProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxTravelDistance = 15f;

    [Header("AoE Explosion")]
    [SerializeField] private float aoeRadius = 1.5f;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private LayerMask obstacleLayers;

    private Vector3 moveDirection;
    private float damage;
    private float traveled;
    private bool exploded;
    private HealthBase attacker;
    private Collider[] ignoredColliders;

    public void Initialize(Vector3 targetPosition, float dmg, HealthBase attackerHealth = null)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        moveDirection = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
        damage = dmg;
        attacker = attackerHealth;

        if (attackerHealth != null)
            ignoredColliders = attackerHealth.GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        if (exploded) return;

        float step = speed * Time.deltaTime;
        transform.position += moveDirection * step;
        traveled += step;

        if (traveled >= maxTravelDistance)
            Explode();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (exploded) return;
        if (other.GetComponent<WarlockProjectile>() != null) return;
        if (ignoredColliders != null)
            foreach (var col in ignoredColliders)
                if (col == other) return;

        int layer = 1 << other.gameObject.layer;
        bool isTarget = (targetLayers.value & layer) != 0;
        bool isObstacle = (obstacleLayers.value & layer) != 0;
        if (!isTarget && !isObstacle) return;

        Explode();
    }

    private void Explode()
    {
        exploded = true;

        Collider[] hits = Physics.OverlapSphere(transform.position, aoeRadius, targetLayers);
        foreach (var hit in hits)
        {
            var ps = hit.GetComponent<PlayerStats>() ?? hit.GetComponentInParent<PlayerStats>();
            if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, attacker); continue; }

            var hb = hit.GetComponentInParent<HealthBase>();
            if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, aoeRadius);
    }
}