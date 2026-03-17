using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;

    [Header("Passive")]
    public WeaponPassiveData passiveData;

    public float health;
    public float damage;
    public float attackSpeed;
    public float moveSpeed;
    public float critChance;
    public float critDamage;
    public float evadeChance;
    public float damageTaken;

    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;

    public ComboHit[] combo;

    public ComboHit secondaryAttack;

    public float comboCooldown;
    public float secondaryCooldown;
    public float comboResetTime;
}

[System.Serializable]
public class ComboHit
{
    [Header("Damage")]
    public float damageScale;

    [Header("Hitbox Shape")]
    public float range;
    public float angle;
    public float extraRange;

    [Header("Animation")]
    public string animationTrigger;
    public float animationDuration;

    [Header("VFX")]
    public GameObject hitVFXPrefab;
    public float vfxOffset;
    public float vfxDurationMultiplier = 1f;
    public float vfxLoops = 1f;
}