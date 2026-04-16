using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class WarlockPhaseSettings
{
    public int ultimateCircleCountMin = 4;
    public int ultimateCircleCountMax = 7;
    public float ultimateMinSpawnDistance = 1f;
    public int summonCount = 3;
    [Range(0f, 1f)] public float eliteRatio = 0.33f;
}

public class MinibossWarlockController : WarlockController
{
    [Header("Jump")]
    [SerializeField] private float jumpTriggerRange = 2f;
    [SerializeField] private float jumpEscapeCooldown = 8f;
    [SerializeField] private float jumpCooldownReductionOnHit = 2f;
    [SerializeField] private float jumpCooldownReductionInterval = 0.2f;
    [SerializeField] private float jumpRadius = 6f;
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private float jumpPeakHeight = 2f;
    [SerializeField] private int jumpCandidates = 8;

    [Header("Ultimate")]
    [SerializeField] private float ultimateDamageScale = 1.5f;
    [SerializeField] private float ultimateWindUpDuration = 2f;
    [SerializeField] private float ultimateWarningDuration = 1.5f;
    [SerializeField] private float ultimateAoeRadius = 2f;
    [SerializeField] private float ultimateCooldown = 12f;
    [SerializeField] private float ultimateSpawnRadius = 8f;

    [Header("Summon")]
    [SerializeField][Range(0f, 1f)] private float arcWarlockRatio = 0.5f;
    [SerializeField] private float summonCooldown = 20f;
    [SerializeField] private float summonWindUpDuration = 1.5f;
    [SerializeField] private float summonMinRadius = 2f;
    [SerializeField] private float summonRadius = 5f;
    [SerializeField] private float summonMinSpawnDistance = 1.5f;
    [SerializeField] private GameObject normalWarlockPrefab;
    [SerializeField] private GameObject eliteWarlockPrefab;
    [SerializeField] private GameObject normalArcWarlockPrefab;
    [SerializeField] private GameObject eliteArcWarlockPrefab;

    [Header("Phase Settings")]
    [SerializeField] private WarlockPhaseSettings phase1Settings;
    [SerializeField] private WarlockPhaseSettings phase2Settings;
    [SerializeField] private WarlockPhaseSettings phase3Settings;

    [Header("Ritual Phase")]
    [SerializeField] private float ritualDuration = 15f;
    [SerializeField] private Vector2 ritualPositionNormalized = new Vector2(0.5f, 0.5f);
    [SerializeField] private BarrierDomeVFX warlockBarrier;

    [Header("Ritual Smash")]
    [SerializeField] private int ritualSmashCount = 4;
    [SerializeField] private float ritualSmashDamageScale = 1.2f;
    [SerializeField] private float ritualSmashWarningDurationMin = 0.8f;
    [SerializeField] private float ritualSmashWarningDurationMax = 1.5f;
    [SerializeField] private float ritualSmashAoeRadiusMin = 1f;
    [SerializeField] private float ritualSmashAoeRadiusMax = 2.5f;
    [SerializeField] private float ritualSmashSpawnInterval = 3f;
    [SerializeField] private float ritualSmashInitialDelay = 2f;

    [Header("Ritual Lighting")]
    [SerializeField] private float ritualGlobalLightIntensity = 0.2f;
    [SerializeField] private float ritualSpotLightIntensity = 3f;
    [SerializeField] private float ritualSpotLightRange = 20f;
    [SerializeField] private float ritualSpotLightAngle = 60f;
    [SerializeField] private float ritualSpotLightInnerAngle = 30f;
    [SerializeField] private Color ritualSpotLightColor = Color.white;
    [SerializeField] private float lightTransitionDuration = 1.5f;

    [Header("Big Arc Node")]
    [SerializeField] private GameObject bigArcNodePrefab;
    [SerializeField] private float bigArcNodeWallOffset = 1f;
    [SerializeField] private float bigArcNodeDamageScale = 2f;
    [SerializeField] private float bigArcNodeHpScale = 0.3f;
    [SerializeField] private float bigArcNodeSlideRange = 5f;

    private WarlockPhaseSettings currentPhaseSettings;

    private bool isJumping;
    private bool isCastingUltimate;
    private bool isSummoning;
    private bool isSmashAnimFinished;
    private bool isInRitual;

    private float lastJumpEscapeTime;
    private float lastCooldownReductionTime = -Mathf.Infinity;
    private float lastUltimateTime = 0f;
    private float lastSummonTime;

    private bool IsOccupied => isJumping || isSmashing || isCastingUltimate || isSummoning || isInRitual;


    public override bool CanBeInterrupted() => false;

    protected override void Awake()
    {
        base.Awake();
        currentPhaseSettings = phase1Settings;
        lastJumpEscapeTime = -jumpEscapeCooldown * 0.5f;
        lastSummonTime = -summonCooldown * 0.5f;

        var minibossHealth = GetComponent<MinibossWarlockHealthBase>();
        if (minibossHealth != null)
        {
            minibossHealth.OnPhaseTwo += EnterPhaseTwo;
            minibossHealth.OnPhaseThree += EnterPhaseThree;
            minibossHealth.OnDamageReceived += OnDamageReceived;
        }
    }

    private void OnDamageReceived(float damage, bool isCrit)
    {
        if (Time.time < lastCooldownReductionTime + jumpCooldownReductionInterval) return;
        lastCooldownReductionTime = Time.time;
        lastJumpEscapeTime -= jumpCooldownReductionOnHit;
    }

    private Light cachedGlobalLight;
    private Light ritualSpotLight;
    private float originalGlobalLightIntensity;

    private void CacheGlobalLight()
    {
        if (cachedGlobalLight != null) return;
        var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                cachedGlobalLight = l;
                originalGlobalLightIntensity = l.intensity;
                break;
            }
        }
    }

    private Light SpawnRitualSpotLight(Vector3 position)
    {
        var go = new GameObject("RitualSpotLight");
        go.transform.position = position + Vector3.up * 10f;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        var light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = ritualSpotLightColor;
        light.range = ritualSpotLightRange;
        light.spotAngle = ritualSpotLightAngle;
        light.innerSpotAngle = ritualSpotLightInnerAngle;
        light.intensity = 0f;
        return light;
    }

    private IEnumerator TransitionLights(bool ritualOn, Vector3 arenaCenter)
    {
        CacheGlobalLight();

        if (ritualOn)
            ritualSpotLight = SpawnRitualSpotLight(arenaCenter);

        float targetGlobal = ritualOn ? ritualGlobalLightIntensity : originalGlobalLightIntensity;
        float targetSpot = ritualOn ? ritualSpotLightIntensity : 0f;

        float startGlobal = cachedGlobalLight != null ? cachedGlobalLight.intensity : 0f;
        float startSpot = ritualSpotLight != null ? ritualSpotLight.intensity : 0f;

        float elapsed = 0f;
        while (elapsed < lightTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lightTransitionDuration);
            if (cachedGlobalLight != null) cachedGlobalLight.intensity = Mathf.Lerp(startGlobal, targetGlobal, t);
            if (ritualSpotLight != null) ritualSpotLight.intensity = Mathf.Lerp(startSpot, targetSpot, t);
            yield return null;
        }

        if (!ritualOn && ritualSpotLight != null)
        {
            UnityEngine.Object.Destroy(ritualSpotLight.gameObject);
            ritualSpotLight = null;
        }
    }

    private IEnumerator RitualSmashRoutine()
    {
        yield return new WaitForSeconds(ritualSmashInitialDelay);
        Debug.Log($"[RitualSmash] Start spawning at Time={Time.time:F2}");

        while (isInRitual)
        {
            var spawned = new List<Vector3>();
            LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
            int maxAttempts = ritualSmashCount * 8;

            for (int attempt = 0; attempt < maxAttempts && spawned.Count < ritualSmashCount; attempt++)
            {
                Vector3 candidate = GetRandomRoomPosition();
                if (candidate == Vector3.zero) continue;

                bool tooClose = false;
                foreach (var pos in spawned)
                    if (Vector3.Distance(candidate, pos) < ritualSmashAoeRadiusMin) { tooClose = true; break; }
                if (tooClose) continue;

                float radius = Random.Range(ritualSmashAoeRadiusMin, ritualSmashAoeRadiusMax);
                float duration = Random.Range(ritualSmashWarningDurationMin, ritualSmashWarningDurationMax);
                SpawnAOEWarning(candidate, ritualSmashDamageScale, radius, duration);
                spawned.Add(candidate);
            }

            yield return new WaitForSeconds(ritualSmashSpawnInterval);
            if (!isInRitual) yield break;
        }
    }

    private Vector3 GetRandomRoomPosition()
    {
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        Vector3 origin = transform.position;

        float left = 0f, right = 0f, back = 0f, forward = 0f;
        if (Physics.Raycast(origin, Vector3.left, out RaycastHit h, 100f, wallMask)) left = h.distance;
        if (Physics.Raycast(origin, Vector3.right, out h, 100f, wallMask)) right = h.distance;
        if (Physics.Raycast(origin, Vector3.back, out h, 100f, wallMask)) back = h.distance;
        if (Physics.Raycast(origin, Vector3.forward, out h, 100f, wallMask)) forward = h.distance;

        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(origin.x - left, origin.x + right);
            float z = Random.Range(origin.z - back, origin.z + forward);
            Vector3 candidate = new Vector3(x, origin.y, z);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, NavMesh.AllAreas)) continue;

            Vector3 dir = navHit.position - origin;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(
                new Vector3(origin.x, origin.y + 0.1f, origin.z),
                dir.normalized, dist, wallMask)) continue;

            return navHit.position;
        }
        return Vector3.zero;
    }

    private void EnterPhaseTwo()
    {
        currentPhaseSettings = phase2Settings;
        SpawnBigArcNodes();
    }

    private void EnterPhaseThree()
    {
        currentPhaseSettings = phase3Settings;
        StartCoroutine(RitualPhaseRoutine());
    }

    private Vector3 GetRitualPosition()
    {
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        Vector3 origin = transform.position;

        float left = 0f, right = 0f, back = 0f, forward = 0f;

        if (Physics.Raycast(origin, Vector3.left, out RaycastHit h, 100f, wallMask)) left = h.distance;
        if (Physics.Raycast(origin, Vector3.right, out h, 100f, wallMask)) right = h.distance;
        if (Physics.Raycast(origin, Vector3.back, out h, 100f, wallMask)) back = h.distance;
        if (Physics.Raycast(origin, Vector3.forward, out h, 100f, wallMask)) forward = h.distance;

        Vector3 leftPoint = origin + Vector3.left * left;
        Vector3 rightPoint = origin + Vector3.right * right;
        Vector3 backPoint = origin + Vector3.back * back;
        Vector3 forwardPoint = origin + Vector3.forward * forward;

        float x = Mathf.Lerp(leftPoint.x, rightPoint.x, ritualPositionNormalized.x);
        float z = Mathf.Lerp(backPoint.z, forwardPoint.z, ritualPositionNormalized.y);

        Vector3 target = new Vector3(x, origin.y, z);
        if (NavMesh.SamplePosition(target, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            return navHit.position;
        return origin;
    }

    private IEnumerator RitualPhaseRoutine()
    {
        // 1. Jump to ritual position
        Vector3 ritualPos = GetRitualPosition();
        yield return StartCoroutine(JumpToPosition(ritualPos));

        isInRitual = true;
        movement.StopMoving();
        movement.SetCanMove(false);

        // 2. Heal all BigArcNodes to full
        var allNodes = UnityEngine.Object.FindObjectsByType<BigArcNode>(FindObjectsSortMode.None);
        foreach (var node in allNodes)
            node.ForceFullHeal();

        // 3. Invincible + barrier for warlock and all nodes
        if (health != null) health.IsInvincible = true;
        warlockBarrier?.Show();
        foreach (var node in UnityEngine.Object.FindObjectsByType<BigArcNode>(FindObjectsSortMode.None))
            node.SetRitualMode(true);

        // 3b. Light transition in
        StartCoroutine(TransitionLights(true, GetRitualPosition()));
        StartCoroutine(RitualSmashRoutine());

        // 4. Destroy all BigArcLines -> relink
        var allLines = UnityEngine.Object.FindObjectsByType<BigArcLine>(FindObjectsSortMode.None);
        foreach (var line in allLines)
            UnityEngine.Object.Destroy(line.gameObject);

        yield return null;

        // 5. Set all nodes to phase three (border patrol, fast speed)
        foreach (var node in allNodes)
            node.SetPhaseThree();

        yield return null;

        // 5b. Set ritual mode on new BigArcLines
        var ritualLines = UnityEngine.Object.FindObjectsByType<BigArcLine>(FindObjectsSortMode.None);
        foreach (var line in ritualLines)
            line.SetRitualMode(true);

        // 6. Wait ritual duration
        yield return new WaitForSeconds(ritualDuration);

        // 7. Remove barrier and invincible
        if (health != null) health.IsInvincible = false;
        warlockBarrier?.Hide();
        foreach (var node in allNodes)
            node.SetRitualMode(false);

        var postRitualLines = UnityEngine.Object.FindObjectsByType<BigArcLine>(FindObjectsSortMode.None);
        foreach (var line in postRitualLines)
            line.SetRitualMode(false);

        // 7b. Light transition out
        StartCoroutine(TransitionLights(false, Vector3.zero));

        // 8. Set post-ritual speed for nodes
        foreach (var node in allNodes)
            node.SetPostRitual();

        // 9. Warlock back to normal
        isWindingUp = false;
        isSmashing = false;
        hasFired = false;
        currentState = WarlockState.Wander;
        movement.SetCanMove(true);
        isInRitual = false;
    }

    private void SpawnBigArcNodes()
    {
        if (bigArcNodePrefab == null) return;

        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        Vector3 origin = transform.position;

        var sides = new (Vector3 dir, BigArcNode.WallSide side)[]
        {
            (Vector3.forward,  BigArcNode.WallSide.Top),
            (Vector3.back,     BigArcNode.WallSide.Bottom),
            (Vector3.left,     BigArcNode.WallSide.Left),
            (Vector3.right,    BigArcNode.WallSide.Right),
        };

        foreach (var (dir, side) in sides)
        {
            if (!Physics.Raycast(new Vector3(origin.x, origin.y + 0.1f, origin.z), dir, out RaycastHit hit, 100f, wallMask))
                continue;

            // random position along that wall
            Vector3 spawnPos = GetRandomPositionOnWall(hit, side, bigArcNodeWallOffset);

            var go = Instantiate(bigArcNodePrefab, spawnPos, Quaternion.identity);
            var node = go.GetComponent<BigArcNode>();
            var nodeHealth = go.GetComponent<BigArcNodeHealthBase>();
            if (nodeHealth != null) nodeHealth.SetMaxHp(stats.MaxHP * bigArcNodeHpScale);
            node?.Initialize(side, stats.Damage * bigArcNodeDamageScale, health);
        }
    }

    private Vector3 GetRandomPositionOnWall(RaycastHit hit, BigArcNode.WallSide side, float offset)
    {
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        Vector3 wallNormal = hit.normal;
        Vector3 basePos = hit.point + wallNormal * offset;
        basePos.y = transform.position.y;

        Vector3 slideAxis = (side == BigArcNode.WallSide.Top || side == BigArcNode.WallSide.Bottom)
            ? Vector3.right
            : Vector3.forward;

        for (int attempt = 0; attempt < 10; attempt++)
        {
            float slide = Random.Range(-bigArcNodeSlideRange, bigArcNodeSlideRange);
            Vector3 candidate = basePos + slideAxis * slide;

            if (!UnityEngine.AI.NavMesh.SamplePosition(candidate, out UnityEngine.AI.NavMeshHit navHit, 0.5f, UnityEngine.AI.NavMesh.AllAreas))
                continue;

            // candidate must not be blocked by wall from warlock
            Vector3 dir = navHit.position - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(
                new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z),
                dir.normalized, dist, wallMask)) continue;

            return navHit.position;
        }

        return basePos;
    }

    protected override void UpdateState()
    {
        if (IsOccupied || isWindingUp) return;

        if (HasTarget && Vector3.Distance(transform.position, TargetPosition) < jumpTriggerRange && CanEscapeJump())
        {
            StartCoroutine(JumpRoutine());
            return;
        }

        base.UpdateState();
    }

    protected override void TickState()
    {
        if (IsOccupied) return;

        if (currentState == WarlockState.WindUp && !isWindingUp)
        {
            if (TryUltimate()) return;
            if (TrySummon()) return;
            if (TrySmash()) return;
        }

        base.TickState();
    }

    protected override bool TrySmash()
    {
        if (isSmashing) return true;
        if (Time.time < lastSmashTime + smashCooldown) return false;
        if (!HasTarget || Vector3.Distance(transform.position, TargetPosition) > smashRange) return false;

        StartCoroutine(EliteSmashRoutine());
        return true;
    }

    private IEnumerator EliteSmashRoutine()
    {
        isSmashing = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(smashWindUpDuration);

        SpawnAOEWarning(TargetPosition, smashDamageScale, smashAoeRadius, smashWarningDuration);

        isSmashAnimFinished = false;
        animator?.SetTrigger("Smash");
        yield return new WaitUntil(() => isSmashAnimFinished);

        lastSmashTime = Time.time;
        isSmashing = false;
        TriggerPostAttackDelay();
    }

    public void FinishSmash() => isSmashAnimFinished = true;


    private bool CanEscapeJump() => !isJumping && Time.time >= lastJumpEscapeTime + jumpEscapeCooldown;

    private Vector3 FindJumpDestination()
    {
        Vector3 best = transform.position;
        float bestDist = -1f;
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");

        for (int i = 0; i < jumpCandidates; i++)
        {
            float angle = i * (360f / jumpCandidates);
            Vector3 candidate = transform.position + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * jumpRadius * stats.MoveSpeedRatio;

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas)) continue;

            Vector3 dir = hit.position - transform.position;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(
                new Vector3(transform.position.x, transform.position.y + 0.1f, transform.position.z),
                dir.normalized, dist, wallMask)) continue;

            float d = Vector3.Distance(hit.position, TargetPosition);
            if (d > bestDist) { bestDist = d; best = hit.position; }
        }

        return best;
    }

    private IEnumerator JumpToPosition(Vector3 destination)
    {
        isJumping = true;
        movement.SetCanMove(false);
        animator?.SetTrigger("Jump");

        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / jumpDuration;
        }

        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            transform.position = Vector3.Lerp(startPos, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpPeakHeight;
            yield return null;
        }

        var agent = movement.GetAgent();
        if (NavMesh.SamplePosition(destination, out NavMeshHit landHit, 2f, NavMesh.AllAreas))
            destination = landHit.position;

        if (agent != null) agent.Warp(destination);
        else transform.position = destination;

        if (animator != null) animator.speed = 1f;
        isJumping = false;
    }

    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        movement.SetCanMove(false);
        animator?.SetTrigger("Jump");

        yield return null;

        if (animator != null)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0f) animator.speed = info.length / jumpDuration;
        }

        Vector3 destination = FindJumpDestination();
        Vector3 startPos = transform.position;
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            transform.position = Vector3.Lerp(startPos, destination, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * jumpPeakHeight;
            yield return null;
        }

        var agent = movement.GetAgent();
        if (NavMesh.SamplePosition(destination, out NavMeshHit landHit, 2f, NavMesh.AllAreas))
            destination = landHit.position;

        if (agent != null) agent.Warp(destination);
        else transform.position = destination;

        if (animator != null) animator.speed = 1f;
        movement.SetCanMove(true);
        lastJumpEscapeTime = Time.time;
        isJumping = false;
    }


    private bool TryUltimate()
    {
        if (isCastingUltimate) return true;
        if (Time.time < lastUltimateTime + ultimateCooldown) return false;

        StartCoroutine(UltimateRoutine());
        return true;
    }

    private IEnumerator UltimateRoutine()
    {
        isCastingUltimate = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(ultimateWindUpDuration);

        SpawnUltimateCircles();

        isSmashAnimFinished = false;
        animator?.SetTrigger("Smash");
        yield return new WaitUntil(() => isSmashAnimFinished);

        lastUltimateTime = Time.time;
        isCastingUltimate = false;
        TriggerPostAttackDelay();
    }

    private void SpawnUltimateCircles()
    {
        int circleCount = Random.Range(currentPhaseSettings.ultimateCircleCountMin, currentPhaseSettings.ultimateCircleCountMax + 1);
        var spawned = new List<Vector3>();
        int maxAttempts = circleCount * 5;
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");

        for (int attempt = 0; attempt < maxAttempts && spawned.Count < circleCount; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle * ultimateSpawnRadius;
            Vector3 candidate = TargetPosition + new Vector3(rand.x, 0f, rand.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, NavMesh.AllAreas)) continue;

            Vector3 dir = navHit.position - TargetPosition;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(
                new Vector3(TargetPosition.x, TargetPosition.y + 0.1f, TargetPosition.z),
                dir.normalized, dist, wallMask)) continue;

            bool tooClose = false;
            foreach (var pos in spawned)
                if (Vector3.Distance(navHit.position, pos) < currentPhaseSettings.ultimateMinSpawnDistance) { tooClose = true; break; }
            if (tooClose) continue;

            SpawnAOEWarning(navHit.position, ultimateDamageScale, ultimateAoeRadius, ultimateWarningDuration);
            spawned.Add(navHit.position);
        }
    }


    private bool TrySummon()
    {
        if (isSummoning) return true;
        if (Time.time < lastSummonTime + summonCooldown) return false;

        StartCoroutine(SummonRoutine());
        return true;
    }

    private IEnumerator SummonRoutine()
    {
        isSummoning = true;
        movement.StopMoving();

        animator?.SetTrigger("SmashWindUp");
        yield return new WaitForSeconds(summonWindUpDuration);

        SpawnSummons();

        isSmashAnimFinished = false;
        animator?.SetTrigger("Smash");
        yield return new WaitUntil(() => isSmashAnimFinished);

        lastSummonTime = Time.time;
        isSummoning = false;
        TriggerPostAttackDelay();
    }

    private void SpawnSummons()
    {
        var spawned = new List<Vector3>();
        LayerMask wallMask = 1 << LayerMask.NameToLayer("Wall");
        int count = currentPhaseSettings.summonCount;
        int maxAttempts = count * 8;
        int spawnedCount = 0;

        for (int attempt = 0; attempt < maxAttempts && spawnedCount < count; attempt++)
        {
            Vector2 rand = Random.insideUnitCircle.normalized * Random.Range(summonMinRadius, summonRadius);
            Vector3 candidate = TargetPosition + new Vector3(rand.x, 0f, rand.y);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit navHit, 1f, NavMesh.AllAreas)) continue;

            Vector3 dir = navHit.position - TargetPosition;
            dir.y = 0f;
            float dist = dir.magnitude;
            if (dist > 0.01f && Physics.Raycast(
                new Vector3(TargetPosition.x, TargetPosition.y + 0.1f, TargetPosition.z),
                dir.normalized, dist, wallMask)) continue;

            bool tooClose = false;
            foreach (var pos in spawned)
                if (Vector3.Distance(navHit.position, pos) < summonMinSpawnDistance) { tooClose = true; break; }
            if (tooClose) continue;

            bool isElite = Random.value <= currentPhaseSettings.eliteRatio;
            bool isArc = Random.value <= arcWarlockRatio;

            GameObject prefab = isElite
                ? (isArc ? eliteArcWarlockPrefab : eliteWarlockPrefab)
                : (isArc ? normalArcWarlockPrefab : normalWarlockPrefab);

            if (prefab != null)
                Instantiate(prefab, navHit.position, Quaternion.identity);

            spawned.Add(navHit.position);
            spawnedCount++;
        }
    }


    public override void OnDeath()
    {
        base.OnDeath();
        StopAllCoroutines();
        isJumping = false;
        isCastingUltimate = false;
        isSummoning = false;

        foreach (var warlock in UnityEngine.Object.FindObjectsByType<WarlockController>(FindObjectsSortMode.None))
        {
            if ((object)warlock == this) continue;
            var h = warlock.GetComponent<EnemyHealthBase>();
            if (h != null && !h.IsDead) h.TakeDamage(h.MaxHP);
        }

        foreach (var node in UnityEngine.Object.FindObjectsByType<BigArcNode>(FindObjectsSortMode.None))
        {
            var h = node.GetComponent<BigArcNodeHealthBase>();
            if (h != null && !h.IsDead) h.TakeDamage(h.MaxHP);
        }
    }

    protected new void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, smashRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpTriggerRange);
    }
}