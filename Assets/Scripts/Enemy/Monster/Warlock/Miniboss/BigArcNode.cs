using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BigArcNode : MonoBehaviour
{
    public enum WallSide { Top, Bottom, Left, Right }
    public enum MoveMode { SideOnly, BorderPatrol }

    [Header("Config")]
    [SerializeField] private GameObject bigArcLinePrefab;
    [SerializeField] private float moveSpeedMin = 1.5f;
    [SerializeField] private float moveSpeedMax = 3.5f;

    [Header("Spawn Effect")]
    [SerializeField] private GameObject spawnEffectPrefab;
    [SerializeField] private float spawnDuration = 1f;
    [SerializeField] private float spawnFadeOutDuration = 0.5f;
    [SerializeField] private float spawnRiseDistance = 1f;
    [SerializeField] private float spawnEffectScale = 1f;

    [Header("Regen")]
    [SerializeField][Range(0f, 1f)] private float disableThreshold = 0.5f;
    [SerializeField] private float regenPercentPerSec = 0.05f;

    [Header("Phase 3 - Border Patrol")]
    [SerializeField] private float patrolDirMinDuration = 3f;
    [SerializeField] private float patrolDirMaxDuration = 7f;

    [Header("References")]
    [SerializeField] private Animator animator;

    [Header("Ground Snap")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundOffset = 0.05f;

    private static readonly List<BigArcNode> AllNodes = new List<BigArcNode>();
    private static event System.Action OnNodeStateChanged;

    private BigArcNode slot1;
    private BigArcLine activeLink;

    private float linkDamage;
    private HealthBase attacker;

    public WallSide Side { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    private bool initialDelayDone = false;
    private bool isRegening = false;
    private bool isPhaseThree = false;

    private BigArcNodeHealthBase health;
    private MoveMode moveMode = MoveMode.SideOnly;
    private float moveSpeed;
    private float moveDir = 1f;
    private LayerMask wallMask;

    private float patrolDirTimer = 0f;
    private float patrolDirDuration = 0f;

    public void Initialize(WallSide side, float dmg, HealthBase atk)
    {
        Side = side;
        linkDamage = dmg;
        attacker = atk;
    }

    public void SetPhaseThree()
    {
        isPhaseThree = true;
        moveMode = MoveMode.BorderPatrol;
        moveDir = Random.value < 0.5f ? 1f : -1f;
        moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
        patrolDirDuration = Random.Range(patrolDirMinDuration, patrolDirMaxDuration);
        patrolDirTimer = 0f;
    }

    public void ForceFullHeal()
    {
        if (health == null) return;
        health.Heal(health.MaxHP);
        if (!IsEnabled && isRegening)
        {
            StopAllCoroutines();
            isRegening = false;
            Enable();
        }
    }

    private void Awake()
    {
        wallMask = 1 << LayerMask.NameToLayer("Wall");
        moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);

        health = GetComponent<BigArcNodeHealthBase>();
        if (health != null)
        {
            health.OnDeath += OnDeath;
            health.OnDamaged += OnDamaged;
        }

        AllNodes.Add(this);
    }

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        if (health != null) health.SetInvincible(true);

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        yield return null;

        Vector3 finalPos = transform.position;
        Vector3 startPos = finalPos + Vector3.down * spawnRiseDistance;
        transform.position = startPos;

        if (sr != null) sr.enabled = true;

        EnemySpawnEffect effect = null;
        if (spawnEffectPrefab != null)
        {
            var effectGO = Instantiate(spawnEffectPrefab, finalPos, Quaternion.identity);
            effectGO.transform.localScale *= spawnEffectScale;
            effect = effectGO.GetComponent<EnemySpawnEffect>();
            effect?.Init();
            effect?.PlayFadeIn(spawnDuration);
        }

        float t = 0f;
        while (t < spawnDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.SmoothStep(0f, 1f, t / spawnDuration);
            transform.position = Vector3.Lerp(startPos, finalPos, ratio);
            yield return null;
        }
        transform.position = finalPos;

        effect?.PlayFadeOut(spawnFadeOutDuration);

        if (health != null) health.SetInvincible(false);

        Invoke(nameof(OnInitialDelayDone), 0f);
    }

    private void OnInitialDelayDone()
    {
        initialDelayDone = true;
        OnNodeStateChanged += OnNodeChanged;
        TryLink();
        OnNodeStateChanged?.Invoke();
    }

    private void OnDestroy()
    {
        OnNodeStateChanged -= OnNodeChanged;
        AllNodes.Remove(this);
        if (gameObject.scene.isLoaded)
            OnNodeStateChanged?.Invoke();
    }

    private void Update()
    {
        if (!IsEnabled) return;

        if (moveMode == MoveMode.SideOnly)
            MoveAlongWall();
        else
            MoveAlongBorder();

        SnapToGround();
    }

    private void MoveAlongWall()
    {
        Vector3 dir = GetSideMoveVector();
        if (Physics.Raycast(transform.position, dir, 0.5f, wallMask))
        {
            moveDir *= -1f;
            moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
            return;
        }
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    private void MoveAlongBorder()
    {
        patrolDirTimer += Time.deltaTime;
        if (patrolDirTimer >= patrolDirDuration)
        {
            moveDir *= -1f;
            patrolDirTimer = 0f;
            patrolDirDuration = Random.Range(patrolDirMinDuration, patrolDirMaxDuration);
        }

        Vector3 dir = GetSideMoveVector();
        if (Physics.Raycast(transform.position, dir, 0.5f, wallMask))
        {
            moveSpeed = Random.Range(moveSpeedMin, moveSpeedMax);
            AdvanceToNextSide();
            return;
        }
        transform.position += dir * moveSpeed * Time.deltaTime;
    }

    private void AdvanceToNextSide()
    {
        if (moveDir > 0f)
        {
            switch (Side)
            {
                case WallSide.Top: Side = WallSide.Right; break;
                case WallSide.Right: Side = WallSide.Bottom; break;
                case WallSide.Bottom: Side = WallSide.Left; break;
                case WallSide.Left: Side = WallSide.Top; break;
            }
        }
        else
        {
            switch (Side)
            {
                case WallSide.Top: Side = WallSide.Left; break;
                case WallSide.Left: Side = WallSide.Bottom; break;
                case WallSide.Bottom: Side = WallSide.Right; break;
                case WallSide.Right: Side = WallSide.Top; break;
            }
        }
    }

    private Vector3 GetSideMoveVector()
    {
        switch (Side)
        {
            case WallSide.Top:
            case WallSide.Bottom: return Vector3.right * moveDir;
            case WallSide.Left:
            case WallSide.Right: return Vector3.forward * moveDir;
            default: return Vector3.right * moveDir;
        }
    }

    private void OnDamaged()
    {
        if (!IsEnabled || isRegening) return;
        if (health.CurrentHP / health.MaxHP <= disableThreshold)
            StartCoroutine(DisableAndRegenRoutine());
    }

    private IEnumerator DisableAndRegenRoutine()
    {
        isRegening = true;
        Disable();

        while (health.CurrentHP < health.MaxHP)
        {
            health.Heal(health.MaxHP * regenPercentPerSec);
            yield return new WaitForSeconds(1f);
        }

        Enable();
        isRegening = false;
    }

    private void Disable()
    {
        IsEnabled = false;
        animator?.SetTrigger("Disable");
        if (health != null) health.SetInvincible(true);
        UnlinkAll();
        OnNodeStateChanged?.Invoke();
    }

    private void Enable()
    {
        IsEnabled = true;
        animator?.SetTrigger("Enable");
        if (health != null) health.SetInvincible(false);
        TryLink();
        OnNodeStateChanged?.Invoke();
    }

    private void UnlinkAll()
    {
        if (activeLink != null)
        {
            Destroy(activeLink.gameObject);
            activeLink = null;
        }
        if (slot1 != null)
        {
            slot1.activeLink = null;
            slot1.NotifyUnlink(this);
            slot1 = null;
        }
    }

    private void OnNodeChanged() => TryLink();

    private void TryLink()
    {
        if (!initialDelayDone || !IsEnabled) return;

        if (slot1 == null)
        {
            BigArcNode target = FindTarget();
            if (target != null) Link(target);
        }
    }

    private void Link(BigArcNode other)
    {
        slot1 = other;
        if (other.slot1 == null) other.slot1 = this;

        if (!HasFreeSlot()) OnNodeStateChanged -= OnNodeChanged;
        if (!other.HasFreeSlot()) OnNodeStateChanged -= other.OnNodeChanged;

        var go = Instantiate(bigArcLinePrefab, Vector3.zero, Quaternion.identity);
        var line = go.GetComponent<BigArcLine>();
        line?.Initialize(this, other, linkDamage, attacker);
        activeLink = line;
        other.activeLink = line;
    }

    private BigArcNode FindTarget()
    {
        if (isPhaseThree)
            return FindBestInCandidates(_ => true, random: true);

        BigArcNode best = FindBestInCandidates(n => n.Side == GetOppositeSide(Side), random: false);
        if (best != null) return best;

        return FindBestInCandidates(n => n.Side != Side, random: false);
    }

    private BigArcNode FindBestInCandidates(System.Func<BigArcNode, bool> filter, bool random)
    {
        HashSet<BigArcNode> segment = GetSegment();
        var candidates = new List<BigArcNode>();

        foreach (var node in AllNodes)
        {
            if (node == this) continue;
            if (!filter(node)) continue;
            if (segment.Contains(node)) continue;
            if (!node.HasFreeSlot()) continue;
            if (!node.IsEnabled) continue;
            if (!HasLineOfSight(transform.position, node.transform.position)) continue;
            candidates.Add(node);
        }

        if (candidates.Count == 0) return null;
        return random ? candidates[Random.Range(0, candidates.Count)] : candidates[0];
    }

    public void TryUpgradeLink(BigArcNode newOpposite)
    {
        if (slot1 == newOpposite) return;

        if (slot1 != null && slot1.Side != GetOppositeSide(Side))
        {
            slot1.NotifyUnlink(this);
            slot1 = null;
        }

        if (HasFreeSlot()) Link(newOpposite);
    }

    public void NotifyUnlink(BigArcNode other)
    {
        if (slot1 == other) slot1 = null;

        OnNodeStateChanged -= OnNodeChanged;
        OnNodeStateChanged += OnNodeChanged;
        TryLink();
    }

    public bool HasFreeSlot() => slot1 == null;

    private WallSide GetOppositeSide(WallSide side)
    {
        switch (side)
        {
            case WallSide.Top: return WallSide.Bottom;
            case WallSide.Bottom: return WallSide.Top;
            case WallSide.Left: return WallSide.Right;
            case WallSide.Right: return WallSide.Left;
            default: return WallSide.Bottom;
        }
    }

    private HashSet<BigArcNode> GetSegment()
    {
        var visited = new HashSet<BigArcNode>();
        var queue = new Queue<BigArcNode>();
        queue.Enqueue(this);
        while (queue.Count > 0)
        {
            var n = queue.Dequeue();
            if (!visited.Add(n)) continue;
            if (n.slot1 != null) queue.Enqueue(n.slot1);
        }
        return visited;
    }

    private bool HasLineOfSight(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        dir.y = 0f;
        float dist = dir.magnitude;
        if (dist < 0.01f) return true;
        return !Physics.Raycast(new Vector3(from.x, from.y + 0.1f, from.z), dir.normalized, dist, wallMask);
    }

    private void SnapToGround()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    private void OnDeath()
    {
        UnlinkAll();
        AllNodes.Remove(this);
        OnNodeStateChanged?.Invoke();
    }
}