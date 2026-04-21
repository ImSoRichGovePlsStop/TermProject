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

    private bool canSpawnZapper = false;
    private bool canSpawnBomber = false;

    private PlayerStats playerStats;
    private PlayerCombatContext context;

    public float hpScaleBonus = 0f;
    public float startHpPercent = 1f;

    public void Init(PlayerStats playerStats, PlayerCombatContext context)
    {
        this.playerStats = playerStats;
        this.context = context;

        currentHP = (maxHP + playerStats.MaxHealth * hpScaleBonus) * startHpPercent;

        brawlerTimer = brawlerInterval;
        zapperTimer = zapperInterval;
        bomberTimer = bomberInterval;
    }

    public void SetSpawnConfig(bool canZapper, bool canBomber)
    {
        canSpawnZapper = canZapper;
        canSpawnBomber = canBomber;
    }

    private float _auraSpeedMult = 1f;
    public float auraSpeedMult
    {
        get => _auraSpeedMult;
        set
        {
            float oldBrawler = brawlerInterval * _auraSpeedMult;
            float oldZapper = zapperInterval * _auraSpeedMult;
            float oldBomber = bomberInterval * _auraSpeedMult;

            _auraSpeedMult = value;

            float newBrawler = brawlerInterval * _auraSpeedMult;
            float newZapper = zapperInterval * _auraSpeedMult;
            float newBomber = bomberInterval * _auraSpeedMult;

            if (oldBrawler > 0f) brawlerTimer = (brawlerTimer / oldBrawler) * newBrawler;
            if (oldZapper > 0f) zapperTimer = (zapperTimer / oldZapper) * newZapper;
            if (oldBomber > 0f) bomberTimer = (bomberTimer / oldBomber) * newBomber;
        }
    }

    public void ReduceSpawnIntervals(float amount)
    {
        brawlerInterval = Mathf.Max(0.5f, brawlerInterval - amount);
        zapperInterval = Mathf.Max(0.5f, zapperInterval - amount);
        bomberInterval = Mathf.Max(0.5f, bomberInterval - amount);
    }

    [ContextMenu("Debug: Force Spawn Elite Brawler")]
    private void Debug_SpawnEliteBrawler()
    {
        SpawnSummoner(brawlerPrefab, SummonerTier.Elite);
    }

    [ContextMenu("Debug: Force Spawn Elite Zapper")]
    private void Debug_SpawnEliteZapper()
    {
        SpawnSummoner(zapperPrefab, SummonerTier.Elite);
    }

    [ContextMenu("Debug: Force Spawn Elite Bomber")]
    private void Debug_SpawnEliteBomber()
    {
        SpawnSummoner(bomberPrefab, SummonerTier.Elite);
    }

    public void AddHP(float amount)
    {
        Heal(amount);
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
            timer = interval * auraSpeedMult;
            SpawnSummoner(prefab);
        }
    }

    public float eliteBrawlerChance = 0f;
    public float eliteZapperChance = 0f;
    public float eliteBomberChance = 0f;

    private void SpawnSummoner(GameObject prefab, SummonerTier tier = SummonerTier.Normal)
    {
        if (playerStats == null) return;

        SummonerTier spawnTier = tier;
        if (prefab == brawlerPrefab && eliteBrawlerChance > 0f && Random.value < eliteBrawlerChance)
            spawnTier = SummonerTier.Elite;
        else if (prefab == zapperPrefab && eliteZapperChance > 0f && Random.value < eliteZapperChance)
            spawnTier = SummonerTier.Elite;
        else if (prefab == bomberPrefab && eliteBomberChance > 0f && Random.value < eliteBomberChance)
            spawnTier = SummonerTier.Elite;

        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        var summoner = obj.GetComponent<SummonerBase>();
        if (summoner != null)
            summoner.Init(playerStats, spawnTier);
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