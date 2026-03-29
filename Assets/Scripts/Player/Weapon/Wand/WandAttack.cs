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

    [Header("Spawn")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Header("Status HUD")]
    [SerializeField] private Sprite totemCooldownIcon;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private WeaponEquip weaponEquip;
    private PlayerController controller;
    private List<Totem> activeTotems = new List<Totem>();

    private StatusEntry cooldownEntry;
    private const string COOLDOWN_ID = "wand_totem_cooldown";

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
        context = GetComponent<PlayerCombatContext>();
        weaponEquip = GetComponent<WeaponEquip>();
        controller = GetComponent<PlayerController>();
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
        if (cooldownEntry == null || PlayerStatusHUD.Instance == null) return;

        WeaponData weapon = weaponEquip?.GetCurrentWeapon();

        if (weapon == null || weapon.weaponType != WeaponType.Wand)
        {
            PlayerStatusHUD.Instance.Unregister(COOLDOWN_ID);
            return;
        }

        PlayerStatusHUD.Instance.Register(cooldownEntry);

        float cooldown = weapon.secondaryCooldown;
        float remaining = controller != null ? controller.SecondaryCooldownRemaining : 0f;
        float ratio = cooldown > 0f ? Mathf.Clamp01(remaining / cooldown) : 0f;

        cooldownEntry.sweepFill = ratio;
        cooldownEntry.isActive = remaining <= 0f;
        PlayerStatusHUD.Instance.Refresh(COOLDOWN_ID);
    }

    public void PlaceTotem()
    {
        if (totemPrefab == null) return;

        activeTotems.RemoveAll(t => t == null || t.IsDead);

        GameObject obj = Instantiate(totemPrefab, transform.position, Quaternion.identity);
        var totem = obj.GetComponent<Totem>();
        if (totem != null)
        {
            totem.Init(stats, context);
            activeTotems.Add(totem);
        }
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