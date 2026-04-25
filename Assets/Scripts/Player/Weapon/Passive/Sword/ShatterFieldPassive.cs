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
    [HideInInspector] public GameObject rootVFXPrefab;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private float baseRadius = 1f;

    private float cooldownTimer = 0f;
    private float Cooldown => shatterStrike ? 3f : 5f;
    private float FieldDuration => shatterStrike ? 5f : 3f;

    // slow state per enemy (shared across all zones)
    private const float slowAmount = -0.15f;
    private const int maxSlowStacks = 4;
    private const float slowDuration = 1.5f;
    private const float rootDuration = 3f;

    private const float BaseDamagePercent = 0.15f;
    private const float IntensifiedDamagePercent = 0.25f;
    private const float BaseTickInterval = 0.5f;
    private const float WideFieldRadiusMultiplier = 1.5f;
    private const float ExploitHealPercent = 0.02f;
    private const float RootMoveSpeedModifier = -1f;
    private const float BrittleDamageTakenBonus = 0.3f;

    public class EnemySlowState
    {
        public int stacks = 0;
        public EntityStatModifier currentModifier = new EntityStatModifier();
        public Coroutine expireCoroutine;
        public bool isRooted = false;
        public Coroutine rootCoroutine;
        public EntityStatModifier rootModifier = new EntityStatModifier();
        public EntityStatModifier brittleModifier = new EntityStatModifier();
        public GameObject rootVFX;
    }

    private Dictionary<HealthBase, EnemySlowState> slowStates
        = new Dictionary<HealthBase, EnemySlowState>();

    private HashSet<HealthBase> trackedForExploit = new HashSet<HealthBase>();

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext, WeaponData weaponData, float radiusMultiplier = 1f)
    {
        stats = playerStats;
        context = combatContext;
        context.OnSecondaryAttack += OnSecondaryAttack;
        context.OnSecondaryAttackForced += OnSecondaryAttackForced;
        context.OnEntityKilled += OnEnemyKilled;

        if (weaponData != null && weaponData.secondaryAttack != null)
            baseRadius = weaponData.secondaryAttack.range * radiusMultiplier;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.OnSecondaryAttack -= OnSecondaryAttack;
            context.OnSecondaryAttackForced -= OnSecondaryAttackForced;
            context.OnEntityKilled -= OnEnemyKilled;
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
        zone.damagePercent = intensifiedField ? IntensifiedDamagePercent : BaseDamagePercent;
        zone.tickInterval = BaseTickInterval;
        zone.duration = FieldDuration;
        zone.radius = wideField ? baseRadius * WideFieldRadiusMultiplier : baseRadius;
        zone.fieldCountsAsAttack = shatterStrike;
        zone.passive = this;
        zone.Init(stats, context);
    }

    public void ApplySlow(HealthBase enemy)
    {
        if (!deepChill) return;

        var entityStats = enemy.GetComponent<EntityStats>();

        if (!slowStates.ContainsKey(enemy))
            slowStates[enemy] = new EnemySlowState();

        var state = slowStates[enemy];

        if (state.isRooted)
        {
            if (state.rootCoroutine != null) StopCoroutine(state.rootCoroutine);
            state.rootCoroutine = StartCoroutine(RootExpire(enemy, state));
            return;
        }

        entityStats?.RemoveMultiplierModifier(state.currentModifier);

        state.stacks = Mathf.Min(state.stacks + 1, maxSlowStacks);
        state.currentModifier.moveSpeed = slowAmount * state.stacks;
        entityStats?.AddMultiplierModifier(state.currentModifier);

        if (state.expireCoroutine != null) StopCoroutine(state.expireCoroutine);
        state.expireCoroutine = StartCoroutine(SlowExpire(enemy, state));

        // L4 Shatter Point
        if (shatterPoint && state.stacks >= maxSlowStacks && !state.isRooted)
            ApplyRoot(enemy, state);

        // L5B Exploit
        if (exploit && state.isRooted && !trackedForExploit.Contains(enemy))
            trackedForExploit.Add(enemy);
    }

    public void CheckExploit()
    {
        if (!exploit) return;

        var toRemove = new List<HealthBase>();
        foreach (var enemy in trackedForExploit)
        {
            if (enemy == null || enemy.IsDead)
            {
                stats.HealPercent(ExploitHealPercent);
                Debug.Log("[ShatterField] Exploit: heal 2% max HP");
                toRemove.Add(enemy);
            }
        }
        foreach (var e in toRemove)
            trackedForExploit.Remove(e);
    }

    private void OnEnemyKilled(HealthBase enemy)
    {
        CheckExploit();
    }

    private void ApplyRoot(HealthBase enemy, EnemySlowState state)
    {
        state.isRooted = true;

        var entityStats = enemy?.GetComponent<EntityStats>();
        entityStats?.RemoveMultiplierModifier(state.currentModifier);
        if (state.expireCoroutine != null) StopCoroutine(state.expireCoroutine);
        state.stacks = 0;
        state.currentModifier.moveSpeed = 0f;
        state.expireCoroutine = null;

        state.rootModifier.moveSpeed = RootMoveSpeedModifier;
        entityStats?.AddMultiplierModifier(state.rootModifier);

        if (brittle)
        {
            state.brittleModifier.damageTaken = BrittleDamageTakenBonus;
            entityStats?.AddMultiplierModifier(state.brittleModifier);
        }

        if (state.rootCoroutine != null) StopCoroutine(state.rootCoroutine);
        state.rootCoroutine = StartCoroutine(RootExpire(enemy, state));
        // Spawn root VFX
        if (rootVFXPrefab != null && enemy != null)
        {
            var enemyHealth = enemy as EnemyHealthBase;
            Vector3 spawnPos = (enemyHealth?.groundPoint != null)
                ? enemyHealth.groundPoint.position
                : enemy.transform.position;
            state.rootVFX = Instantiate(rootVFXPrefab, enemy.transform);
            state.rootVFX.transform.position = spawnPos + new Vector3(0f, 0f, -0.01f);
            Vector3 ps = enemy.transform.lossyScale;
            state.rootVFX.transform.localScale = new Vector3(1f / ps.x, 1f / ps.y, 1f / ps.z);
        }
    }

    private IEnumerator SlowExpire(HealthBase enemy, EnemySlowState state)
    {
        yield return new WaitForSeconds(slowDuration);

        if (enemy != null)
        {
            var entityStats = enemy.GetComponent<EntityStats>();
            entityStats?.RemoveMultiplierModifier(state.currentModifier);
        }

        state.stacks = 0;
        state.currentModifier.moveSpeed = 0f;
        state.expireCoroutine = null;
        slowStates.Remove(enemy);
    }

    private IEnumerator RootExpire(HealthBase enemy, EnemySlowState state)
    {
        yield return new WaitForSeconds(rootDuration);

        if (enemy != null)
        {
            var entityStats = enemy.GetComponent<EntityStats>();
            entityStats?.RemoveMultiplierModifier(state.rootModifier);
            if (brittle)
                entityStats?.RemoveMultiplierModifier(state.brittleModifier);
        }

        state.rootModifier.moveSpeed = 0f;
        state.brittleModifier.damageTaken = 0f;
        state.isRooted = false;
        trackedForExploit.Remove(enemy);
        if (state.rootVFX != null)
        {
            Destroy(state.rootVFX);
            state.rootVFX = null;
        }
    }
}