using UnityEngine;

public class ConductionPassive : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject zapperPrefab;

    [Header("Stable Charge")]
    [SerializeField] private float stableChargeDurationBonus = 3f;

    [Header("High Voltage")]
    [SerializeField] private float highVoltageChainBonus = 0.2f;

    [Header("Unstable Conductor")]
    [System.NonSerialized] public float eliteZapperChance = 0.15f;

    public bool stableCharge = false;
    public bool residualCurrent = false;
    public bool highVoltage = false;
    public bool lightningRod = false;
    public bool arcPulse = false;
    public bool cardiacArrest = false;
    public bool unstableConductor = false;

    private PlayerStats stats;
    private PlayerCombatContext context;

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        stats = playerStats;
        context = combatContext;
        SummonerBase.OnSummonerPreInit += OnSummonerPreInit;
        ZapperSummoner.OnAttachedEnemyKilled += OnAttachedEnemyKilled;
        WandAttack.OnTotemSpawned += OnTotemSpawned;
    }

    private void OnDestroy()
    {
        SummonerBase.OnSummonerPreInit -= OnSummonerPreInit;
        ZapperSummoner.OnAttachedEnemyKilled -= OnAttachedEnemyKilled;
        WandAttack.OnTotemSpawned -= OnTotemSpawned;
    }

    private void OnTotemSpawned(Totem totem)
    {
        if (!enabled) return;
        totem.eliteZapperChance = unstableConductor ? eliteZapperChance : 0f;
    }

    private void OnSummonerPreInit(SummonerBase summoner)
    {
        if (!enabled) return;
        if (summoner is ZapperSummoner zapper)
        {
            if (stableCharge) zapper.attachDurationBonus += stableChargeDurationBonus;
            if (highVoltage) zapper.chainPercentBonus += highVoltageChainBonus;
            if (lightningRod) zapper.lightningRod = true;
            if (arcPulse) zapper.arcPulse = true;
            if (cardiacArrest) zapper.cardiacArrest = true;
        }
        else if (summoner is BrawlerSummoner brawler)
        {
            if (lightningRod) brawler.lightningRod = true;
        }
        else if (summoner is BomberSummoner bomber)
        {
            if (lightningRod) bomber.lightningRod = true;
        }
    }

    private void OnAttachedEnemyKilled(HealthBase entity)
    {
        if (!enabled || !residualCurrent) return;
        if (zapperPrefab == null) return;
        Vector2 randCircle = Random.insideUnitCircle * 0.5f;
        Vector3 spawnPos = entity.transform.position + new Vector3(randCircle.x, 0f, randCircle.y);
        var obj = Instantiate(zapperPrefab, spawnPos, Quaternion.identity);
        obj.GetComponent<SummonerBase>()?.Init(stats, SummonerTier.Mini);
    }
}