using System.Collections;
using UnityEngine;

public abstract class BaseBossHealth : HealthBase
{
    [Header("Boss Health")]
    [SerializeField] protected float hurtStunDuration = 0.15f;

    private Coroutine hurtCoroutine;

    public override void TakeDamage(float damage, bool isCrit = false)
    {
        if (isDead) return;
        if (damage <= 0f) return;

        base.TakeDamage(damage, isCrit);

        if (!isDead && isHurt)
        {
            if (hurtCoroutine != null)
                StopCoroutine(hurtCoroutine);
            hurtCoroutine = StartCoroutine(HurtRoutine());
        }
    }

    private IEnumerator HurtRoutine()
    {
        yield return new WaitForSeconds(hurtStunDuration);
        EndHurt();
        hurtCoroutine = null;
    }
}