using UnityEngine;

public class MedusaBackAttack : MonoBehaviour
{
    public enum BackAttackType
    {
        SpawnCircleUnderPlayer,
        BeamToPlayer
    }

    private enum AttackPhase
    {
        Idle,
        TrackingWarning,
        LockedWarning,
        Fire
    }

    [Header("References")]
    [SerializeField] private MedusaFacing facing;

    [Header("Target")]
    [SerializeField] private string targetTag = "Player";

    [Header("Attack Cycle")]
    [SerializeField] private bool randomizeAttackTypeEachCycle = true;
    [SerializeField] private BackAttackType currentAttackType = BackAttackType.SpawnCircleUnderPlayer;
    [SerializeField] private float idleDuration = 1.5f;
    [SerializeField] private float trackDuration = 0.6f;
    [SerializeField] private float lockDuration = 0.8f;
    [SerializeField] private float fireDuration = 0.25f;

    [Header("Damage")]
    [SerializeField] private float damageScale = 1f;
    private EntityStats entityStats;

    [Header("Circle Attack")]
    [SerializeField] private float circleRadius = 1.25f;
    [SerializeField] private GameObject circleWarningPrefab;
    [SerializeField] private GameObject circleHitPrefab;
    [SerializeField] private Vector3 circleOffset = new Vector3(0f, 0.02f, 0f);

    [Header("Beam Attack")]
    [SerializeField] private float beamRadius = 0.45f;
    [SerializeField] private float beamLength = 8f;
    [SerializeField] private GameObject beamWarningPrefab;
    [SerializeField] private GameObject beamHitPrefab;
    [SerializeField] private Vector3 beamOffset = new Vector3(0f, 0.02f, 0f);

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private AttackPhase currentPhase = AttackPhase.Idle;
    private float phaseTimer;

    private Transform playerTarget;
    private bool hasDamagedThisCycle;

    private Vector3 trackedTargetPosition;
    private Vector3 trackedBeamDirection = Vector3.forward;

    private Vector3 lockedTargetPosition;
    private Vector3 lockedBeamDirection = Vector3.forward;

    private GameObject currentWarningInstance;
    private GameObject currentHitInstance;

    // ใช้เช็ก "เปลี่ยนทิศหรือยัง"
    private bool hasConsumedCurrentFacing;
    private bool lastFacingUp;
    private bool hasInitializedFacing;

    private void Awake()
    {
        if (facing == null)
            facing = GetComponent<MedusaFacing>();

        if (facing == null)
            facing = GetComponentInChildren<MedusaFacing>();

        if (facing == null)
            facing = GetComponentInParent<MedusaFacing>();

        FindPlayerTarget();
        StartIdlePhase();
        entityStats = GetComponent<EntityStats>();
        selfHealth = GetComponent<HealthBase>();
    }

    private HealthBase selfHealth;

    private void Update()
    {
        if (playerTarget == null)
            FindPlayerTarget();

        if (facing == null)
            return;

        bool currentFacingUp = facing.IsFacingUp;

        if (!hasInitializedFacing)
        {
            hasInitializedFacing = true;
            lastFacingUp = currentFacingUp;
            hasConsumedCurrentFacing = false;
            StartIdlePhase();
        }

        if (currentFacingUp != lastFacingUp)
        {
            lastFacingUp = currentFacingUp;
            hasConsumedCurrentFacing = false;
            StartIdlePhase();
        }

        if (hasConsumedCurrentFacing && currentPhase == AttackPhase.Idle)
            return;

        phaseTimer -= Time.deltaTime;

        switch (currentPhase)
        {
            case AttackPhase.Idle:
                if (phaseTimer <= 0f)
                {
                    StartTrackingWarningPhase();
                    hasConsumedCurrentFacing = true;
                }
                break;

            case AttackPhase.TrackingWarning:
                UpdateTrackingData();
                UpdateWarningVisualUsingTracked();

                if (phaseTimer <= 0f)
                {
                    LockAttackData();
                    StartLockedWarningPhase();
                }
                break;

            case AttackPhase.LockedWarning:
                UpdateWarningVisualUsingLocked();

                if (phaseTimer <= 0f)
                {
                    FireAttack();
                    StartFirePhase();
                }
                break;

            case AttackPhase.Fire:
                UpdateHitVisual();

                if (!hasDamagedThisCycle)
                {
                    ApplyAttackDamage();
                    hasDamagedThisCycle = true;
                }

                if (phaseTimer <= 0f)
                    StartIdlePhase();
                break;
        }
    }

    private void StartIdlePhase()
    {
        ClearAllIndicators();
        currentPhase = AttackPhase.Idle;
        phaseTimer = idleDuration;
        hasDamagedThisCycle = false;
    }

    private void StartTrackingWarningPhase()
    {
        if (playerTarget == null)
        {
            StartIdlePhase();
            return;
        }

        if (randomizeAttackTypeEachCycle)
            currentAttackType = (BackAttackType)Random.Range(0, 2);

        UpdateTrackingData();

        ClearAllIndicators();
        SpawnWarningVisual();

        currentPhase = AttackPhase.TrackingWarning;
        phaseTimer = trackDuration;
        hasDamagedThisCycle = false;
    }

    private void StartLockedWarningPhase()
    {
        currentPhase = AttackPhase.LockedWarning;
        phaseTimer = lockDuration;
    }

    private void StartFirePhase()
    {
        currentPhase = AttackPhase.Fire;
        phaseTimer = fireDuration;
    }

    private void UpdateTrackingData()
    {
        if (playerTarget == null)
            return;

        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                trackedTargetPosition = playerTarget.position;
                break;

            case BackAttackType.BeamToPlayer:
                Vector3 fromMedusaToPlayer = playerTarget.position - transform.position;
                fromMedusaToPlayer.y = 0f;

                if (fromMedusaToPlayer.sqrMagnitude < 0.0001f)
                    fromMedusaToPlayer = Vector3.forward;

                trackedBeamDirection = fromMedusaToPlayer.normalized;
                break;
        }
    }

    private void LockAttackData()
    {
        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                lockedTargetPosition = trackedTargetPosition;
                break;

            case BackAttackType.BeamToPlayer:
                lockedBeamDirection = trackedBeamDirection;
                break;
        }
    }

    private void SpawnWarningVisual()
    {
        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                if (circleWarningPrefab != null)
                {
                    currentWarningInstance = Instantiate(
                        circleWarningPrefab,
                        trackedTargetPosition + circleOffset,
                        Quaternion.identity
                    );

                    currentWarningInstance.transform.localScale =
                        new Vector3(circleRadius * 2f, 1f, circleRadius * 2f);
                }
                break;

            case BackAttackType.BeamToPlayer:
                if (beamWarningPrefab != null)
                {
                    Quaternion rot = Quaternion.LookRotation(trackedBeamDirection, Vector3.up);
                    currentWarningInstance = Instantiate(
                        beamWarningPrefab,
                        transform.position + beamOffset,
                        rot
                    );

                    currentWarningInstance.transform.localScale =
                        new Vector3(beamRadius * 2f, 1f, beamLength);
                }
                break;
        }
    }

    private void UpdateWarningVisualUsingTracked()
    {
        if (currentWarningInstance == null)
            return;

        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                currentWarningInstance.transform.position = trackedTargetPosition + circleOffset;
                currentWarningInstance.transform.rotation = Quaternion.identity;
                currentWarningInstance.transform.localScale =
                    new Vector3(circleRadius * 2f, 1f, circleRadius * 2f);
                break;

            case BackAttackType.BeamToPlayer:
                currentWarningInstance.transform.position = transform.position + beamOffset;
                currentWarningInstance.transform.rotation = Quaternion.LookRotation(trackedBeamDirection, Vector3.up);
                currentWarningInstance.transform.localScale =
                    new Vector3(beamRadius * 2f, 1f, beamLength);
                break;
        }
    }

    private void UpdateWarningVisualUsingLocked()
    {
        if (currentWarningInstance == null)
            return;

        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                currentWarningInstance.transform.position = lockedTargetPosition + circleOffset;
                currentWarningInstance.transform.rotation = Quaternion.identity;
                currentWarningInstance.transform.localScale =
                    new Vector3(circleRadius * 2f, 1f, circleRadius * 2f);
                break;

            case BackAttackType.BeamToPlayer:
                currentWarningInstance.transform.position = transform.position + beamOffset;
                currentWarningInstance.transform.rotation = Quaternion.LookRotation(lockedBeamDirection, Vector3.up);
                currentWarningInstance.transform.localScale =
                    new Vector3(beamRadius * 2f, 1f, beamLength);
                break;
        }
    }

    private void FireAttack()
    {
        if (currentWarningInstance != null)
        {
            Destroy(currentWarningInstance);
            currentWarningInstance = null;
        }

        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                if (circleHitPrefab != null)
                {
                    currentHitInstance = Instantiate(
                        circleHitPrefab,
                        lockedTargetPosition + circleOffset,
                        Quaternion.identity
                    );

                    currentHitInstance.transform.localScale =
                        new Vector3(circleRadius * 2f, 1f, circleRadius * 2f);
                }
                break;

            case BackAttackType.BeamToPlayer:
                if (beamHitPrefab != null)
                {
                    Quaternion rot = Quaternion.LookRotation(lockedBeamDirection, Vector3.up);
                    currentHitInstance = Instantiate(
                        beamHitPrefab,
                        transform.position + beamOffset,
                        rot
                    );

                    currentHitInstance.transform.localScale =
                        new Vector3(beamRadius * 2f, 1f, beamLength);
                }
                break;
        }
    }

    private void UpdateHitVisual()
    {
        if (currentHitInstance == null)
            return;

        switch (currentAttackType)
        {
            case BackAttackType.SpawnCircleUnderPlayer:
                currentHitInstance.transform.position = lockedTargetPosition + circleOffset;
                currentHitInstance.transform.rotation = Quaternion.identity;
                currentHitInstance.transform.localScale =
                    new Vector3(circleRadius * 2f, 1f, circleRadius * 2f);
                break;

            case BackAttackType.BeamToPlayer:
                currentHitInstance.transform.position = transform.position + beamOffset;
                currentHitInstance.transform.rotation = Quaternion.LookRotation(lockedBeamDirection, Vector3.up);
                currentHitInstance.transform.localScale =
                    new Vector3(beamRadius * 2f, 1f, beamLength);
                break;
        }
    }

    private void ApplyAttackDamage()
    {
        if (playerTarget == null)
            return;

        PlayerStats stats = playerTarget.GetComponent<PlayerStats>();
        if (stats == null) stats = playerTarget.GetComponentInParent<PlayerStats>();
        if (stats == null) stats = playerTarget.GetComponentInChildren<PlayerStats>();

        if (stats != null)
        {
            switch (currentAttackType)
            {
                case BackAttackType.SpawnCircleUnderPlayer:
                    ApplyCircleDamage(stats);
                    break;
                case BackAttackType.BeamToPlayer:
                    ApplyBeamDamage(stats);
                    break;
            }
        }

        LayerMask extraMask = (1 << LayerMask.NameToLayer("Summoner"))
                            | (1 << LayerMask.NameToLayer("Totem"));

        float scanRadius = currentAttackType == BackAttackType.SpawnCircleUnderPlayer
            ? circleRadius + 1f
            : beamLength;

        Vector3 scanCenter = currentAttackType == BackAttackType.SpawnCircleUnderPlayer
            ? lockedTargetPosition
            : transform.position;

        Collider[] hits = Physics.OverlapSphere(scanCenter, scanRadius, extraMask);
        foreach (var col in hits)
        {
            var hb = col.GetComponent<HealthBase>() ?? col.GetComponentInParent<HealthBase>();
            if (hb == null || hb.IsDead) continue;

            switch (currentAttackType)
            {
                case BackAttackType.SpawnCircleUnderPlayer:
                    ApplyCircleDamageToHealth(hb);
                    break;
                case BackAttackType.BeamToPlayer:
                    ApplyBeamDamageToHealth(hb);
                    break;
            }
        }
    }

    private void ApplyCircleDamage(PlayerStats stats)
    {
        Vector3 p = stats.transform.position;
        p.y = lockedTargetPosition.y;

        float dist = Vector3.Distance(p, lockedTargetPosition);
        if (dist <= circleRadius)
            stats.TakeDamage(GetDamage(), selfHealth);
    }

    private void ApplyBeamDamage(PlayerStats stats)
    {
        Vector3 beamStart = transform.position + beamOffset;
        Vector3 beamEnd = beamStart + lockedBeamDirection * beamLength;

        Vector3 closest = ClosestPointOnLineSegment(beamStart, beamEnd, stats.transform.position);
        Vector3 flatClosest = closest;
        flatClosest.y = stats.transform.position.y;

        float dist = Vector3.Distance(flatClosest, stats.transform.position);
        if (dist <= beamRadius)
            stats.TakeDamage(GetDamage(), selfHealth);
    }

    private void ApplyCircleDamageToHealth(HealthBase hb)
    {
        Vector3 p = hb.transform.position;
        p.y = lockedTargetPosition.y;

        float dist = Vector3.Distance(p, lockedTargetPosition);
        if (dist <= circleRadius)
            hb.TakeDamage(GetDamage());
    }

    private void ApplyBeamDamageToHealth(HealthBase hb)
    {
        Vector3 beamStart = transform.position + beamOffset;
        Vector3 beamEnd = beamStart + lockedBeamDirection * beamLength;

        Vector3 closest = ClosestPointOnLineSegment(beamStart, beamEnd, hb.transform.position);
        Vector3 flatClosest = closest;
        flatClosest.y = hb.transform.position.y;

        float dist = Vector3.Distance(flatClosest, hb.transform.position);
        if (dist <= beamRadius)
            hb.TakeDamage(GetDamage());
    }

    private Vector3 ClosestPointOnLineSegment(Vector3 a, Vector3 b, Vector3 p)
    {
        Vector3 ab = b - a;
        float denom = Vector3.Dot(ab, ab);
        if (denom <= 0.0001f) return a;

        float t = Vector3.Dot(p - a, ab) / denom;
        t = Mathf.Clamp01(t);
        return a + ab * t;
    }

    private void ClearAllIndicators()
    {
        if (currentWarningInstance != null)
        {
            Destroy(currentWarningInstance);
            currentWarningInstance = null;
        }

        if (currentHitInstance != null)
        {
            Destroy(currentHitInstance);
            currentHitInstance = null;
        }
    }

    private void FindPlayerTarget()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(targetTag);
        if (playerObj != null)
            playerTarget = playerObj.transform;
    }

    private void OnDrawGizmos()
    {
        if (!showDebug)
            return;

        if (!Application.isPlaying)
            return;

        Gizmos.color = currentPhase == AttackPhase.TrackingWarning || currentPhase == AttackPhase.LockedWarning
            ? Color.red
            : Color.yellow;

        if (currentAttackType == BackAttackType.SpawnCircleUnderPlayer)
        {
            Vector3 pos = currentPhase == AttackPhase.TrackingWarning ? trackedTargetPosition : lockedTargetPosition;
            Gizmos.DrawWireSphere(pos + circleOffset, circleRadius);
        }
        else if (currentAttackType == BackAttackType.BeamToPlayer)
        {
            Vector3 start = transform.position + beamOffset;
            Vector3 dir = currentPhase == AttackPhase.TrackingWarning ? trackedBeamDirection : lockedBeamDirection;
            Vector3 end = start + dir * beamLength;
            Gizmos.DrawLine(start, end);
        }
    }

    public void ClearSpawnedIndicators()
    {
        if (currentWarningInstance != null)
        {
            Destroy(currentWarningInstance);
            currentWarningInstance = null;
        }

        if (currentHitInstance != null)
        {
            Destroy(currentHitInstance);
            currentHitInstance = null;
        }
    }

    private float GetDamage() => (entityStats != null ? entityStats.Damage : 1f) * damageScale;
}