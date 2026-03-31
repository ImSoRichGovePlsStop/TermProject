using UnityEngine;

public class Totem : HealthBase
{
    [Header("Decay")]
    [SerializeField] private float hpDecayRate = 4f;

    [Header("Spawn Prefabs")]
    [SerializeField] private GameObject brawlerPrefab;
    [SerializeField] private GameObject zapperPrefab;
    [SerializeField] private GameObject bomberPrefab;

    [Header("Spawn Intervals")]
    [SerializeField] private float brawlerInterval = 3f;
    [SerializeField] private float zapperInterval = 4f;
    [SerializeField] private float bomberInterval = 5f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadius = 1f;

    private float brawlerTimer;
    private float zapperTimer;
    private float bomberTimer;

    private bool canSpawnZapper = true;
    private bool canSpawnBomber = true;

    private PlayerStats playerStats;
    private PlayerCombatContext context;

    public void Init(PlayerStats playerStats, PlayerCombatContext context)
    {
        this.playerStats = playerStats;
        this.context = context;

        currentHP = maxHP;

        brawlerTimer = brawlerInterval;
        zapperTimer = zapperInterval;
        bomberTimer = bomberInterval;
    }

    public void SetSpawnConfig(bool canZapper, bool canBomber)
    {
        canSpawnZapper = canZapper;
        canSpawnBomber = canBomber;
    }

    public void AddHP(float amount)
    {
        if (isDead) return;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
    }

    protected override void Update()
    {
        if (isDead) return;

        // HP decay
        TakeDamage(hpDecayRate * Time.deltaTime);

        if (isDead) return;

        TickSpawn(ref brawlerTimer, brawlerInterval, brawlerPrefab, true);
        TickSpawn(ref zapperTimer, zapperInterval, zapperPrefab, canSpawnZapper);
        TickSpawn(ref bomberTimer, bomberInterval, bomberPrefab, canSpawnBomber);
    }

    private void TickSpawn(ref float timer, float interval, GameObject prefab, bool canSpawn)
    {
        if (!canSpawn || prefab == null) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = interval;
            SpawnSummoner(prefab);
        }
    }

    private void SpawnSummoner(GameObject prefab)
    {
        if (playerStats == null) return;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        var summoner = obj.GetComponent<SummonerBase>();
        if (summoner != null)
            summoner.Init(playerStats);
    }

    protected override void OnDamageTaken(float damage, bool isCrit) { }

    protected override void OnDie()
    {
        OnTotemDied();
        Destroy(gameObject, 1f);
    }

    protected virtual void OnTotemDied() { }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}