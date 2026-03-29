using System.Collections.Generic;
using UnityEngine;

public class BossSwordProjectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float travelDistance = 5f;
    [SerializeField] private float returnStopDistance = 0.4f;

    [Header("Damage")]
    [SerializeField] private float hitRadius = 1.1f;

    private Vector3 direction;
    private Vector3 startPosition;
    private Vector3 outwardTarget;
    private Transform owner;
    private float damage;
    private LayerMask playerLayer;

    private bool initialized = false;
    private bool returning = false;

    private readonly HashSet<PlayerStats> hitOnOutward = new HashSet<PlayerStats>();
    private readonly HashSet<PlayerStats> hitOnReturn = new HashSet<PlayerStats>();

    public void Initialize(Vector3 targetPosition, float attackDamage, LayerMask layerMask)
    {
        Initialize(null, targetPosition, attackDamage, layerMask);
    }

    public void Initialize(Transform ownerTransform, Vector3 targetPosition, float attackDamage, LayerMask layerMask)
    {
        owner = ownerTransform;
        damage = attackDamage;
        playerLayer = layerMask;

        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.001f)
            direction = dir.normalized;
        else if (owner != null)
        {
            Vector3 ownerForward = owner.forward;
            ownerForward.y = 0f;
            direction = ownerForward.sqrMagnitude > 0.001f ? ownerForward.normalized : Vector3.forward;
        }
        else
        {
            direction = Vector3.forward;
        }

        startPosition = transform.position;
        outwardTarget = startPosition + direction * travelDistance;

        initialized = true;
        returning = false;
    }

    private void Update()
    {
        if (!initialized)
            return;

        Vector3 targetPos = returning
            ? (owner != null ? owner.position : startPosition)
            : outwardTarget;

        targetPos.y = transform.position.y;

        Vector3 toTarget = targetPos - transform.position;
        toTarget.y = 0f;

        float step = speed * Time.deltaTime;

        if (toTarget.sqrMagnitude <= step * step)
        {
            transform.position = new Vector3(targetPos.x, transform.position.y, targetPos.z);
            CheckCircleDamage();

            if (!returning)
            {
                returning = true;
            }
            else
            {
                Destroy(gameObject);
            }

            return;
        }

        Vector3 moveDir = toTarget.normalized;
        transform.position += moveDir * step;

        if (moveDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);

        CheckCircleDamage();

        if (returning)
        {
            Vector3 home = owner != null ? owner.position : startPosition;
            home.y = transform.position.y;

            Vector3 toHome = home - transform.position;
            toHome.y = 0f;

            if (toHome.sqrMagnitude <= returnStopDistance * returnStopDistance)
                Destroy(gameObject);
        }
    }

    private void CheckCircleDamage()
    {
        HashSet<PlayerStats> alreadyHit = returning ? hitOnReturn : hitOnOutward;

        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats stats = hits[i].GetComponent<PlayerStats>();
            if (stats == null)
                stats = hits[i].GetComponentInParent<PlayerStats>();

            if (stats != null && !alreadyHit.Contains(stats))
            {
                stats.TakeDamage(damage);
                alreadyHit.Add(stats);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
    }
}