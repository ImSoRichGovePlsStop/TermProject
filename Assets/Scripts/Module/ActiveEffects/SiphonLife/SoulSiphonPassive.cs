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
    private float _buffTimer = 0f;
    private int _buffEnemyCount = 0;
    private float _flashTimer = 0f;
    private Coroutine _buffCoroutine;

    private const float CooldownFlashDuration = 1f;

    private bool BuffActive => _buffTimer > 0f;
    private bool OnCooldown => _cooldownTimer > 0f;
    private bool FlashActive => _flashTimer > 0f;

    private SiphonBuffIndicator _indicator;

    public void Init(PlayerStats playerStats, LayerMask layerMask)
    {
        _stats = playerStats;
        _layerMask = layerMask;

        SpawnIndicator();
    }

    private void Update()
    {
        if (_cooldownTimer > 0f) _cooldownTimer -= Time.deltaTime;
        if (_buffTimer > 0f) _buffTimer -= Time.deltaTime;
        if (_flashTimer > 0f) _flashTimer -= Time.deltaTime;

        HandleInput();
        UpdateIndicator();
    }

    private void OnDisable()
    {
        if (_indicator != null)
        {
            Destroy(_indicator.gameObject);
            _indicator = null;
        }
    }

    private void SpawnIndicator()
    {
        if (Module.buffIndicatorPrefab == null) return;

        if (_indicator != null)
            Destroy(_indicator.gameObject);

        var go = Instantiate(Module.buffIndicatorPrefab, transform.position, Quaternion.identity, transform);
        _indicator = go.GetComponent<SiphonBuffIndicator>();
        _indicator?.Init();
    }

    private void UpdateIndicator()
    {
        if (_indicator == null) return;

        if (BuffActive)
        {
            float ratio = Mathf.Clamp01(_buffTimer / buffDuration);
            _indicator.SetBuff(ratio);
        }
        else if (OnCooldown && FlashActive)
        {
            float ratio = Mathf.Clamp01(_cooldownTimer / Module.moduleCooldown);
            _indicator.SetCooldown(ratio);
        }
        else
        {
            _indicator.Hide();
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

        if (!kb[Key.Q].wasPressedThisFrame) return;
        if (!enabled || _stats == null || Module == null) return;

        if (OnCooldown)
        {
            float ratio = Mathf.Clamp01(_cooldownTimer / Module.moduleCooldown);
            _indicator?.ShowCooldown(ratio);
            _flashTimer = CooldownFlashDuration;
        }
        else
        {
            StartCoroutine(SiphonCoroutine());
        }
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

        _buffEnemyCount = enemiesHit;
        _buffTimer = buffDuration;
        _indicator?.ShowBuff(1f, enemiesHit);

        if (enemiesHit > 0)
            ApplyAttackBuff(attackBuffPerEnemy * enemiesHit);

        yield return null;
    }

    private void ApplyAttackBuff(float buffPercent)
    {
        if (_buffCoroutine != null)
            StopCoroutine(_buffCoroutine);

        _buffCoroutine = StartCoroutine(BuffCoroutine(buffPercent));
    }

    private IEnumerator BuffCoroutine(float buffPercent)
    {
        _stats.AddMultiplierModifier(new StatModifier { damage = buffPercent });

        yield return new WaitForSeconds(buffDuration);

        _stats.RemoveMultiplierModifier(new StatModifier { damage = buffPercent });
        _buffCoroutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, burstRange);
    }
}