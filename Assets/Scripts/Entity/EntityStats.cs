using UnityEngine;

public class EntityStats : MonoBehaviour
{
    [Header("Base Stats")]
    [SerializeField] protected float baseMaxHP = 30f;
    [SerializeField] protected float baseMoveSpeed = 3f;
    [SerializeField] protected float baseDamage = 6f;
    [SerializeField] protected float baseAttackSpeed = 1f;
    [SerializeField] private float baseShield = 0f;
    public float BaseShield => baseShield;

    private EntityStatModifier flatModifier = new EntityStatModifier();
    private EntityStatModifier multiplierModifier = new EntityStatModifier();
    private StatScale statScale = StatScale.Default;

    public void SetStatScale(StatScale scale) => statScale = scale;

    public float MaxHP => (baseMaxHP + flatModifier.maxHP) * (1f + multiplierModifier.maxHP) * statScale.hp;
    public float MoveSpeed => Mathf.Max(0f, (baseMoveSpeed + flatModifier.moveSpeed) * (1f + multiplierModifier.moveSpeed) * statScale.moveSpeed);
    public float MoveSpeedRatio => baseMoveSpeed > 0f ? MoveSpeed / baseMoveSpeed : 1f;
    public float Damage => Mathf.Max(0f, (baseDamage + flatModifier.damage) * (1f + multiplierModifier.damage) * statScale.damage);
    public float AttackSpeed => Mathf.Max(0f, (baseAttackSpeed + flatModifier.attackSpeed) * (1f + multiplierModifier.attackSpeed));
    public float DamageTaken => Mathf.Max(0f, 1f + flatModifier.damageTaken + multiplierModifier.damageTaken);

    public void AddFlatModifier(EntityStatModifier modifier)
    {
        flatModifier.maxHP += modifier.maxHP;
        flatModifier.moveSpeed += modifier.moveSpeed;
        flatModifier.damage += modifier.damage;
        flatModifier.damageTaken += modifier.damageTaken;
        flatModifier.attackSpeed += modifier.attackSpeed;
    }

    public void RemoveFlatModifier(EntityStatModifier modifier)
    {
        flatModifier.maxHP -= modifier.maxHP;
        flatModifier.moveSpeed -= modifier.moveSpeed;
        flatModifier.damage -= modifier.damage;
        flatModifier.damageTaken -= modifier.damageTaken;
        flatModifier.attackSpeed -= modifier.attackSpeed;
    }

    public void AddMultiplierModifier(EntityStatModifier modifier)
    {
        multiplierModifier.maxHP += modifier.maxHP;
        multiplierModifier.moveSpeed += modifier.moveSpeed;
        multiplierModifier.damage += modifier.damage;
        multiplierModifier.damageTaken += modifier.damageTaken;
        multiplierModifier.attackSpeed += modifier.attackSpeed;
    }

    public void RemoveMultiplierModifier(EntityStatModifier modifier)
    {
        multiplierModifier.maxHP -= modifier.maxHP;
        multiplierModifier.moveSpeed -= modifier.moveSpeed;
        multiplierModifier.damage -= modifier.damage;
        multiplierModifier.damageTaken -= modifier.damageTaken;
        multiplierModifier.attackSpeed -= modifier.attackSpeed;
    }
}