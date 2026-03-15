using System.Collections;
using UnityEngine;

public class EnemyStatusHandler : MonoBehaviour
{
    private EnemyStatModifier multiplierModifier = new EnemyStatModifier();

    public bool IsRooted => multiplierModifier.moveSpeed <= -1f;

    public float MoveSpeedMultiplier
    {
        get { return Mathf.Max(0f, (1f + multiplierModifier.moveSpeed)); }
    }

    public float AttackSpeedMultiplier
    {
        get { return Mathf.Max(0f, (1f + multiplierModifier.attackSpeed)); }
    }

    public float DamageTakenMultiplier
    {
        get { return Mathf.Max(0f, (1f + multiplierModifier.damageTaken)); }
    }

    public void AddMultiplierModifier(EnemyStatModifier modifier)
    {
        multiplierModifier.moveSpeed += modifier.moveSpeed;
        multiplierModifier.attackSpeed += modifier.attackSpeed;
        multiplierModifier.damageTaken += modifier.damageTaken;
    }

    public void AddMultiplierModifier(EnemyStatModifier modifier, float duration)
    {
        StartCoroutine(TimedModifierCoroutine(modifier, duration));
    }

    public void RemoveMultiplierModifier(EnemyStatModifier modifier)
    {
        multiplierModifier.moveSpeed -= modifier.moveSpeed;
        multiplierModifier.attackSpeed -= modifier.attackSpeed;
        multiplierModifier.damageTaken -= modifier.damageTaken;
    }

    private IEnumerator TimedModifierCoroutine(EnemyStatModifier modifier, float duration)
    {
        AddMultiplierModifier(modifier);
        yield return new WaitForSeconds(duration);
        RemoveMultiplierModifier(modifier);
    }
}