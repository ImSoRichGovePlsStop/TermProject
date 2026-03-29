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

    protected override void Start()
    {
        base.Start();
        if (DamageNumberSpawner.Instance != null)
            DamageNumberSpawner.Instance.RegisterEntity(this, healthBarHeight);
    }

    public void DieWithAnimation()
    {
        useAnimation = true;
        Die();
    }

    public void DieWithoutAnimation()
    {
        useAnimation = false;
        destroyDelay = 0f;
        Die();
    }

    protected override void OnDie()
    {
        if (useAnimation && animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, destroyDelay);
    }
}