using System.Collections.Generic;
using UnityEngine;

public class FoundationPassive : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject brawlerPrefab;
    public GameObject zapperPrefab;
    public GameObject bomberPrefab;

    [Header("Infectious Strike")]
    [SerializeField] private float infectiousStrikeChance = 0.08f;

    [Header("Swift Minions")]
    [SerializeField] private float swiftMinionSpeedMult = 0.3f;

    [Header("Recycled Essence")]
    [SerializeField] private int recycledEssenceMaxStacks = 60;
    [SerializeField] private float recycledEssenceCooldownReduceSeconds = 3f;
    [SerializeField] private int stackNormal = 4;
    [SerializeField] private int stackElite = 8;
    [SerializeField] private int stackMiniboss = 10;
    [SerializeField] private int stackBoss = 12;

    [Header("Great Conjunction")]
    [SerializeField] private int greatConjunctionMaxTotems = 2;
    [SerializeField] private float greatConjunctionStartHpPercent = 0.7f;

    [Header("Mana Feedback")]
    [SerializeField] private float manaFeedbackHPPerHit = 0.5f;

    [Header("Rapid Spawn")]
    [SerializeField] private float rapidSpawnIntervalReduction = 1f;

    [Header("Shared Essence - Totem")]
    [SerializeField] private float sharedEssenceTotemHpScale = 0.05f;

    [Header("Shared Essence - Brawler")]
    [SerializeField] private float sharedEssenceBrawlerHpScale = 0.1f;
    [SerializeField] private float sharedEssenceBrawlerSpeedScale = 0.15f;
    [SerializeField] private float sharedEssenceBrawlerDamageScale = 0.15f;

    [Header("Shared Essence - Zapper")]
    [SerializeField] private float sharedEssenceZapperHpScale = 0.1f;
    [SerializeField] private float sharedEssenceZapperSpeedScale = 0.15f;

    [Header("Shared Essence - Bomber")]
    [SerializeField] private float sharedEssenceBomberHpScale = 0.1f;
    [SerializeField] private float sharedEssenceBomberSpeedScale = 0.2f;
    [SerializeField] private float sharedEssenceBomberDamageScale = 0.5f;

    [Header("Warlord")]
    [System.NonSerialized] public float eliteBrawlerChance = 0.15f;

    public bool infectiousStrike = false;
    public bool swiftMinions = false;
    public bool recycledEssence = false;
    public bool sharedEssence = false;
    public bool rapidSpawn = false;
    public bool manaFeedback = false;
    public bool greatConjunction = false;
    public bool warlord = false;
    public bool canSpawnZapper = false;
    public bool canSpawnBomber = false;

    private PlayerStats stats;
    private PlayerCombatContext context;
    private WandAttack wandAttack;
    private int currentStacks = 0;

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        stats = playerStats;
        context = combatContext;
        wandAttack = playerStats.GetComponent<WandAttack>();
        context.OnAttack += OnAttack;
        SummonerBase.OnSummonerPreInit += OnSummonerPreInit;
        SummonerBase.OnSummonerInit += OnSummonerInit;
        WandAttack.OnTotemSpawned += OnTotemSpawned;
    }

    private void OnDestroy()
    {
        if (context != null)
            context.OnAttack -= OnAttack;
        SummonerBase.OnSummonerPreInit -= OnSummonerPreInit;
        SummonerBase.OnSummonerInit -= OnSummonerInit;
        WandAttack.OnTotemSpawned -= OnTotemSpawned;
    }

    private void Update()
    {
        if (wandAttack == null) return;
        if (recycledEssence)
            wandAttack.SetInnerBorder((float)currentStacks / recycledEssenceMaxStacks, true);
        else
            wandAttack.SetInnerBorder(0f, false);
    }

    public void ApplyTotemConfig()
    {
        if (wandAttack == null) return;
        wandAttack.SetMaxCharges(greatConjunction ? greatConjunctionMaxTotems : 1);
        wandAttack.SetTotemStartHpPercent(greatConjunction ? greatConjunctionStartHpPercent : 1f);
        wandAttack.SetShowChargeCount(greatConjunction);
    }

    private void OnTotemSpawned(Totem totem)
    {
        if (!enabled) return;
        if (sharedEssence) totem.hpScaleBonus += sharedEssenceTotemHpScale;
        if (rapidSpawn) totem.ReduceSpawnIntervals(rapidSpawnIntervalReduction);
        totem.eliteBrawlerChance = warlord ? eliteBrawlerChance : 0f;
        totem.SetSpawnConfig(canSpawnZapper, canSpawnBomber);
    }

    private void OnSummonerPreInit(SummonerBase summoner)
    {
        if (!enabled || !sharedEssence) return;

        if (summoner is BrawlerSummoner brawler)
        {
            brawler.hpScaleBonus += sharedEssenceBrawlerHpScale;
            brawler.speedScaleBonus += sharedEssenceBrawlerSpeedScale;
            brawler.damageScaleBonus += sharedEssenceBrawlerDamageScale;
        }
        else if (summoner is ZapperSummoner zapper)
        {
            zapper.hpScaleBonus += sharedEssenceZapperHpScale;
            zapper.speedScaleBonus += sharedEssenceZapperSpeedScale;
        }
        else if (summoner is BomberSummoner bomber)
        {
            bomber.hpScaleBonus += sharedEssenceBomberHpScale;
            bomber.speedScaleBonus += sharedEssenceBomberSpeedScale;
            bomber.damageScaleBonus += sharedEssenceBomberDamageScale;
        }
    }

    private void OnSummonerInit(EntityStats summonerStats)
    {
        if (!enabled) return;
        if (swiftMinions)
            summonerStats.AddMultiplierModifier(new EntityStatModifier { moveSpeed = swiftMinionSpeedMult });
    }

    private void OnAttack()
    {
        if (!enabled) return;

        if (infectiousStrike)
        {
            var pool = new System.Collections.Generic.List<GameObject>();
            if (brawlerPrefab != null) pool.Add(brawlerPrefab);
            if (canSpawnZapper && zapperPrefab != null) pool.Add(zapperPrefab);
            if (canSpawnBomber && bomberPrefab != null) pool.Add(bomberPrefab);

            if (pool.Count > 0)
            {
                foreach (var enemy in context.LastHitEnemies)
                {
                    if (enemy == null || enemy.IsDead) continue;
                    if (Random.value > infectiousStrikeChance) continue;
                    var prefab = pool[Random.Range(0, pool.Count)];
                    Vector2 randCircle = Random.insideUnitCircle * 0.5f;
                    Vector3 spawnPos = enemy.transform.position + new Vector3(randCircle.x, 0f, -Mathf.Abs(randCircle.y) - 0.3f);
                    GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                    obj.GetComponent<SummonerBase>()?.Init(stats, SummonerTier.Mini);
                }
            }
        }

        if (recycledEssence && wandAttack != null)
        {
            foreach (var enemy in context.LastHitEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                var enemyHealth = enemy.GetComponent<EnemyHealthBase>();
                if (enemyHealth == null) continue;

                currentStacks += GetStacksForTier(enemyHealth.Tier);
                if (currentStacks >= recycledEssenceMaxStacks)
                {
                    currentStacks = 0;
                    wandAttack.ReduceTotemCooldownSeconds(recycledEssenceCooldownReduceSeconds);
                }
            }
        }
        if (manaFeedback && wandAttack != null && context.LastHitEnemies.Count > 0)
        {
            float hp = context.LastHitEnemies.Count * manaFeedbackHPPerHit;
            foreach (var totem in wandAttack.GetActiveTotems())
            {
                if (totem == null || totem.IsDead) continue;
                totem.AddHP(hp);
            }
        }
    }

    private int GetStacksForTier(EnemyTier tier)
    {
        switch (tier)
        {
            case EnemyTier.Elite: return stackElite;
            case EnemyTier.Miniboss: return stackMiniboss;
            case EnemyTier.Boss: return stackBoss;
            default: return stackNormal;
        }
    }
}