using UnityEngine;

public class SummonerHealth : HealthBase
{
    [Header("Animation")]
    [SerializeField] private Animator animator;

    private bool useAnimation = true;

    protected override void Awake()
    {
        base.Awake();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void DieWithAnimation()
    {
        useAnimation = true;
        Die();
    }

    public void DieWithoutAnimation()
    {
        useAnimation = false;
        Die();
    }

    protected override void OnDie()
    {
        if (useAnimation && animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, destroyDelay);
    }
}