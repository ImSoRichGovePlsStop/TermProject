using UnityEngine;
using System.Collections.Generic;

public class ReaverProjectile : EnemyProjectileBase
{
    [SerializeField] private float groundOffset = 0.5f;

    private readonly HashSet<GameObject> alreadyHit = new HashSet<GameObject>();

    public override void Initialize(Vector3 targetPosition, float dmg, HealthBase attackerHealth = null)
    {
        base.Initialize(targetPosition, dmg, attackerHealth);

        LayerMask groundMask = 1 << LayerMask.NameToLayer("Ground");
        if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, groundMask))
            transform.position = new Vector3(transform.position.x, hit.point.y + groundOffset, transform.position.z);

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, angle - 90f, 0f);
        }
    }

    protected override void Update()
    {
        if (hasHit) return;
        Move();
        if (traveled >= maxTravelDistance)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }

    protected override void OnHit(Collider hitTarget = null)
    {
        if (hitTarget != null && !alreadyHit.Contains(hitTarget.gameObject))
        {
            alreadyHit.Add(hitTarget.gameObject);
            DealDamageTo(hitTarget);
        }
    }
}