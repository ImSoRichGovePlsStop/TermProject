using UnityEngine;

public enum EnemyTier { Normal, Elite, Miniboss, Boss }

public class EnemyHealthBase : HealthBase
{
    [Header("Tier")]
    [SerializeField] private EnemyTier tier = EnemyTier.Normal;
    public EnemyTier Tier => tier;

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