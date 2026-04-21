using UnityEngine;

public abstract class EnemyProjectileBase : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] protected float speed = 8f;
    [SerializeField] protected float maxTravelDistance = 15f;

    [Header("Collision")]
    [SerializeField] protected LayerMask targetLayers;
    [SerializeField] protected LayerMask obstacleLayers;

    protected Vector3 moveDirection;
    protected float damage;
    protected float traveled;
    protected bool hasHit;
    protected HealthBase attacker;
    protected Collider[] ignoredColliders;

    public virtual void Initialize(Vector3 targetPosition, float dmg, HealthBase attackerHealth = null)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;
        moveDirection = dir.sqrMagnitude > 0.001f ? dir.normalized : transform.forward;
        damage = dmg;
        attacker = attackerHealth;

        if (attackerHealth != null)
            ignoredColliders = attackerHealth.GetComponentsInChildren<Collider>();
    }

    protected virtual void Update()
    {
        if (hasHit) return;
        if (Physics.Raycast(transform.position, moveDirection, speed * Time.deltaTime + 0.1f, obstacleLayers))
        {
            OnHit();
            return;
        }
        Move();
        if (traveled >= maxTravelDistance)
            OnHit();
    }

    protected virtual void Move()
    {
        float step = speed * Time.deltaTime;
        transform.position += moveDirection * step;
        traveled += step;
    }

    protected void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        if (other.GetComponent<EnemyProjectileBase>() != null) return;
        if (ignoredColliders != null)
            foreach (var col in ignoredColliders)
                if (col == other) return;

        int layer = 1 << other.gameObject.layer;
        bool isTarget = (targetLayers.value & layer) != 0;
        bool isObstacle = (obstacleLayers.value & layer) != 0;
        if (!isTarget && !isObstacle) return;

        OnHit(isTarget ? other : null);
    }

    protected virtual void OnHit(Collider hitTarget = null)
    {
        hasHit = true;
        Destroy(gameObject);
    }

    protected void DealDamageTo(Collider col)
    {
        var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
        if (ps != null && !ps.IsDead) { ps.TakeDamage(damage, attacker); return; }

        var hb = col.GetComponentInParent<HealthBase>();
        if (hb != null && !hb.IsDead) hb.TakeDamage(damage);
    }
}