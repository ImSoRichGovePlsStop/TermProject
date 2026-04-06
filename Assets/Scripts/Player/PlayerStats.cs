using System;
using System.Collections;
using System.Collections.Generic;
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
    private float weaponDamageTaken;

    private StatModifier flatModifier = new StatModifier();
    private StatModifier multiplierModifier = new StatModifier();

    private PlayerCombatContext context;

    public class ShieldInstance
    {
        public float value;
        public float duration;
        public float expiresAt;
        public Coroutine expireCoroutine;
    }

    private List<ShieldInstance> shields = new List<ShieldInstance>();

    public float CurrentShield
    {
        get
        {
            float total = 0f;
            foreach (var s in shields) total += s.value;
            return total;
        }
    }

    public bool HasShield => shields.Count > 0;

    public event Action<ShieldInstance, float, bool> OnShieldInstanceLost; // <Instance, remainingValue, wasTimedOut>
    public event Action<float> OnPlayerDamaged;
    public event Action OnPlayerDeath;

    public bool IsInvincible { get; private set; }

    public void SetInvincible(bool value)
    {
        IsInvincible = value;
    }

    [Header("Debug")]
    [SerializeField] private bool showDebugUI = true;

    //private void OnGUI()
    //{
    //    if (!showDebugUI) return;
    //    GUI.Box(new Rect(10, 10, 300, 280), "Player Stats");
    //    GUI.skin.label.fontSize = 16;
    //    GUI.Label(new Rect(20, 40, 280, 25), $"HP:  {CurrentHealth:F1} / {MaxHealth:F1}");
    //    GUI.Label(new Rect(20, 70, 280, 25), $"SHIELD:  {CurrentShield:F1}");
    //    GUI.Label(new Rect(20, 100, 280, 25), $"DMG:  {Damage:F1}");
    //    GUI.Label(new Rect(20, 130, 280, 25), $"ASPD:  {AttackSpeed:F2}");
    //    GUI.Label(new Rect(20, 160, 280, 25), $"SPD:  {MoveSpeed:F2}");
    //    GUI.Label(new Rect(20, 190, 280, 25), $"CRIT:  {CritChance:P0} / {CritDamage:P0}");
    //    GUI.Label(new Rect(20, 220, 280, 25), $"EVADE:  {EvadeChance:P0}  INV: {IsInvincible}");
    //    GUI.Label(new Rect(20, 250, 280, 25), $"DMG TAKEN:  {DamageTaken:F2}");
    //}

    public void SetDebugUI(bool enabled)
    {
        showDebugUI = enabled;
    }

    [ContextMenu("Test: +10 Damage")]
    private void Debug_AddDamage()
        => AddFlatModifier(new StatModifier { damage = 10f });

    [ContextMenu("Test: +50% Attack Speed")]
    private void Debug_AddAttackSpeed()
        => AddMultiplierModifier(new StatModifier { attackSpeed = 0.5f });

    [ContextMenu("Test: +30% Crit Chance")]
    private void Debug_AddCritChance()
        => AddFlatModifier(new StatModifier { critChance = 0.3f });

    [ContextMenu("Test: -30% Move Speed (Debuff)")]
    private void Debug_SlowDebuff()
        => AddFlatModifier(new StatModifier { moveSpeed = -3f });

    [ContextMenu("Test: Reset All Modifiers")]
    private void Debug_Reset()
        => ResetModifiers();

    [ContextMenu("Test: Take 20 Damage")]
    private void Debug_TakeDamage()
        => TakeDamage(20f, null);

    [ContextMenu("Test: 25 Flat Heal")]
    private void Debug_HealFlat()
        => Heal(25);

    [ContextMenu("Test: 10 Percent Heal")]
    private void Debug_HealPercent()
        => HealPercent(0.1f);

    [ContextMenu("Test: Full Heal")]
    private void Debug_Heal()
        => HealFull();

    [ContextMenu("Test: Gain 20 Shield")]
    private void Debug_Shield()
        => GainShield(20, 3);

    public StatModifier FlatModifier => flatModifier;
    public StatModifier MultiplierModifier => multiplierModifier;

    public float BaseDamage => weaponDamage + flatModifier.damage;
    public float BaseHealth => weaponHealth + flatModifier.health;
    public float BaseAttackSpeed => weaponAttackSpeed + flatModifier.attackSpeed;
    public float BaseMoveSpeed => weaponMoveSpeed + flatModifier.moveSpeed;
    public float BaseCritChance => weaponCritChance + flatModifier.critChance;
    public float BaseCritDamage => weaponCritDamage + flatModifier.critDamage;

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
    public float DamageTaken =>
        (weaponDamageTaken + flatModifier.damageTaken) * (1 + multiplierModifier.damageTaken);

    public float BaseHP      => weaponHealth;
    public float BaseDMG     => weaponDamage;
    public float BaseATKSPD  => weaponAttackSpeed;
    public float BaseMOVSPD  => weaponMoveSpeed;
    public float BaseCrit    => weaponCritChance;
    public float BaseCritDMG => weaponCritDamage;
    public float BaseEvade   => weaponEvadeChance;

    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;

    void Awake()
    {
        context = GetComponent<PlayerCombatContext>();
        ApplyDefault();
    }

    public void ApplyDefault()
    {
        weaponHealth = defaultHealth;
        weaponMoveSpeed = defaultMoveSpeed;
        weaponDamage = 0;
        weaponAttackSpeed = 0;
        weaponCritChance = 0;
        weaponCritDamage = 1f;
        weaponEvadeChance = 0;
        weaponDamageTaken = 1;
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
        weaponDamageTaken = weapon.damageTaken;
        CurrentHealth = MaxHealth;

        Debug.Log($"Stats applied from {weapon.weaponName}");
    }

    // buffs/debuffs from modules (Don't check for invincibility)

    public void AddFlatModifier(StatModifier bonus, float duration)
    {
        StartCoroutine(TimedModifierCoroutine(bonus, duration, false));
    }

    public void AddFlatModifier(StatModifier bonus)
    {
        float ratio = CurrentHealth / MaxHealth;

        flatModifier.health += bonus.health;
        flatModifier.damage += bonus.damage;
        flatModifier.attackSpeed += bonus.attackSpeed;
        flatModifier.moveSpeed += bonus.moveSpeed;
        flatModifier.critChance += bonus.critChance;
        flatModifier.critDamage += bonus.critDamage;
        flatModifier.evadeChance += bonus.evadeChance;
        flatModifier.damageTaken += bonus.damageTaken;

        CurrentHealth = MaxHealth * ratio;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void AddMultiplierModifier(StatModifier bonus, float duration)
    {
        StartCoroutine(TimedModifierCoroutine(bonus, duration, true));
    }

    public void AddMultiplierModifier(StatModifier bonus)
    {
        float ratio = CurrentHealth / MaxHealth;

        multiplierModifier.health += bonus.health;
        multiplierModifier.damage += bonus.damage;
        multiplierModifier.attackSpeed += bonus.attackSpeed;
        multiplierModifier.moveSpeed += bonus.moveSpeed;
        multiplierModifier.critChance += bonus.critChance;
        multiplierModifier.critDamage += bonus.critDamage;
        multiplierModifier.evadeChance += bonus.evadeChance;
        multiplierModifier.damageTaken += bonus.damageTaken;

        CurrentHealth = MaxHealth * ratio;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveFlatModifier(StatModifier bonus)
    {
        float ratio = CurrentHealth / MaxHealth;

        flatModifier.health -= bonus.health;
        flatModifier.damage -= bonus.damage;
        flatModifier.attackSpeed -= bonus.attackSpeed;
        flatModifier.moveSpeed -= bonus.moveSpeed;
        flatModifier.critChance -= bonus.critChance;
        flatModifier.critDamage -= bonus.critDamage;
        flatModifier.evadeChance -= bonus.evadeChance;
        flatModifier.damageTaken -= bonus.damageTaken;

        CurrentHealth = MaxHealth * ratio;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void RemoveMultiplierModifier(StatModifier bonus)
    {
        float ratio = CurrentHealth / MaxHealth;

        multiplierModifier.health -= bonus.health;
        multiplierModifier.damage -= bonus.damage;
        multiplierModifier.attackSpeed -= bonus.attackSpeed;
        multiplierModifier.moveSpeed -= bonus.moveSpeed;
        multiplierModifier.critChance -= bonus.critChance;
        multiplierModifier.critDamage -= bonus.critDamage;
        multiplierModifier.evadeChance -= bonus.evadeChance;
        multiplierModifier.damageTaken -= bonus.damageTaken;

        CurrentHealth = MaxHealth * ratio;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    // buffs/debuffs from monsters, traps, etc. (Check for invincibility)

    public void TakeDebuff(StatModifier debuff, float duration)
    {
        if (IsInvincible) return;
        StartCoroutine(TimedModifierCoroutine(debuff, duration, false));
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
        StartCoroutine(TimedModifierCoroutine(debuff, duration, true));
    }

    public bool TakeDebuffMultiplier(StatModifier debuff)
    {
        if (IsInvincible) return false;
        AddMultiplierModifier(debuff);
        return true;
    }

    private IEnumerator TimedModifierCoroutine(StatModifier modifier, float duration, bool isMultiplier)
    {
        if (isMultiplier)
            AddMultiplierModifier(modifier);
        else
            AddFlatModifier(modifier);

        yield return new WaitForSeconds(duration);

        if (isMultiplier)
            RemoveMultiplierModifier(modifier);
        else
            RemoveFlatModifier(modifier);
    }

    public void ResetModifiers()
    {
        flatModifier = new StatModifier();
        multiplierModifier = new StatModifier();
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0, MaxHealth);
    }

    public void Heal(float amount)
    {
        float actual = Mathf.Min(amount, MaxHealth - CurrentHealth);
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        if (actual > 0f)
            DamageNumberSpawner.Instance?.SpawnHealNumber(transform.position, actual);
    }

    public void HealPercent(float percent)
    {
        float amount = MaxHealth * percent;
        float actual = Mathf.Min(amount, MaxHealth - CurrentHealth);
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0, MaxHealth);
        if (actual > 0f)
            DamageNumberSpawner.Instance?.SpawnHealNumber(transform.position, actual);
    }

    public void HealFull()
    {
        float actual = MaxHealth - CurrentHealth;
        CurrentHealth = MaxHealth;
        if (actual > 0f)
            DamageNumberSpawner.Instance?.SpawnHealNumber(transform.position, actual);
    }

    public ShieldInstance GainShield(float value, float duration)
    {
        var instance = new ShieldInstance { value = value, duration = duration };

        instance.expiresAt = duration == float.PositiveInfinity
            ? float.PositiveInfinity
            : Time.time + duration;

        int insertIndex = shields.Count;
        for (int i = 0; i < shields.Count; i++)
        {
            if (instance.expiresAt < shields[i].expiresAt)
            {
                insertIndex = i;
                break;
            }
        }
        shields.Insert(insertIndex, instance);

        if (duration != float.PositiveInfinity)
            instance.expireCoroutine = StartCoroutine(ShieldExpireCoroutine(instance));

        return instance;
    }

    public void ExtendShield(ShieldInstance instance, float extraDuration, float extraValue)
    {
        if (!shields.Contains(instance)) return;

        instance.value += extraValue;

        if (extraDuration > 0f)
        {
            if (instance.expireCoroutine != null)
                StopCoroutine(instance.expireCoroutine);

            instance.expiresAt = Mathf.Max(Time.time, instance.expiresAt) + extraDuration;
            instance.duration = instance.expiresAt - Time.time;
            instance.expireCoroutine = StartCoroutine(ShieldExpireCoroutine(instance));
        }
    }

    private IEnumerator ShieldExpireCoroutine(ShieldInstance instance)
    {
        yield return new WaitForSeconds(instance.expiresAt - Time.time);

        if (!shields.Contains(instance)) yield break;

        float remaining = instance.value;
        shields.Remove(instance);
        OnShieldInstanceLost?.Invoke(instance, remaining, true);
    }

    public void ClearAllShields()
    {
        foreach (var s in shields)
            if (s.expireCoroutine != null) StopCoroutine(s.expireCoroutine);
        shields.Clear();
    }


    public void TakeDamage(float amount, HealthBase attacker)
    {
        if (IsInvincible) return;

        if (UnityEngine.Random.value < EvadeChance)
        {
            Debug.Log("Evaded!");
            return;
        }

        float finalDamage = amount * DamageTaken;

        if (shields.Count > 0)
        {
            var instance = shields[0];
            instance.value -= finalDamage;

            if (instance.value <= 0)
            {
                if (instance.expireCoroutine != null)
                    StopCoroutine(instance.expireCoroutine);
                shields.Remove(instance);
                OnShieldInstanceLost?.Invoke(instance, 0f, false);
            }

            if (context != null)
                context.NotifyGetHit(attacker);
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - finalDamage);
        OnPlayerDamaged?.Invoke(finalDamage);

        if (context != null)
        {
            context.NotifyGetHit(attacker);
            context.NotifyTakeDamage(attacker);
        }

        if (CurrentHealth <= 0)
        {
            Debug.Log("Player died!");
            OnPlayerDeath?.Invoke();
        }
    }

    public bool LastHitWasCrit { get; private set; }

    public float CalculateDamage(float damageScale)
    {
        float dmg = Damage * damageScale;

        LastHitWasCrit = UnityEngine.Random.value < CritChance;
        if (LastHitWasCrit)
        {
            dmg *= CritDamage;
            Debug.Log("Critical hit!");
            context?.NotifyCritHit();
        }

        return dmg;
    }
}