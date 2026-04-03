using System.Collections.Generic;
using UnityEngine;

public class FoundationPassive : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject brawlerPrefab;

    [Header("Infectious Strike")]
    [SerializeField] private float infectiousStrikeChance = 0.08f;

    [Header("Swift Minions")]
    [SerializeField] private float swiftMinionSpeedMult = 0.3f;

    [Header("Recycled Essence")]
    [SerializeField] private int recycledEssenceMaxStacks = 100;
    [SerializeField] private float recycledEssenceCooldownReduce = 0.2f;
    [SerializeField] private int stackNormal = 4;
    [SerializeField] private int stackElite = 8;
    [SerializeField] private int stackMiniboss = 12;
    [SerializeField] private int stackBoss = 16;

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
    [SerializeField] private float sharedEssenceBomberCenterDamageScale = 0.5f;
    [SerializeField] private float sharedEssenceBomberEdgeDamageScale = 0.5f;

    public bool infectiousStrike = false;
    public bool swiftMinions = false;
    public bool recycledEssence = false;
    public bool sharedEssence = false;
    public bool rapidSpawn = false;

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

    private void OnTotemSpawned(Totem totem)
    {
        if (!enabled) return;
        if (sharedEssence) totem.hpScaleBonus += sharedEssenceTotemHpScale;
        if (rapidSpawn) totem.ReduceSpawnIntervals(rapidSpawnIntervalReduction);
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
            bomber.centerDamageScaleBonus += sharedEssenceBomberCenterDamageScale;
            bomber.edgeDamageScaleBonus += sharedEssenceBomberEdgeDamageScale;
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

        if (infectiousStrike && brawlerPrefab != null)
        {
            foreach (var enemy in context.LastHitEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                if (Random.value > infectiousStrikeChance) continue;
                GameObject obj = Instantiate(brawlerPrefab, enemy.transform.position, Quaternion.identity);
                obj.GetComponent<SummonerBase>()?.Init(stats, SummonerTier.Mini);
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
                    wandAttack.ReduceTotemCooldown(recycledEssenceCooldownReduce);
                }
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