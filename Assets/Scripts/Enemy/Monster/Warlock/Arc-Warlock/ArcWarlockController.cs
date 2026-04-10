using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ArcWarlockController : WarlockController
{
    [Header("Arc Projectile")]
    [SerializeField] private GameObject arcProjectilePrefab;

    [Header("Arc Node Spawn")]
    [SerializeField] private GameObject arcNodePrefab;
    [SerializeField] private float arcSpawnDamageScale = 1.2f;
    [SerializeField] private float nodeMinSpawnRadius = 1.5f;
    [SerializeField] private float nodeMaxSpawnRadius = 4f;
    [SerializeField] private ArcNode.LinkMode nodeLinkMode = ArcNode.LinkMode.Nearest;
    [SerializeField] private float minNodeDistance = 3f;

    public override void FireProjectile()
    {
        if (arcProjectilePrefab == null || firePoint == null) return;
        hasFired = true;
        SpawnHomingProjectile();
    }

    public override void FireLastProjectile()
    {
        if (arcProjectilePrefab == null || firePoint == null) return;
        SpawnHomingProjectile();
        isWindingUp = false;
        hasFired = false;
        lastShootTime = Time.time;
        TriggerPostAttackDelay();
    }

    private void SpawnHomingProjectile()
    {
        var go = Instantiate(arcProjectilePrefab, firePoint.position, arcProjectilePrefab.transform.rotation);
        var proj = go.GetComponent<ArcWarlockProjectile>();
        proj?.Initialize(lockedTargetPosition, stats.Damage * projectileDamageScale, health);
    }

    protected override bool TrySmash()
    {
        if (isSmashing) return true;
        if (isWindingUp) return false;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        if (!HasTarget || Vector3.Distance(transform.position, TargetPosition) > smashRange) return false;
        StartCoroutine(ArcSpawnRoutine());
        return true;
    }

    private IEnumerator ArcSpawnRoutine()
    {
        isSmashing = true;
        movement.StopMoving();
        animator?.SetTrigger("SpawnWindUp");
        yield return null;
        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / smashWindUpDuration;
        }
        yield return new WaitForSeconds(smashWindUpDuration);
        if (animator != null) animator.speed = 1f;
        isSmashExecuting = true;
        SpawnArcNodes(TargetPosition);
        animator?.SetTrigger("Spawn");
        yield return new WaitForSeconds(smashWarningDuration);
        lastSmashTime = Time.time;
        isSmashing = false;
        isSmashExecuting = false;
        TriggerPostAttackDelay();
    }

    private void SpawnArcNodes(Vector3 center)
    {
        if (arcNodePrefab == null) return;
        float dmg = stats.Damage * arcSpawnDamageScale;

        Vector3 posA = FindSpawnPosition(center, -1f);
        float angleA = Mathf.Atan2(posA.x - center.x, posA.z - center.z) * Mathf.Rad2Deg;
        float oppositeAngle = angleA + 180f;

        Vector3 posB = FindSpawnPosition(center, oppositeAngle);

        var nodeA = InstantiateNode(posA, dmg);
        var nodeB = InstantiateNode(posB, dmg);
    }

    private Vector3 FindSpawnPosition(Vector3 center, float preferredAngle)
    {
        Vector3 bestPos = center;
        float bestMinDist = float.MinValue;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float angle = preferredAngle < 0f
                ? Random.Range(0f, 360f)
                : Mathf.LerpAngle(preferredAngle - 45f, preferredAngle + 45f, attempt / 9f);

            float radius = Random.Range(nodeMinSpawnRadius, nodeMaxSpawnRadius);
            Vector3 candidate = center + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;
            Vector3 navPos = hit.position;

            float minDist = GetMinDistToExistingNodes(navPos);
            if (minDist >= minNodeDistance) return navPos;
            if (minDist > bestMinDist) { bestMinDist = minDist; bestPos = navPos; }
        }

        return bestPos;
    }

    private float GetMinDistToExistingNodes(Vector3 pos)
    {
        float min = float.MaxValue;
        foreach (var node in FindObjectsByType<ArcNode>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(pos, node.transform.position);
            if (d < min) min = d;
        }
        return min == float.MaxValue ? float.MaxValue : min;
    }

    private ArcNode InstantiateNode(Vector3 pos, float dmg)
    {
        var go = Instantiate(arcNodePrefab, pos, Quaternion.identity);
        var node = go.GetComponent<ArcNode>();
        node.SetDamageConfig(dmg, health);
        node.SetLinkMode(nodeLinkMode);
        return node;
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, nodeMaxSpawnRadius);
    }
}