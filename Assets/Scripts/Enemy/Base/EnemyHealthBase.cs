using UnityEngine;

public class EnemyHealthBase : HealthBase
{
    private EnemyBase controller;
    private PlayerCombatContext context;

    protected override void Awake()
    {
        base.Awake();
        controller = GetComponent<EnemyBase>();
    }

    protected override void Start()
    {
        base.Start();
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            context = playerObj.GetComponent<PlayerCombatContext>();

        DamageNumberSpawner.Instance?.RegisterEntity(this, healthBarHeight);
    }

    protected override void OnDeathStart()
    {
        controller?.OnDeath();
        context?.NotifyEnemyKilled((HealthBase)this);
    }
}