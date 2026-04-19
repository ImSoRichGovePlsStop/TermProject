using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoulSiphonPassive : MonoBehaviour
{
    public SoulSiphonModule Module;

    public float attackBuffPerEnemy { get; set; }
    public float burstRange { get; set; }
    public float buffDuration { get; set; }

    private PlayerStats _stats;
    private LayerMask _layerMask;

    private float _cooldownTimer = 0f;
    private Coroutine _buffCoroutine;

    private GameObject _activeIndicator;

    public void Init(PlayerStats playerStats, LayerMask layerMask)
    {
        _stats = playerStats;
        _layerMask = layerMask;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        HandleInput();
    }

    private void OnDisable()
    {
        if (_activeIndicator != null)
        {
            Destroy(_activeIndicator);
            _activeIndicator = null;
        }
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
        if (_cooldownTimer > 0f) return;
        if (_stats == null || Module == null) return;

        StartCoroutine(SiphonCoroutine());
    }

    private IEnumerator SiphonCoroutine()
    {
        _cooldownTimer = Module.moduleCooldown;

        if (Module.siphonVFXPrefab != null)
        {
            var vfxGO = Instantiate(Module.siphonVFXPrefab, transform.position, Quaternion.identity);
            vfxGO.GetComponent<SiphonVFX>()?.Init(burstRange);
        }

        var hits = Physics.OverlapSphere(transform.position, burstRange, _layerMask.value);

        int enemiesHit = 0;
        foreach (var col in hits)
        {
            var health = col.GetComponent<HealthBase>();
            if (health == null || health.IsDead) continue;

            float drain = health.CurrentHP * Module.enemyHpDrainPercent;
            health.TakeDamage(drain, false);
            enemiesHit++;
        }

        if (enemiesHit > 0)
        {
            float totalBuff = attackBuffPerEnemy * enemiesHit;
            ApplyAttackBuff(totalBuff, enemiesHit);
        }

        yield return null;
    }

    private void ApplyAttackBuff(float buffPercent, int enemiesHit)
    {
        if (_buffCoroutine != null)
            StopCoroutine(_buffCoroutine);

        _buffCoroutine = StartCoroutine(BuffCoroutine(buffPercent, enemiesHit));
    }

    private IEnumerator BuffCoroutine(float buffPercent, int enemiesHit)
    {
        _stats.AddMultiplierModifier(new StatModifier { damage = buffPercent });

        if (_activeIndicator != null)
            Destroy(_activeIndicator);

        if (Module.buffIndicatorPrefab != null)
        {
            _activeIndicator = Instantiate(
                Module.buffIndicatorPrefab,
                transform.position,
                Quaternion.identity,
                transform
            );
            _activeIndicator.GetComponent<SiphonBuffIndicator>()?.Init(enemiesHit, buffDuration);
        }

        yield return new WaitForSeconds(buffDuration);

        _stats.RemoveMultiplierModifier(new StatModifier { damage = buffPercent });

        _activeIndicator = null;
        _buffCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, burstRange);
    }
}