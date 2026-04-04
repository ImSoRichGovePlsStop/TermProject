[System.Serializable]
public class EntityStatModifier
{
    public float maxHP;
    public float moveSpeed;
    public float damage;
    public float damageTaken;
    public float attackSpeed;
}

[System.Serializable]
public struct StatScale
{
    public float hp;
    public float damage;
    public float moveSpeed;

    public static StatScale Default => new StatScale { hp = 1f, damage = 1f, moveSpeed = 1f };
}