using System.Collections;
using UnityEngine;

public class ArcNodeHealthBase : HealthBase
{
    [SerializeField] private float maxHp = 30f;

    protected override void Awake()
    {
        maxHP = maxHp;
        base.Awake();
    }

    public void SetMaxHp(float value)
    {
        maxHP = value;
        currentHP = value;
    }

    private int[] activeCounter;
    private bool isDecaying;

    public void StartDecay(float duration, int maxSimultaneous, int[] counter)
    {
        activeCounter = counter;
        StartCoroutine(DecayRoutine(duration, maxSimultaneous, counter));
    }

    private IEnumerator DecayRoutine(float duration, int maxSimultaneous, int[] counter)
    {
        while (counter[0] >= maxSimultaneous)
            yield return null;

        if (IsDead) yield break;

        counter[0]++;
        isDecaying = true;
        float startHp = currentHP;
        float elapsed = 0f;

        while (elapsed < duration && !IsDead)
        {
            elapsed += Time.deltaTime;
            TakeDamage(startHp * Time.deltaTime / duration);
            yield return null;
        }

        if (!IsDead) TakeDamage(currentHP);

        isDecaying = false;
        counter[0]--;
    }

    protected override void OnDie()
    {
        if (isDecaying && activeCounter != null)
        {
            isDecaying = false;
            activeCounter[0]--;
        }
        Destroy(gameObject);
    }
}