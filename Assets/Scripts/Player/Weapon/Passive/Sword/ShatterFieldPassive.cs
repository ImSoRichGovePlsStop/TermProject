using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShatterFieldPassive : MonoBehaviour
{
    public bool deepChill = false;
    public bool intensifiedField = false;
    public bool wideField = false;
    public bool shatterPoint = false;
    public bool brittle = false;
    public bool exploit = false;
    public bool shatterStrike = false;

    [HideInInspector] public ShatterFieldZone fieldPrefab;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private float baseRadius = 1f;

    private float cooldownTimer = 0f;
    private float Cooldown => shatterStrike ? 1.5f : 3f;
    private float FieldDuration => shatterStrike ? 5f : 3f;

    // slow state per enemy (shared across all zones)
    private const float slowAmount = -0.15f;
    private const int maxSlowStacks = 4;
    private const float slowDuration = 1.5f;
    private const float rootDuration = 3f;

    public class EnemySlowState
    {
        public int stacks = 0;
        public EnemyStatModifier currentModifier = new EnemyStatModifier();
        public Coroutine expireCoroutine;
        public bool isRooted = false;
        public Coroutine rootCoroutine;
        public EnemyStatModifier rootModifier = new EnemyStatModifier();
        public EnemyStatModifier brittleModifier = new EnemyStatModifier();
    }

    private Dictionary<EnemyHealth, EnemySlowState> slowStates
        = new Dictionary<EnemyHealth, EnemySlowState>();

    private HashSet<EnemyHealth> trackedForExploit = new HashSet<EnemyHealth>();

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext, WeaponData weaponData)
    {
        stats = playerStats;
        context = combatContext;
        context.OnSecondaryAttack += OnSecondaryAttack;
        context.OnSecondaryAttackForced += OnSecondaryAttackForced;
        context.OnEnemyKilled += OnEnemyKilled;

        if (weaponData != null && weaponData.secondaryAttack != null)
            baseRadius = weaponData.secondaryAttack.range;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.OnSecondaryAttack -= OnSecondaryAttack;
            context.OnSecondaryAttackForced -= OnSecondaryAttackForced;
            context.OnEnemyKilled -= OnEnemyKilled;
        }
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void OnSecondaryAttack()
    {
        if (!enabled) return;
        if (cooldownTimer > 0f) return;
        if (fieldPrefab == null) return;

        SpawnField(context.LastSecondaryPosition);
        cooldownTimer = Cooldown;
    }

    private void OnSecondaryAttackForced()
    {
        if (!enabled) return;
        if (fieldPrefab == null) return;
        SpawnField(context.LastSecondaryPosition);
    }

    private void SpawnField(Vector3 position)
    {
        Vector3 spawnPos = new Vector3(position.x, 0.01f, position.z);
        var zone = Instantiate(fieldPrefab, spawnPos, Quaternion.identity);
        zone.damagePercent = intensifiedField ? 0.25f : 0.15f;
        zone.tickInterval = 0.5f;
        zone.duration = FieldDuration;
        zone.radius = wideField ? baseRadius * 1.5f : baseRadius;
        zone.fieldCountsAsAttack = shatterStrike;
        zone.passive = this;
        zone.Init(stats, context);
    }

    public void ApplySlow(EnemyHealth enemy)
    {
        if (!deepChill) return;

        var status = enemy.GetComponent<EnemyStatusHandler>();
        if (status == null) return;

        if (!slowStates.ContainsKey(enemy))
            slowStates[enemy] = new EnemySlowState();

        var state = slowStates[enemy];

        if (state.isRooted)
        {
            if (state.rootCoroutine != null) StopCoroutine(state.rootCoroutine);
            state.rootCoroutine = StartCoroutine(RootExpire(enemy, status, state));
            return;
        }

        status.RemoveMultiplierModifier(state.currentModifier);

        state.stacks = Mathf.Min(state.stacks + 1, maxSlowStacks);
        state.currentModifier.moveSpeed = slowAmount * state.stacks;
        status.AddMultiplierModifier(state.currentModifier);

        if (state.expireCoroutine != null) StopCoroutine(state.expireCoroutine);
        state.expireCoroutine = StartCoroutine(SlowExpire(enemy, status, state));

        // L4 Shatter Point
        if (shatterPoint && state.stacks >= maxSlowStacks && !state.isRooted)
            ApplyRoot(enemy, status, state);

        // L5B Exploit
        if (exploit && state.isRooted && !trackedForExploit.Contains(enemy))
            trackedForExploit.Add(enemy);
    }

    public void CheckExploit()
    {
        if (!exploit) return;

        var toRemove = new List<EnemyHealth>();
        foreach (var enemy in trackedForExploit)
        {
            if (enemy == null || enemy.IsDead)
            {
                stats.HealPercent(0.02f);
                Debug.Log("[ShatterField] Exploit: heal 2% max HP");
                toRemove.Add(enemy);
            }
        }
        foreach (var e in toRemove)
            trackedForExploit.Remove(e);
    }

    private void OnEnemyKilled(EnemyHealth enemy)
    {
        CheckExploit();
    }

    private void ApplyRoot(EnemyHealth enemy, EnemyStatusHandler status, EnemySlowState state)
    {
        state.isRooted = true;

        status.RemoveMultiplierModifier(state.currentModifier);
        if (state.expireCoroutine != null) StopCoroutine(state.expireCoroutine);
        state.stacks = 0;
        state.currentModifier.moveSpeed = 0f;
        state.expireCoroutine = null;

        state.rootModifier.moveSpeed = -1f;
        status.AddMultiplierModifier(state.rootModifier);

        if (brittle)
        {
            state.brittleModifier.damageTaken = 0.3f;
            status.AddMultiplierModifier(state.brittleModifier);
        }

        if (state.rootCoroutine != null) StopCoroutine(state.rootCoroutine);
        state.rootCoroutine = StartCoroutine(RootExpire(enemy, status, state));
    }

    private IEnumerator SlowExpire(EnemyHealth enemy, EnemyStatusHandler status, EnemySlowState state)
    {
        yield return new WaitForSeconds(slowDuration);

        if (status != null)
            status.RemoveMultiplierModifier(state.currentModifier);

        state.stacks = 0;
        state.currentModifier.moveSpeed = 0f;
        state.expireCoroutine = null;
        slowStates.Remove(enemy);
    }

    private IEnumerator RootExpire(EnemyHealth enemy, EnemyStatusHandler status, EnemySlowState state)
    {
        yield return new WaitForSeconds(rootDuration);

        if (status != null)
        {
            status.RemoveMultiplierModifier(state.rootModifier);
            if (brittle)
                status.RemoveMultiplierModifier(state.brittleModifier);
        }

        state.rootModifier.moveSpeed = 0f;
        state.brittleModifier.damageTaken = 0f;
        state.isRooted = false;
        trackedForExploit.Remove(enemy);
    }
}