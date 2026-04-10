using System.Collections.Generic;
using UnityEngine;

public class ArcNode : MonoBehaviour
{
    [SerializeField] private GameObject arcLinePrefab;
    [SerializeField] private float initialDelay = 2f;

    [Header("Ground Snap")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundOffset = 0.05f;

    private static readonly List<ArcNode> AllNodes = new List<ArcNode>();
    private static event System.Action OnNodeCountChanged;

    private ArcNode slot1;
    private ArcNode slot2;

    private float linkDamage;
    private HealthBase attacker;
    private LinkMode linkMode;

    private bool initialDelayDone = false;

    public enum LinkMode { Nearest, Farthest, Random }

    public void SetDamageConfig(float dmg, HealthBase atk)
    {
        linkDamage = dmg;
        attacker = atk;
    }

    public void SetLinkMode(LinkMode mode)
    {
        linkMode = mode;
    }

    private void Awake()
    {
        AllNodes.Add(this);
        OnNodeCountChanged?.Invoke();

        var health = GetComponent<HealthBase>();
        if (health != null) health.OnDeath += OnDeath;
    }

    private void Start()
    {
        Invoke(nameof(OnInitialDelayDone), initialDelay);
    }

    private void OnInitialDelayDone()
    {
        initialDelayDone = true;
        TryLink();
        OnNodeCountChanged += OnNodeChanged;
    }

    private void OnDestroy()
    {
        OnNodeCountChanged -= OnNodeChanged;
        AllNodes.Remove(this);
        if (gameObject.scene.isLoaded)
            OnNodeCountChanged?.Invoke();
    }

    private void OnDeath()
    {
        // OnDestroy will handle the rest via Destroy(gameObject) in ArcNodeHealthBase
    }

    private void LateUpdate() => SnapToGround();

    private void OnNodeChanged() => TryLink();

    private void TryLink()
    {
        if (!initialDelayDone) return;

        if (slot1 == null)
        {
            ArcNode target = FindTarget();
            if (target != null) Link(target);
        }

        if (slot2 == null)
        {
            ArcNode target = FindTarget();
            if (target != null) Link(target);
        }
    }

    private void Link(ArcNode other)
    {
        if (slot1 == null) slot1 = other; else slot2 = other;
        if (other.slot1 == null) other.slot1 = this; else other.slot2 = this;

        if (!HasFreeSlot()) OnNodeCountChanged -= OnNodeChanged;
        if (!other.HasFreeSlot()) OnNodeCountChanged -= other.OnNodeChanged;

        var go = Instantiate(arcLinePrefab, Vector3.zero, Quaternion.identity);
        var line = go.GetComponent<ArcLine>();
        line?.Initialize(this, other, linkDamage, attacker);
    }

    private ArcNode FindTarget()
    {
        HashSet<ArcNode> segment = GetSegment();
        List<ArcNode> candidates = new List<ArcNode>();

        foreach (var node in AllNodes)
        {
            if (node == this) continue;
            if (segment.Contains(node)) continue;
            if (!node.HasFreeSlot()) continue;
            if (!node.initialDelayDone) continue;
            candidates.Add(node);
        }

        if (candidates.Count == 0) return null;

        switch (linkMode)
        {
            case LinkMode.Nearest:
                {
                    ArcNode best = null;
                    float bestDist = float.MaxValue;
                    foreach (var node in candidates)
                    {
                        float d = (transform.position - node.transform.position).sqrMagnitude;
                        if (d < bestDist) { bestDist = d; best = node; }
                    }
                    return best;
                }
            case LinkMode.Farthest:
                {
                    ArcNode best = null;
                    float bestDist = float.MinValue;
                    foreach (var node in candidates)
                    {
                        float d = (transform.position - node.transform.position).sqrMagnitude;
                        if (d > bestDist) { bestDist = d; best = node; }
                    }
                    return best;
                }
            case LinkMode.Random:
                return candidates[Random.Range(0, candidates.Count)];

            default:
                return null;
        }
    }

    private HashSet<ArcNode> GetSegment()
    {
        var visited = new HashSet<ArcNode>();
        var queue = new Queue<ArcNode>();
        queue.Enqueue(this);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            if (!visited.Add(n)) continue;
            if (n.slot1 != null) queue.Enqueue(n.slot1);
            if (n.slot2 != null) queue.Enqueue(n.slot2);
        }
        return visited;
    }

    public void NotifyUnlink(ArcNode other)
    {
        if (slot1 == other) slot1 = null;
        else if (slot2 == other) slot2 = null;

        OnNodeCountChanged -= OnNodeChanged;
        OnNodeCountChanged += OnNodeChanged;
        TryLink();
    }

    public bool HasFreeSlot() => slot1 == null || slot2 == null;

    private void SnapToGround()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        if (slot1 != null) { Gizmos.color = Color.magenta; Gizmos.DrawLine(transform.position, slot1.transform.position); }
        if (slot2 != null) { Gizmos.color = Color.cyan; Gizmos.DrawLine(transform.position, slot2.transform.position); }
    }
}