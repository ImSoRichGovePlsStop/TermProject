using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
[RequireComponent(typeof(PlayerCombatContext))]
public class WandAttack : MonoBehaviour
{
    [Header("Projectile Prefabs")]
    [SerializeField] private GameObject normalProjectilePrefab;
    [SerializeField] private GameObject bigProjectilePrefab;

    [Header("Totem")]
    [SerializeField] private GameObject totemPrefab;
    [SerializeField] private float totemCooldown = 20f;
    [SerializeField] private float interChargeDelay = 3f;
    [SerializeField] private int maxCharges = 1;

    [Header("Spawn")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Status HUD")]
    [SerializeField] private Sprite totemCooldownIcon;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private WeaponEquip weaponEquip;
    private List<Totem> activeTotems = new List<Totem>();
    private int currentCharges = 1;
    private float chargeCooldownRemaining = 0f;
    private float interChargeDelayRemaining = 0f;

    public float TotemCooldown => totemCooldown;
    public float ChargeCooldownRemaining => chargeCooldownRemaining;
    public int CurrentCharges => currentCharges;
    public int MaxCharges => maxCharges;
    public bool IsTotemReady => currentCharges > 0 && interChargeDelayRemaining <= 0f;

    private StatusEntry cooldownEntry;
    private const string COOLDOWN_ID = "wand_totem_cooldown";

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        context = GetComponent<PlayerCombatContext>();
        weaponEquip = GetComponent<WeaponEquip>();
        currentCharges = maxCharges;
    }

    private void Start()
    {
        cooldownEntry = new StatusEntry(COOLDOWN_ID, totemCooldownIcon);
        cooldownEntry.sweepClockwise = false;
        cooldownEntry.isActive = false;
        PlayerStatusHUD.Instance?.Register(cooldownEntry);
    }

    private void OnDestroy()
    {
        PlayerStatusHUD.Instance?.Unregister(COOLDOWN_ID);
    }

    private void Update()
    {
        if (interChargeDelayRemaining > 0f)
            interChargeDelayRemaining -= Time.deltaTime;

        if (chargeCooldownRemaining > 0f)
        {
            chargeCooldownRemaining -= Time.deltaTime;
            if (chargeCooldownRemaining <= 0f)
            {
                currentCharges++;
                chargeCooldownRemaining = currentCharges < maxCharges ? totemCooldown : 0f;
            }
        }

        UpdateHUD();
    }

    private bool showChargeCount = false;
    private float innerBorderFill = 0f;
    private bool showInnerBorder = false;

    public void SetShowChargeCount(bool show) => showChargeCount = show;

    public void SetInnerBorder(float fill, bool show)
    {
        innerBorderFill = fill;
        showInnerBorder = show;
    }

    private void UpdateHUD()
    {
        if (cooldownEntry == null || PlayerStatusHUD.Instance == null) return;
        WeaponData weapon = weaponEquip?.GetCurrentWeapon();
        if (weapon == null || weapon.weaponType != WeaponType.Wand)
        {
            PlayerStatusHUD.Instance.Unregister(COOLDOWN_ID);
            return;
        }
        PlayerStatusHUD.Instance.Register(cooldownEntry);
        cooldownEntry.isActive = currentCharges > 0;
        cooldownEntry.count = showChargeCount ? currentCharges : 0;
        cooldownEntry.showInnerBorder = showInnerBorder;
        cooldownEntry.innerFill = innerBorderFill;
        cooldownEntry.innerFillClockwise = true;
        cooldownEntry.innerBorderColor = new Color(0.2f, 0.6f, 1f);
        if (interChargeDelayRemaining > 0f && currentCharges > 0)
            cooldownEntry.sweepFill = interChargeDelayRemaining / interChargeDelay;
        else
            cooldownEntry.sweepFill = totemCooldown > 0f ? Mathf.Clamp01(chargeCooldownRemaining / totemCooldown) : 0f;
        PlayerStatusHUD.Instance.Refresh(COOLDOWN_ID);
    }

    public void ReduceTotemCooldown(float percent)
    {
        chargeCooldownRemaining *= (1f - percent);
    }

    public void ReduceTotemCooldownSeconds(float seconds)
    {
        if (currentCharges >= maxCharges) return;
        chargeCooldownRemaining -= seconds;
        if (chargeCooldownRemaining <= 0f)
        {
            currentCharges++;
            chargeCooldownRemaining = currentCharges < maxCharges ? totemCooldown : 0f;
        }
    }

    public static event Action<Totem> OnTotemSpawned;
    private float totemStartHpPercent = 1f;

    public void SetMaxCharges(int max)
    {
        int prev = maxCharges;
        maxCharges = max;
        currentCharges = Mathf.Min(currentCharges, maxCharges);

        if (maxCharges > prev && currentCharges < maxCharges && chargeCooldownRemaining <= 0f)
            chargeCooldownRemaining = totemCooldown;
    }

    public void SetTotemStartHpPercent(float percent) => totemStartHpPercent = percent;

    public void PlaceTotem()
    {
        if (totemPrefab == null || !IsTotemReady) return;
        activeTotems.RemoveAll(t => t == null || t.IsDead);
        GameObject obj = Instantiate(totemPrefab, transform.position, Quaternion.identity);
        var totem = obj.GetComponent<Totem>();
        if (totem != null)
        {
            OnTotemSpawned?.Invoke(totem);
            totem.startHpPercent = totemStartHpPercent;
            totem.Init(stats, context);
            activeTotems.Add(totem);
        }
        currentCharges--;
        interChargeDelayRemaining = interChargeDelay;
        if (chargeCooldownRemaining <= 0f)
            chargeCooldownRemaining = totemCooldown;
    }

    public List<Totem> GetActiveTotems() => activeTotems;

    public void FireProjectile(ComboHit hit, int comboIndex, Vector3 direction)
    {
        if (hit == null) return;
        WeaponData weapon = weaponEquip.GetCurrentWeapon();
        if (weapon == null || weapon.wandProjectile == null)
        {
            Debug.LogWarning("WandAttack: wandProjectile is not set in WeaponData.");
            return;
        }
        WandProjectileData data = weapon.wandProjectile;
        bool isBig = (comboIndex == 4);
        GameObject prefab = isBig ? bigProjectilePrefab : normalProjectilePrefab;
        if (prefab == null)
        {
            Debug.LogWarning($"WandAttack: prefab for hit {comboIndex + 1} is not assigned.");
            return;
        }
        Vector3 spawnPos = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.position;
        float camTiltX = -Camera.main.transform.rotation.eulerAngles.x;
        Quaternion spawnRot = Quaternion.Euler(camTiltX, 0f, 0f);
        GameObject proj = Instantiate(prefab, spawnPos, spawnRot);
        AudioManager.Instance?.PlayWandAttack();
        if (isBig)
            proj.transform.localScale = normalProjectilePrefab.transform.localScale * data.bigSpriteScale;
        var wandProj = proj.GetComponent<WandProjectile>();
        if (wandProj != null)
        {
            wandProj.damage = stats.CalculateDamage(hit.damageScale);
            wandProj.isCrit = stats.LastHitWasCrit;
            wandProj.moveDirection = direction.normalized;
            wandProj.maxSpeed = isBig ? data.bigMaxSpeed : data.normalMaxSpeed;
            wandProj.maxRange = isBig ? data.bigRange : data.normalRange;
            wandProj.lifetime = isBig ? data.bigLifetime : data.normalLifetime;
            wandProj.colliderRadius = isBig ? data.normalColliderRadius * data.bigSpriteScale : data.normalColliderRadius;
            wandProj.easePower = data.easePower;
            wandProj.slowThreshold = data.slowThreshold;
            wandProj.fastClip = data.fastClip;
            wandProj.slowClip = data.slowClip;
            wandProj.alwaysSlowClip = isBig;
            wandProj.shooterStats = stats;
            wandProj.context = context;
            wandProj.comboIndex = comboIndex;
        }
        if (isBig)
        {
            var pulse = proj.GetComponent<WandAoEPulse>();
            if (pulse != null)
            {
                pulse.pulseDamageScale = data.aoePulseDamageScale;
                pulse.pulseRadius = data.aoePulseRadius;
                pulse.pulseInterval = data.aoePulseInterval;
                pulse.shooterStats = stats;
                pulse.context = context;
            }
        }
    }
}