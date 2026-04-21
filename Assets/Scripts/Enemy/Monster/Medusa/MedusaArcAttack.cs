using UnityEngine;

public class MedusaArcAttack : MonoBehaviour
{
    public enum ConeMode
    {
        Fixed,
        Random,
        Sweep
    }

    private enum AttackPhase
    {
        RedWarning,
        YellowActive
    }

    [Header("References")]
    [SerializeField] private MedusaFacing facing;

    [Header("Mode")]
    [SerializeField] private bool randomizeModeEachFaceChange = true;
    [SerializeField] private ConeMode currentMode = ConeMode.Fixed;

    [Header("Detection")]
    [SerializeField] private float radius = 2f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private string targetTag = "Player";

    [Header("Damage")]
    [SerializeField] private float damageScale = 1f;
    private EntityStats entityStats;
    private HealthBase selfHealth;

    [Header("Timing")]
    [SerializeField] private float redDuration = 2f;
    [SerializeField] private float yellowDuration = 2f;

    [Header("Indicators")]
    [SerializeField] private GameObject redIndicatorPrefab;
    [SerializeField] private GameObject yellowIndicatorPrefab;
    [SerializeField] private Vector3 redOffset = new Vector3(0f, 0.02f, 0f);
    [SerializeField] private Vector3 yellowOffset = new Vector3(0f, 0.021f, 0f);

    [Header("Attack Origin Offset")]
    [SerializeField] private Vector3 upFacingOffset = new Vector3(0f, 0f, 0.8f);
    [SerializeField] private Vector3 downFacingOffset = new Vector3(0f, 0f, -0.8f);

    [Header("Fixed Mode")]
    [SerializeField] private float fixedYawMin = -45f;
    [SerializeField] private float fixedYawMax = 45f;

    [Header("Random Mode")]
    [SerializeField] private float randomYawRange = 45f;

    [Header("Sweep Mode")]
    [SerializeField] private float sweepRange = 60f;
    [SerializeField] private float sweepSpeed = 2f;
    [SerializeField] private bool invertSweepDirection = false;

    [Header("Debug")]
    [SerializeField] private bool showDebugCone = true;
    [SerializeField] private int debugSegments = 24;

    private AttackPhase currentPhase;
    private float phaseTimer;

    private bool currentFacingUp = true;
    private bool targetFacingUp = false;
    private bool hasDamagedThisYellowPhase = false;

    private float currentYawOffset = 0f;
    private float sweepTime = 0f;
    private float sweepStartTimeForCycle = 0f;

    private GameObject currentRedInstance;
    private ConeWarningMesh currentRedMesh;

    private GameObject currentYellowInstance;
    private ConeWarningMesh currentYellowMesh;

    private void Awake()
    {
        if (facing == null)
            facing = GetComponent<MedusaFacing>();

        entityStats = GetComponent<EntityStats>();
        selfHealth = GetComponent<HealthBase>();

        if (facing == null)
            facing = GetComponentInChildren<MedusaFacing>();

        if (facing == null)
            facing = GetComponentInParent<MedusaFacing>();

        if (facing != null)
            currentFacingUp = facing.IsFacingUp;

        targetFacingUp = !currentFacingUp;
        StartRedPhase();
    }

    private void Update()
    {
        phaseTimer -= Time.deltaTime;

        if (currentPhase == AttackPhase.RedWarning)
        {
            UpdateConeDirection();
            UpdateRedIndicator();

            if (phaseTimer <= 0f)
            {
                currentFacingUp = targetFacingUp;
                ApplyFacing(currentFacingUp);
                StartYellowPhase();
            }
        }
        else if (currentPhase == AttackPhase.YellowActive)
        {
            UpdateConeDirection();
            UpdateYellowIndicator();
            TryDamagePlayerInCone(currentFacingUp);

            if (phaseTimer <= 0f)
            {
                targetFacingUp = !currentFacingUp;
                StartRedPhase();
            }
        }
    }

    private void StartRedPhase()
    {
        currentPhase = AttackPhase.RedWarning;
        phaseTimer = redDuration;
        hasDamagedThisYellowPhase = false;

        PickNextModeAndDirection();

        if (currentMode == ConeMode.Sweep)
            sweepTime = sweepStartTimeForCycle;

        ClearYellowIndicator();
        SpawnRedIndicator();
    }

    private void StartYellowPhase()
    {
        currentPhase = AttackPhase.YellowActive;
        phaseTimer = yellowDuration;
        hasDamagedThisYellowPhase = false;

        if (currentMode == ConeMode.Sweep)
            sweepTime = sweepStartTimeForCycle;

        ClearRedIndicator();
        SpawnYellowIndicator();
    }

    private void PickNextModeAndDirection()
    {
        if (randomizeModeEachFaceChange)
            currentMode = (ConeMode)Random.Range(0, 3);

        switch (currentMode)
        {
            case ConeMode.Fixed:
                currentYawOffset = Random.Range(fixedYawMin, fixedYawMax);
                break;

            case ConeMode.Random:
                currentYawOffset = Random.Range(-randomYawRange, randomYawRange);
                break;

            case ConeMode.Sweep:
                sweepStartTimeForCycle = Random.Range(0f, Mathf.PI * 2f);
                sweepTime = sweepStartTimeForCycle;
                UpdateSweepYaw();
                break;
        }

    }

    private void UpdateConeDirection()
    {
        if (currentMode != ConeMode.Sweep)
            return;

        sweepTime += Time.deltaTime * sweepSpeed;
        UpdateSweepYaw();
    }

    private void UpdateSweepYaw()
    {
        float t = (Mathf.Sin(sweepTime) + 1f) * 0.5f;
        currentYawOffset = Mathf.Lerp(-sweepRange, sweepRange, t);

        if (invertSweepDirection)
            currentYawOffset = -currentYawOffset;
    }

    private void TryDamagePlayerInCone(bool isFacingUp)
    {
        if (hasDamagedThisYellowPhase)
            return;

        Vector3 baseDir = isFacingUp ? Vector3.forward : Vector3.back;
        Vector3 attackDir = Quaternion.AngleAxis(currentYawOffset, Vector3.up) * baseDir;
        Vector3 origin = GetAttackOrigin(isFacingUp);

        Collider[] hits = Physics.OverlapSphere(origin, radius);

        foreach (Collider hit in hits)
        {
            PlayerStats playerStats = hit.GetComponent<PlayerStats>();
            if (playerStats == null) playerStats = hit.GetComponentInParent<PlayerStats>();
            if (playerStats == null) playerStats = hit.GetComponentInChildren<PlayerStats>();

            if (playerStats == null)
            {
                var hb = hit.GetComponent<HealthBase>() ?? hit.GetComponentInParent<HealthBase>();
                if (hb == null || hb.IsDead || hb == selfHealth) continue;

                Vector3 toHb = hit.ClosestPoint(origin) - origin;
                toHb.y = 0f;
                if (toHb.sqrMagnitude < 0.0001f) continue;
                if (toHb.magnitude > radius) continue;
                if (Vector3.Angle(attackDir, toHb.normalized) > angle * 0.5f) continue;

                hb.TakeDamage(GetDamage());
                continue;
            }

            if (!playerStats.CompareTag(targetTag))
                continue;

            Vector3 targetPoint = hit.ClosestPoint(origin);
            Vector3 toTarget = targetPoint - origin;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude < 0.0001f)
                continue;

            float distanceToTarget = toTarget.magnitude;
            if (distanceToTarget > radius)
                continue;

            toTarget.Normalize();

            float currentAngle = Vector3.Angle(attackDir, toTarget);

            if (currentAngle <= angle * 0.5f)
            {
                playerStats.TakeDamage(GetDamage(), selfHealth);
                hasDamagedThisYellowPhase = true;
                return;
            }
        }
    }

    private void SpawnRedIndicator()
    {
        if (redIndicatorPrefab == null)
            return;

        ClearRedIndicator();

        Vector3 origin = GetAttackOrigin(targetFacingUp);
        Quaternion rot = GetConeRotation(targetFacingUp);

        currentRedInstance = Instantiate(
            redIndicatorPrefab,
            origin + redOffset,
            rot
        );

        currentRedMesh = currentRedInstance.GetComponent<ConeWarningMesh>();
        if (currentRedMesh != null)
            currentRedMesh.SetShape(radius, angle);
    }

    private void UpdateRedIndicator()
    {
        if (currentRedInstance == null)
            return;

        Vector3 origin = GetAttackOrigin(targetFacingUp);
        currentRedInstance.transform.position = origin + redOffset;
        currentRedInstance.transform.rotation = GetConeRotation(targetFacingUp);

        if (currentRedMesh == null)
            currentRedMesh = currentRedInstance.GetComponent<ConeWarningMesh>();

        if (currentRedMesh != null)
            currentRedMesh.SetShape(radius, angle);
    }

    private void SpawnYellowIndicator()
    {
        if (yellowIndicatorPrefab == null)
            return;

        ClearYellowIndicator();

        Vector3 origin = GetAttackOrigin(currentFacingUp);
        Quaternion rot = GetConeRotation(currentFacingUp);

        currentYellowInstance = Instantiate(
            yellowIndicatorPrefab,
            origin + yellowOffset,
            rot
        );

        currentYellowMesh = currentYellowInstance.GetComponent<ConeWarningMesh>();
        if (currentYellowMesh != null)
            currentYellowMesh.SetShape(radius, angle);
    }

    private void UpdateYellowIndicator()
    {
        if (currentYellowInstance == null)
            return;

        Vector3 origin = GetAttackOrigin(currentFacingUp);
        currentYellowInstance.transform.position = origin + yellowOffset;
        currentYellowInstance.transform.rotation = GetConeRotation(currentFacingUp);

        if (currentYellowMesh == null)
            currentYellowMesh = currentYellowInstance.GetComponent<ConeWarningMesh>();

        if (currentYellowMesh != null)
            currentYellowMesh.SetShape(radius, angle);
    }

    private void ClearRedIndicator()
    {
        if (currentRedInstance != null)
        {
            Destroy(currentRedInstance);
            currentRedInstance = null;
            currentRedMesh = null;
        }
    }

    private void ClearYellowIndicator()
    {
        if (currentYellowInstance != null)
        {
            Destroy(currentYellowInstance);
            currentYellowInstance = null;
            currentYellowMesh = null;
        }
    }

    private Vector3 GetAttackOrigin(bool isFacingUp)
    {
        return transform.position + (isFacingUp ? upFacingOffset : downFacingOffset);
    }

    private Quaternion GetConeRotation(bool isFacingUp)
    {
        Vector3 baseDir = isFacingUp ? Vector3.forward : Vector3.back;
        Vector3 attackDir = Quaternion.AngleAxis(currentYawOffset, Vector3.up) * baseDir;

        if (attackDir.sqrMagnitude < 0.0001f)
            attackDir = Vector3.forward;

        return Quaternion.LookRotation(attackDir, Vector3.up);
    }

    private void ApplyFacing(bool isUp)
    {
        if (facing == null)
            return;

        facing.SendMessage("SetFacingUp", isUp, SendMessageOptions.DontRequireReceiver);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugCone)
            return;

        bool drawFacingUp = Application.isPlaying ? targetFacingUp : true;

        if (Application.isPlaying && currentPhase == AttackPhase.YellowActive)
            drawFacingUp = currentFacingUp;

        Vector3 baseDir = drawFacingUp ? Vector3.forward : Vector3.back;
        Vector3 dir = Quaternion.AngleAxis(currentYawOffset, Vector3.up) * baseDir;
        Vector3 origin = GetAttackOrigin(drawFacingUp);

        Gizmos.color = currentPhase == AttackPhase.RedWarning ? Color.red : Color.yellow;
        DrawConeGizmo(origin, dir, angle, radius, debugSegments);
    }

    private void DrawConeGizmo(Vector3 origin, Vector3 dir, float coneAngle, float coneRadius, int segments)
    {
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector3.forward;

        dir.Normalize();

        float halfAngle = coneAngle * 0.5f;

        Vector3 leftEdge = origin + (Quaternion.AngleAxis(-halfAngle, Vector3.up) * dir) * coneRadius;
        Vector3 rightEdge = origin + (Quaternion.AngleAxis(halfAngle, Vector3.up) * dir) * coneRadius;

        Gizmos.DrawLine(origin, leftEdge);
        Gizmos.DrawLine(origin, rightEdge);

        Vector3 prevPoint = leftEdge;

        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float a = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 nextPoint = origin + (Quaternion.AngleAxis(a, Vector3.up) * dir) * coneRadius;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        Gizmos.DrawLine(origin, origin + dir * coneRadius);
    }

    public void ClearSpawnedIndicators()
    {
        if (currentRedInstance != null)
        {
            Destroy(currentRedInstance);
            currentRedInstance = null;
            currentRedMesh = null;
        }

        if (currentYellowInstance != null)
        {
            Destroy(currentYellowInstance);
            currentYellowInstance = null;
            currentYellowMesh = null;
        }
    }

    private float GetDamage() => (entityStats != null ? entityStats.Damage : 1f) * damageScale;
}