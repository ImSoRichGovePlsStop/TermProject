using UnityEngine;

[RequireComponent(typeof(SummonerHealth))]
[RequireComponent(typeof(SummonerMovement))]
[RequireComponent(typeof(EntityStats))]
public abstract class SummonerBase : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 8f;

    [Header("References")]
    [SerializeField] protected SummonerHealth health;
    [SerializeField] protected SummonerMovement movement;
    [SerializeField] protected EntityStats stats;

    protected PlayerStats playerStats;
    protected float remainingLifetime;

    public PlayerStats PlayerStats => playerStats;

    public virtual void Init(PlayerStats playerStats)
    {
        this.playerStats = playerStats;
        remainingLifetime = lifetime;
    }

    protected virtual void Awake()
    {
        if (health == null)
            health = GetComponent<SummonerHealth>();

        if (movement == null)
            movement = GetComponent<SummonerMovement>();

        if (stats == null)
            stats = GetComponent<EntityStats>();

        health.OnDeath += OnDeath;
    }

    protected virtual void OnDeath()
    {
        movement.SetCanMove(false);
    }

    protected virtual void Update()
    {
        TickLifetime();
    }

    protected virtual void TickLifetime()
    {
        remainingLifetime -= Time.deltaTime;

        if (remainingLifetime <= 0f)
            health.DieWithAnimation();
    }

    protected void DieWithoutAnimation()
    {
        health.DieWithoutAnimation();
    }
}