using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;

    [Header("Passive")]
    public WeaponPassiveData passiveData;

    [Header("Stats")]
    public float health;
    public float damage;
    public float attackSpeed;
    public float moveSpeed;
    public float critChance;
    public float critDamage;
    public float evadeChance;
    public float damageTaken;

    [Header("Dash")]
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;

    [Header("Primary Combo")]
    public ComboHit[] combo;

    [Header("Secondary Attack")]
    public ComboHit secondaryAttack;

    [Header("Attack Cooldowns")]
    public float comboCooldown;
    public float secondaryCooldown;
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
}