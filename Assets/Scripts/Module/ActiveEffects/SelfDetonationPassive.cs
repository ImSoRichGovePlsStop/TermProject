using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelfDetonationPassive : MonoBehaviour
{
    public SelfDetonationModule SelfDetonationModule;

    public float attackPercent { get; set; }
    public float hpPercent { get; set; }
    public float burstRadius { get; set; }

    private PlayerStats _stats;
    private PlayerCombatContext _context;

    private bool _isArmed = false;
    private float _cooldownTimer = 0f;

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        _stats = playerStats;
        _context = combatContext;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        HandleInput();
    }

    private void HandleInput()
    {
        var ui = UIManager.Instance;
        if (ui != null)
        {
            if (ui.IsInventoryOpen) return;
            if (ui.IsPassiveOpen) return;
            if (ui.IsShopOpen) return;
            if (ui.IsMergeOpen) return;
            if (ui.IsUpgradeOpen) return;
            if (ui.IsGamblerOpen) return;
            if (ui.IsStorageOpen) return;
            if (ui.IsCardPhaseOpen) return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb[Key.Q].wasPressedThisFrame)
            TryActivate();
    }

    private void TryActivate()
    {
        if (!enabled) return;
        if (_isArmed) return;
        if (_cooldownTimer > 0f) return;
        if (_stats == null || SelfDetonationModule == null) return;

        float hpCost = _stats.MaxHealth * SelfDetonationModule.activationHpCostPercent;

        if (_stats.CurrentHealth <= hpCost + SelfDetonationModule.minHpBuffer)
        {
            Debug.Log("[SelfDetonation] Not enough HP to activate.");
            return;
        }

        PayHpCost(hpCost);
        StartCoroutine(CountdownCoroutine());
    }

    private void PayHpCost(float cost)
    {
        float raw = cost / Mathf.Max(_stats.DamageTaken, 0.001f);
        _stats.TakeDamage(raw, null);
    }

    private IEnumerator CountdownCoroutine()
    {
        _isArmed = true;

        GameObject countdownFX = null;
        DetnationVFX detonationVFX = null;

        var prefab = SelfDetonationModule.countdownVFXPrefab;
        if (prefab != null)
        {
            countdownFX = Instantiate(prefab, transform.position, Quaternion.identity, transform);
            detonationVFX = countdownFX.GetComponent<DetnationVFX>();
            detonationVFX?.Init(SelfDetonationModule.countdownDuration, burstRadius);
        }

        yield return new WaitForSeconds(SelfDetonationModule.countdownDuration);

        if (countdownFX != null)
            Destroy(countdownFX);

        if (_stats == null || _stats.IsDead)
        {
            _isArmed = false;
            yield break;
        }

        Detonate();

        _isArmed = false;
        _cooldownTimer = SelfDetonationModule.moduleCooldown;
    }

    private void Detonate()
    {
        if (_stats == null || SelfDetonationModule == null) return;

        float attackDamage = _stats.CalculateDamage(attackPercent);
        bool wasCrit = _stats.LastHitWasCrit;

        float hpDamage = hpPercent * _stats.MaxHealth;
        float totalDamage = attackDamage + hpDamage;

        var burstVFXPrefab = SelfDetonationModule.burstVFXPrefab;
        if (burstVFXPrefab != null)
            Instantiate(burstVFXPrefab, transform.position, Quaternion.identity);

        DamageEnemiesInRadius(totalDamage, wasCrit);

        _stats.HealPercent(SelfDetonationModule.healBackPercent);
    }

    private void DamageEnemiesInRadius(float damage, bool isCrit)
    {
        var hits = Physics.OverlapSphere(transform.position, burstRadius);
        foreach (var col in hits)
        {
            var health = col.GetComponent<HealthBase>();
            if (health == null || health.IsDead) continue;
            health.TakeDamage(damage, isCrit);
        }
    }
}