using UnityEngine;

public class WeaponEquip : MonoBehaviour
{
    [SerializeField] private WeaponData startingWeapon;

    private WeaponData currentWeapon;
    private PlayerStats stats;
    private AttackHitbox hitbox;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        hitbox = GetComponentInChildren<AttackHitbox>();
    }

    void Start()
    {
        if (startingWeapon != null)
            Equip(startingWeapon);
    }

    public void Equip(WeaponData weapon)
    {
        currentWeapon = weapon;
        stats.ApplyWeapon(weapon);
        Debug.Log($"Equipped {weapon.weaponName}");
    }

    public void Unequip()
    {
        currentWeapon = null;
        stats.ApplyWeapon(null);
        Debug.Log("Unequipped weapon");
    }

    public WeaponData GetCurrentWeapon() => currentWeapon;
}