using System.Collections;
using UnityEngine;

public class EnemyStatusHandler : MonoBehaviour
{
    private EnemyStatModifier flatModifier = new EnemyStatModifier();
    private EnemyStatModifier multiplierModifier = new EnemyStatModifier();

    public float MoveSpeedMultiplier
    {
        get { return 1f + multiplierModifier.moveSpeed + flatModifier.moveSpeed; }
    }

    public float AttackSpeedMultiplier
    {
        get { return 1f + multiplierModifier.attackSpeed + flatModifier.attackSpeed; }
    }

    public float DamageTakenMultiplier
    {
        get { return 1f + multiplierModifier.damageTaken + flatModifier.damageTaken; }
    }

    public void AddFlatModifier(EnemyStatModifier modifier)
    {
        flatModifier.moveSpeed += modifier.moveSpeed;
        flatModifier.attackSpeed += modifier.attackSpeed;
        flatModifier.damageTaken += modifier.damageTaken;
    }

    public void AddFlatModifier(EnemyStatModifier modifier, float duration)
    {
        StartCoroutine(TimedModifierCoroutine(modifier, duration, false));
    }

    public void RemoveFlatModifier(EnemyStatModifier modifier)
    {
        flatModifier.moveSpeed -= modifier.moveSpeed;
        flatModifier.attackSpeed -= modifier.attackSpeed;
        flatModifier.damageTaken -= modifier.damageTaken;
    }

    public void AddMultiplierModifier(EnemyStatModifier modifier)
    {
        multiplierModifier.moveSpeed += modifier.moveSpeed;
        multiplierModifier.attackSpeed += modifier.attackSpeed;
        multiplierModifier.damageTaken += modifier.damageTaken;
    }

    public void AddMultiplierModifier(EnemyStatModifier modifier, float duration)
    {
        StartCoroutine(TimedModifierCoroutine(modifier, duration, true));
    }

    public void RemoveMultiplierModifier(EnemyStatModifier modifier)
    {
        multiplierModifier.moveSpeed -= modifier.moveSpeed;
        multiplierModifier.attackSpeed -= modifier.attackSpeed;
        multiplierModifier.damageTaken -= modifier.damageTaken;
    }

    private IEnumerator TimedModifierCoroutine(EnemyStatModifier modifier, float duration, bool isMultiplier)
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
}