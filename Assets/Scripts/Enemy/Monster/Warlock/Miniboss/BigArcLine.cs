using UnityEngine;

public class BigArcLine : MonoBehaviour
{
    [Header("Phase 1 - Pulse")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float pulseDuration = 2f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private Color pulseColorA = new Color(0.6f, 0f, 1f, 0f);
    [SerializeField] private Color pulseColorB = new Color(0.6f, 0f, 1f, 0.7f);

    [Header("Phase 2 - Active")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private BoxCollider damageCollider;

    [Header("Offset")]
    [SerializeField] private float yOffset = 0.5f;
    [SerializeField] private float damageInterval = 0.5f;
    [SerializeField] private float colliderHeight = 1f;
    [SerializeField] private float colliderWidth = 0.6f;
    [SerializeField] private float colliderLengthScale = 1f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Ritual Mode")]
    [SerializeField] private float ritualLengthMultiplier = 1.3f;
    [SerializeField] private float ritualDamageMultiplier = 2f;
    [SerializeField] private float ritualDamageInterval = 0.3f;
    [SerializeField] private float ritualColliderLengthScale = 1.3f;

    private BigArcNode nodeA;
    private BigArcNode nodeB;
    private float linkDamage;
    private float baseDamage;
    private HealthBase attacker;

    private float elapsed;
    private bool isActive;
    private float damageTimer;
    private float scaleX;
    private float scaleZ;
    private bool isRitual = false;

    private float currentDamageInterval => isRitual ? ritualDamageInterval : damageInterval;
    private float currentColliderLengthScale => isRitual ? ritualColliderLengthScale : colliderLengthScale;
    private float currentLengthMultiplier => isRitual ? ritualLengthMultiplier : 1f;

    public void SetRitualMode(bool active)
    {
        isRitual = active;
        linkDamage = active ? baseDamage * ritualDamageMultiplier : baseDamage;
    }

    public void Initialize(BigArcNode a, BigArcNode b, float dmg, HealthBase atk)
    {
        nodeA = a;
        nodeB = b;
        linkDamage = dmg;
        baseDamage = dmg;
        attacker = atk;
        elapsed = 0f;
        isActive = false;
        damageTimer = 0f;

        scaleX = transform.localScale.x;
        scaleZ = transform.localScale.z;

        if (lineRenderer != null) lineRenderer.positionCount = 2;
        if (spriteRenderer != null) spriteRenderer.enabled = false;
        if (animator != null) animator.enabled = false;
        if (damageCollider != null) damageCollider.enabled = false;
    }

    private static int wallLayerMask = -1;

    private static int GetWallMask()
    {
        if (wallLayerMask == -1)
            wallLayerMask = 1 << LayerMask.NameToLayer("Wall");
        return wallLayerMask;
    }

    private void Update()
    {
        if (nodeA == null || nodeB == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 posA = nodeA.transform.position + Vector3.up * yOffset;
        Vector3 posB = nodeB.transform.position + Vector3.up * yOffset;

        Vector3 dir = posB - posA;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist > 0.01f && Physics.Raycast(
            new Vector3(posA.x, posA.y + 0.1f, posA.z),
            dir.normalized, dist, GetWallMask()))
        {
            Destroy(gameObject);
            return;
        }

        elapsed += Time.deltaTime;

        if (!isActive)
        {
            UpdateLineRenderer(posA, posB);

            float alpha = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed * Mathf.PI));
            Color col = Color.Lerp(pulseColorA, pulseColorB, alpha);
            lineRenderer.startColor = col;
            lineRenderer.endColor = col;

            if (elapsed >= pulseDuration) TransitionToActive(posA, posB);
        }
        else
        {
            UpdateSpriteTransform(posA, posB);
            UpdateCollider(posA, posB);
        }
    }

    private void TransitionToActive(Vector3 posA, Vector3 posB)
    {
        isActive = true;

        if (lineRenderer != null) lineRenderer.enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (animator != null) animator.enabled = true;
        if (damageCollider != null) damageCollider.enabled = true;

        UpdateSpriteTransform(posA, posB);
        UpdateCollider(posA, posB);
    }

    private void UpdateLineRenderer(Vector3 posA, Vector3 posB)
    {
        if (lineRenderer == null) return;
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, posA);
        lineRenderer.SetPosition(1, posB);
    }

    private void UpdateSpriteTransform(Vector3 posA, Vector3 posB)
    {
        float distance = Vector3.Distance(posA, posB);
        Vector3 dir = (posB - posA).normalized;

        transform.position = (posA + posB) * 0.5f;
        transform.up = dir;

        float cameraRotX = Camera.main != null ? Camera.main.transform.eulerAngles.x : 0f;
        Vector3 euler = transform.eulerAngles;
        euler.x = cameraRotX;
        transform.eulerAngles = euler;

        float spriteHeight = spriteRenderer.sprite.bounds.size.y;
        transform.localScale = new Vector3(scaleX, distance * currentLengthMultiplier / spriteHeight, scaleZ);
    }

    private void UpdateCollider(Vector3 posA, Vector3 posB)
    {
        if (damageCollider == null) return;
        float distance = Vector3.Distance(posA, posB);
        damageCollider.size = new Vector3(colliderWidth, distance * currentColliderLengthScale / transform.lossyScale.y, colliderHeight);
        damageCollider.center = Vector3.zero;
        Vector3 euler = transform.eulerAngles;
        euler.x = 90f;
        damageCollider.transform.eulerAngles = euler;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        DealDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;
        damageTimer += Time.deltaTime;
        if (damageTimer < currentDamageInterval) return;
        damageTimer = 0f;
        DealDamage(other);
    }

    private void DealDamage(Collider other)
    {
        if (targetLayers != 0 && (targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
        var ps = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
        if (ps != null && !ps.IsDead) { ps.TakeDamage(linkDamage, attacker); return; }
        var hb = other.GetComponentInParent<HealthBase>();
        if (hb != null && !hb.IsDead) hb.TakeDamage(linkDamage);
    }

    private void OnDestroy()
    {
        if (animator != null) animator.enabled = false;
        if (!gameObject.scene.isLoaded) return;
        if (nodeA != null) nodeA.NotifyUnlink(nodeB);
        if (nodeB != null) nodeB.NotifyUnlink(nodeA);
    }
}