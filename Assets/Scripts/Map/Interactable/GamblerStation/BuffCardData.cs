using UnityEngine;

public enum BuffType
{
    ExtremePositive,
    Positive,
    Negative,
    ExtremeNegative
}

public enum PermanentBuffType
{
    MaxHp,
    AttackDamage,
    AttackSpeed,
    MoveSpeed,
    CritChance,
    CritDamage,
    EvadeChance
}

[CreateAssetMenu(fileName = "BuffCardData", menuName = "Gambler/BuffCardData")]
public class BuffCardData : ScriptableObject
{
    [Header("Display")]
    public Sprite icon;
    public string buffName;
    [TextArea] public string description;
    public BuffType buffType;

    public virtual void Apply()
    {
        Debug.Log($"[BuffCardData] Applied: {buffName}");
    }
}