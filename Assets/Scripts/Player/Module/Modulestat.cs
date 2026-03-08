using UnityEngine;

[CreateAssetMenu(fileName = "ModuleStat_New", menuName = "Inventory/Stat")]
public class ModuleStat : ScriptableObject
{
    public float bonusMaxHp;
    public float bonusAttack;
    public float bonusAttackSpeed;
    public float bonusMoveSpeed;
    public float bonusCritChance;
    public float bonusCritDamage;
}