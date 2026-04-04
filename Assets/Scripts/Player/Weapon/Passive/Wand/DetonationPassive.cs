using UnityEngine;

public class DetonationPassive : MonoBehaviour
{
    [Header("Overcharge")]
    [SerializeField] private float overchargeDamageBonus = 0.25f;

    [Header("Wide Blast")]
    [SerializeField] private float wideBlastRadiusBonus = 0.35f;

    [Header("Shrapnel")]
    [System.NonSerialized] public GameObject bomberPrefab;

    public bool volatileBody = false;
    public bool overcharge = false;
    public bool wideBlast = false;
    public bool unstableCharge = false;
    public bool scorchedEarth = false;
    public bool shrapnel = false;
    public bool singularity = false;

    private PlayerStats playerStats;
    public float eliteBomberChance = 0f;

    public void Init(PlayerStats playerStats, PlayerCombatContext combatContext)
    {
        this.playerStats = playerStats;
        SummonerBase.OnSummonerPreInit += OnSummonerPreInit;
        SummonerBase.OnSummonerInit += OnSummonerInit;
        WandAttack.OnTotemSpawned += OnTotemSpawned;
    }

    private void OnDestroy()
    {
        SummonerBase.OnSummonerPreInit -= OnSummonerPreInit;
        SummonerBase.OnSummonerInit -= OnSummonerInit;
        WandAttack.OnTotemSpawned -= OnTotemSpawned;
    }

    private void OnTotemSpawned(Totem totem)
    {
        if (!enabled) return;
        totem.eliteBomberChance = singularity ? eliteBomberChance : 0f;
    }

    private void OnSummonerPreInit(SummonerBase summoner)
    {
        if (!enabled) return;
        if (summoner is BomberSummoner bomber)
        {
            if (volatileBody) bomber.volatileBody = true;
            if (overcharge) bomber.explosionDamageMult += overchargeDamageBonus;
            if (wideBlast) bomber.explosionRadiusMult += wideBlastRadiusBonus;
            if (unstableCharge) bomber.unstableCharge = true;
            if (scorchedEarth) bomber.scorchedEarth = true;
            if (shrapnel && bomber.tier != SummonerTier.Mini) bomber.shrapnel = true;
            if (singularity && bomber.tier == SummonerTier.Elite) bomber.singularity = true;
        }
    }

    private void OnSummonerInit(EntityStats summonerStats)
    {
        if (!enabled || !shrapnel) return;
        var bomber = summonerStats.GetComponent<BomberSummoner>();
        if (bomber == null || bomber.tier == SummonerTier.Mini) return;
        bomber.OnExploded += (pos) => OnBomberExploded(pos, bomber);
    }

    private void OnBomberExploded(Vector3 position, BomberSummoner bomber)
    {
        if (bomberPrefab == null || playerStats == null) return;

        int count = bomber.GetShrapnelCount(bomber.tier);
        float dist = bomber.GetShrapnelDistance(bomber.tier);

        float angleStep = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
            Vector3 target = position + dir * dist;

            var go = Instantiate(bomberPrefab, position, Quaternion.identity);
            var mini = go.GetComponent<BomberSummoner>();
            if (mini != null)
            {
                mini.Init(playerStats, SummonerTier.Mini);
                mini.Launch(target);
            }
        }
    }
}