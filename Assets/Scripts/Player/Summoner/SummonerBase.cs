using System;
using UnityEngine;

public enum SummonerTier { Mini, Normal, Elite }

[RequireComponent(typeof(SummonerHealth))]
[RequireComponent(typeof(SummonerMovement))]
[RequireComponent(typeof(EntityStats))]
public abstract class SummonerBase : MonoBehaviour
{
    [Header("Search")]
    [SerializeField] protected float searchRadius = 10f;

    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 8f;

    [Header("Tier Scale")]
    [SerializeField] private float miniLifetimeMult = 0.6f;
    [SerializeField] private float miniSearchRadiusMult = 0.7f;
    [SerializeField] private float eliteLifetimeMult = 1.5f;
    [SerializeField] private float eliteSearchRadiusMult = 1.5f;

    [Header("Target Height")]
    [SerializeField] protected float maxHeightDiff = 1f;

    [Header("References")]
    [SerializeField] protected SummonerHealth health;
    [SerializeField] protected SummonerMovement movement;
    [SerializeField] protected EntityStats stats;

    protected PlayerStats playerStats;
    protected SummonerTier tier = SummonerTier.Normal;
    protected float remainingLifetime;

    public static event Action<SummonerBase> OnSummonerPreInit;
    public static event Action<EntityStats> OnSummonerInit;

    [System.NonSerialized] public float hpScaleBonus = 0f;
    [System.NonSerialized] public float speedScaleBonus = 0f;

    public PlayerStats PlayerStats => playerStats;

    public virtual void Init(PlayerStats playerStats, SummonerTier tier = SummonerTier.Normal)
    {
        this.playerStats = playerStats;
        this.tier = tier;
        remainingLifetime = lifetime;
        if (tier == SummonerTier.Mini) { remainingLifetime *= miniLifetimeMult; searchRadius *= miniSearchRadiusMult; }
        else if (tier == SummonerTier.Elite) { remainingLifetime *= eliteLifetimeMult; searchRadius *= eliteSearchRadiusMult; }
        OnSummonerPreInit?.Invoke(this);
        ApplyPlayerScaling();
        OnSummonerInit?.Invoke(stats);
    }

    protected virtual void ApplyPlayerScaling() { }

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