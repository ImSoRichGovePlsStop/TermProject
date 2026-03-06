using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Default Stats")]
    [SerializeField] private float defaultHealth = 100f;
    [SerializeField] private float defaultMoveSpeed = 5f;

    private float weaponHealth;
    private float weaponDamage;
    private float weaponAttackSpeed;
    private float weaponMoveSpeed;
    private float weaponCritChance;
    private float weaponCritDamage;
    private float weaponEvadeChance;

    private StatBonus additiveBonus = new StatBonus();

    private StatBonus multiplierBonus = new StatBonus();

    public float MaxHealth =>
        (weaponHealth + additiveBonus.health) * (1 + multiplierBonus.health);
    public float Damage =>
        (weaponDamage + additiveBonus.damage) * (1 + multiplierBonus.damage);
    public float AttackSpeed =>
        (weaponAttackSpeed + additiveBonus.attackSpeed) * (1 + multiplierBonus.attackSpeed);
    public float MoveSpeed =>
        (weaponMoveSpeed + additiveBonus.moveSpeed) * (1 + multiplierBonus.moveSpeed);
    public float CritChance =>
        (weaponCritChance + additiveBonus.critChance) * (1 + multiplierBonus.critChance);
    public float CritDamage =>
        (weaponCritDamage + additiveBonus.critDamage) * (1 + multiplierBonus.critDamage);
    public float EvadeChance =>
        (weaponEvadeChance + additiveBonus.evadeChance) * (1 + multiplierBonus.evadeChance);

    public float CurrentHealth { get; private set; }

    void Awake() => ApplyDefault();

    void ApplyDefault()
    {
        weaponHealth = defaultHealth;
        weaponMoveSpeed = defaultMoveSpeed;
        weaponDamage = 0;
        weaponAttackSpeed = 0;
        weaponCritChance = 0;
        weaponCritDamage = 1f;
        weaponEvadeChance = 0;
        CurrentHealth = MaxHealth;
    }

    public void ApplyWeapon(WeaponData weapon)
    {
        if (weapon == null) { ApplyDefault(); return; }

        weaponHealth = weapon.health;
        weaponDamage = weapon.damage;
        weaponAttackSpeed = weapon.attackSpeed;
        weaponMoveSpeed = weapon.moveSpeed;
        weaponCritChance = weapon.critChance;
        weaponCritDamage = weapon.critDamage;
        weaponEvadeChance = weapon.evadeChance;
        CurrentHealth = MaxHealth;

        Debug.Log($"Stats applied from {weapon.weaponName}");
    }

    public void AddAdditiveBonus(StatBonus bonus)
    {
        additiveBonus.health += bonus.health;
        additiveBonus.damage += bonus.damage;
        additiveBonus.attackSpeed += bonus.attackSpeed;
        additiveBonus.moveSpeed += bonus.moveSpeed;
        additiveBonus.critChance += bonus.critChance;
        additiveBonus.critDamage += bonus.critDamage;
        additiveBonus.evadeChance += bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void AddMultiplierBonus(StatBonus bonus)
    {
        multiplierBonus.health += bonus.health;
        multiplierBonus.damage += bonus.damage;
        multiplierBonus.attackSpeed += bonus.attackSpeed;
        multiplierBonus.moveSpeed += bonus.moveSpeed;
        multiplierBonus.critChance += bonus.critChance;
        multiplierBonus.critDamage += bonus.critDamage;
        multiplierBonus.evadeChance += bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveAdditiveBonus(StatBonus bonus)
    {
        additiveBonus.health -= bonus.health;
        additiveBonus.damage -= bonus.damage;
        additiveBonus.attackSpeed -= bonus.attackSpeed;
        additiveBonus.moveSpeed -= bonus.moveSpeed;
        additiveBonus.critChance -= bonus.critChance;
        additiveBonus.critDamage -= bonus.critDamage;
        additiveBonus.evadeChance -= bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveMultiplierBonus(StatBonus bonus)
    {
        multiplierBonus.health -= bonus.health;
        multiplierBonus.damage -= bonus.damage;
        multiplierBonus.attackSpeed -= bonus.attackSpeed;
        multiplierBonus.moveSpeed -= bonus.moveSpeed;
        multiplierBonus.critChance -= bonus.critChance;
        multiplierBonus.critDamage -= bonus.critDamage;
        multiplierBonus.evadeChance -= bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        Debug.Log($"Healed {amount}, HP: {CurrentHealth}/{MaxHealth}");
    }

    public void HealPercent(float percent)
    {
        float amount = MaxHealth * percent;
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        Debug.Log($"Healed {percent * 100}%, HP: {CurrentHealth}/{MaxHealth}");
    }

    public void HealFull()
    {
        CurrentHealth = MaxHealth;
        Debug.Log($"Fully healed, HP: {CurrentHealth}/{MaxHealth}");
    }

    public void ResetBonuses()
    {
        additiveBonus = new StatBonus();
        multiplierBonus = new StatBonus();
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (Random.value < EvadeChance)
        {
            Debug.Log("Evaded!");
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        Debug.Log($"Took {amount} damage, HP: {CurrentHealth}/{MaxHealth}");

        if (CurrentHealth <= 0)
            Debug.Log("Player died!");
    }

    public float CalculateDamage(float damageScale)
    {
        float dmg = Damage * damageScale;

        if (Random.value < CritChance)
        {
            dmg *= CritDamage;
            Debug.Log("Critical hit!");
        }

        return dmg;
    }
}