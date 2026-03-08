using System.Collections;
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

    private StatModifier flatModifier = new StatModifier();

    private StatModifier multiplierModifier = new StatModifier();

    public bool IsInvincible { get; private set; }

    public void SetInvincible(bool value)
    {
        IsInvincible = value;
    }

    private void Update()
    {
        Debug.Log($"HP: {CurrentHealth:F1}/{MaxHealth:F1} | DMG: {Damage:F1} | SPD: {MoveSpeed:F1} | CRIT: {CritChance:P0}/{CritDamage:P0} | EVADE: {EvadeChance:P0} | INV: {IsInvincible}");
    }

    public float MaxHealth =>
        (weaponHealth + flatModifier.health) * (1 + multiplierModifier.health);
    public float Damage =>
        (weaponDamage + flatModifier.damage) * (1 + multiplierModifier.damage);
    public float AttackSpeed =>
        (weaponAttackSpeed + flatModifier.attackSpeed) * (1 + multiplierModifier.attackSpeed);
    public float MoveSpeed =>
        (weaponMoveSpeed + flatModifier.moveSpeed) * (1 + multiplierModifier.moveSpeed);
    public float CritChance =>
        (weaponCritChance + flatModifier.critChance) * (1 + multiplierModifier.critChance);
    public float CritDamage =>
        (weaponCritDamage + flatModifier.critDamage) * (1 + multiplierModifier.critDamage);
    public float EvadeChance =>
        (weaponEvadeChance + flatModifier.evadeChance) * (1 + multiplierModifier.evadeChance);

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

    // buffs/debuffs from modules (Don't check for invincibility)

    public void AddFlatModifier(StatModifier bonus)
    {
        flatModifier.health += bonus.health;
        flatModifier.damage += bonus.damage;
        flatModifier.attackSpeed += bonus.attackSpeed;
        flatModifier.moveSpeed += bonus.moveSpeed;
        flatModifier.critChance += bonus.critChance;
        flatModifier.critDamage += bonus.critDamage;
        flatModifier.evadeChance += bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void AddMultiplierModifier(StatModifier bonus)
    {
        multiplierModifier.health += bonus.health;
        multiplierModifier.damage += bonus.damage;
        multiplierModifier.attackSpeed += bonus.attackSpeed;
        multiplierModifier.moveSpeed += bonus.moveSpeed;
        multiplierModifier.critChance += bonus.critChance;
        multiplierModifier.critDamage += bonus.critDamage;
        multiplierModifier.evadeChance += bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveFlatModifier(StatModifier bonus)
    {
        flatModifier.health -= bonus.health;
        flatModifier.damage -= bonus.damage;
        flatModifier.attackSpeed -= bonus.attackSpeed;
        flatModifier.moveSpeed -= bonus.moveSpeed;
        flatModifier.critChance -= bonus.critChance;
        flatModifier.critDamage -= bonus.critDamage;
        flatModifier.evadeChance -= bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveMultiplierModifier(StatModifier bonus)
    {
        multiplierModifier.health -= bonus.health;
        multiplierModifier.damage -= bonus.damage;
        multiplierModifier.attackSpeed -= bonus.attackSpeed;
        multiplierModifier.moveSpeed -= bonus.moveSpeed;
        multiplierModifier.critChance -= bonus.critChance;
        multiplierModifier.critDamage -= bonus.critDamage;
        multiplierModifier.evadeChance -= bonus.evadeChance;

        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    // buffs/debuffs from monsters, traps, etc. (Check for invincibility)

    public void TakeDebuff(StatModifier debuff, float duration)
    {
        if (IsInvincible) return;
        StartCoroutine(DebuffCoroutine(debuff, duration, false));
    }

    public bool TakeDebuff(StatModifier debuff)
    {
        if (IsInvincible) return false;
        AddFlatModifier(debuff);
        return true;
    }

    public void TakeDebuffMultiplier(StatModifier debuff, float duration)
    {
        if (IsInvincible) return;
        StartCoroutine(DebuffCoroutine(debuff, duration, true));
    }

    public bool TakeDebuffMultiplier(StatModifier debuff)
    {
        if (IsInvincible) return false;
        AddMultiplierModifier(debuff);
        return true;
    }

    private IEnumerator DebuffCoroutine(StatModifier debuff, float duration, bool isMultiplier)
    {
        if (isMultiplier)
            AddMultiplierModifier(debuff);
        else
            AddFlatModifier(debuff);

        yield return new WaitForSeconds(duration);

        if (isMultiplier)
            RemoveMultiplierModifier(debuff);
        else
            RemoveFlatModifier(debuff);
    }

    public void ResetModifiers()
    {
        flatModifier = new StatModifier();
        multiplierModifier = new StatModifier();
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

    public void TakeDamage(float amount)
    {
        if (IsInvincible) return;

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