using UnityEngine;

public enum WeaponType
{
    Sword,
    Wand,
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "Weapon/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("Info")]
    public string weaponName;
    public Sprite icon;

    [Header("Weapon Type")]
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Animation")]
    public AnimatorOverrideController animatorOverrideController;

    [Header("Passive")]
    public WeaponPassiveData passiveData;

    [Header("Base Stats")]
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

    [Header("Attack")]
    public ComboHit[] combo;
    public ComboHit secondaryAttack;
    public float comboCooldown;
    public float secondaryCooldown;
    public float comboResetTime;

    [Header("Wand Projectile")]
    public WandProjectileData wandProjectile;

    [Header("Grid")]
    public Vector2Int[] gridSizePerLevel;

    public Vector2Int GetGridSize(int level)
    {
        if (gridSizePerLevel == null || gridSizePerLevel.Length == 0)
            return new Vector2Int(5, 5);
        int index = Mathf.Clamp(level - 1, 0, gridSizePerLevel.Length - 1);
        return gridSizePerLevel[index];
    }

    private void OnValidate()
    {
        if (gridSizePerLevel == null || gridSizePerLevel.Length != 15)
        {
            var old = gridSizePerLevel ?? new Vector2Int[0];
            gridSizePerLevel = new Vector2Int[15];
            for (int i = 0; i < 15; i++)
                gridSizePerLevel[i] = i < old.Length ? old[i] : new Vector2Int(5, 5);
        }
    }
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

// Wand-specific projectile settings
[System.Serializable]
public class WandProjectileData
{
    [Header("Normal Projectile (Hits 1-4)")]
    public float normalMaxSpeed = 12f;
    public float normalRange = 6f;
    public float normalLifetime = 3f;
    public float normalColliderRadius = 0.15f;

    [Header("Big Projectile (Hit 5)")]
    public float bigMaxSpeed = 7f;
    public float bigRange = 6f;
    public float bigLifetime = 5f;
    public float bigSpriteScale = 1.8f;

    [Header("Deceleration")]
    public float easePower = 2f;

    [Header("Sprite Animation")]
    [Range(0f, 1f)]
    public float slowThreshold = 0.4f;
    public AnimationClip fastClip;
    public AnimationClip slowClip;

    [Header("AoE Pulse (Hit 5)")]
    public float aoePulseDamageScale = 0.30f;
    public float aoePulseRadius = 1.8f;
    public float aoePulseInterval = 1f;
}