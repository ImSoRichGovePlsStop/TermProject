using System.Collections.Generic;
using UnityEngine;

public class EliteBrawlerAura : MonoBehaviour
{
    [Header("VFX")]
    [SerializeField] private Color auraColor = new Color(0f, 1f, 0.3f, 0.25f);
    [SerializeField] private float auraPulseSpeed = 1f;
    [SerializeField] private float auraPulseAlphaMin = 0.15f;
    [SerializeField] private float auraPulseAlphaMax = 0.35f;
    [SerializeField] private float groundOffset = 0.01f;
    [SerializeField] private LayerMask groundLayer;

    private Renderer auraRenderer;
    private Transform auraVFX;

    [Header("Aura")]
    [SerializeField] private float auraRadius = 1f;
    [SerializeField] private float regenInterval = 0.5f;
    [SerializeField] private float regenPercent = 0.025f;

    [Header("Buffs")]
    [SerializeField] private float summonerAttackSpeedMult = 0.4f;
    [SerializeField] private float playerAttackSpeedMult = 0.4f;
    [SerializeField] private float playerDamageTakenReduction = 0.15f;

    private EntityStats ownerStats;
    private PlayerStats playerStats;

    private LayerMask summonerMask;
    private LayerMask playerMask;

    private StatModifier playerModifier = new StatModifier();
    private EntityStatModifier summonerModifier = new EntityStatModifier();

    private HashSet<EntityStats> buffedSummoners = new HashSet<EntityStats>();
    private bool playerBuffed = false;

    private static bool playerBuffedByAura = false;
    private static HashSet<EntityStats> globalBuffedSummoners = new HashSet<EntityStats>();
    private static Dictionary<HealthBase, float> lastRegenTimePerEntity = new Dictionary<HealthBase, float>();

    public void Init(EntityStats stats, PlayerStats pStats, HealthBase ownerHealth)
    {
        ownerStats = stats;
        playerStats = pStats;

        summonerMask = 1 << LayerMask.NameToLayer("Summoner");
        playerMask = 1 << LayerMask.NameToLayer("Player");

        playerModifier.attackSpeed = playerAttackSpeedMult;
        playerModifier.damageTaken = -playerDamageTakenReduction;

        summonerModifier.attackSpeed = summonerAttackSpeedMult;

        if (ownerHealth != null)
            ownerHealth.OnDeath += OnOwnerDeath;

        auraVFX = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (auraVFX != null)
        {
            auraRenderer = auraVFX.GetComponent<Renderer>();
            if (auraRenderer != null)
            {
                auraRenderer.material = new Material(auraRenderer.material);
                auraRenderer.material.color = auraColor;
            }
            auraVFX.localScale = new Vector3(auraRadius * 2f, auraVFX.localScale.y, auraRadius * 2f);
        }
    }

    private void OnOwnerDeath()
    {
        RemoveAllBuffs();
        enabled = false;
        if (auraVFX != null)
        {
            Destroy(auraVFX.gameObject);
            auraVFX = null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        RemoveAllBuffs();
        if (auraVFX != null)
            Destroy(auraVFX.gameObject);
    }

    private void Update()
    {
        if (ownerStats != null)
            transform.position = ownerStats.transform.position;

        UpdatePlayerBuff();
        UpdateSummonerBuffs();
        DoRegen();

        if (auraRenderer != null)
        {
            float alpha = Mathf.Lerp(auraPulseAlphaMin, auraPulseAlphaMax,
                (Mathf.Sin(Time.time * auraPulseSpeed) + 1f) * 0.5f);
            Color c = auraColor;
            c.a = alpha;
            auraRenderer.material.color = c;
        }

        if (auraVFX != null)
        {
            Vector3 pos = transform.position;
            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
                pos.y = hit.point.y + groundOffset;
            auraVFX.position = pos;
        }
    }

    private void UpdatePlayerBuff()
    {
        bool inAura = Physics.CheckSphere(transform.position, auraRadius, playerMask);

        if (inAura && !playerBuffed && !playerBuffedByAura)
        {
            playerStats?.AddMultiplierModifier(playerModifier);
            playerBuffed = true;
            playerBuffedByAura = true;
        }
        else if (!inAura && playerBuffed)
        {
            playerStats?.RemoveMultiplierModifier(playerModifier);
            playerBuffed = false;
            playerBuffedByAura = false;
        }
    }

    private void UpdateSummonerBuffs()
    {
        var hits = Physics.OverlapSphere(transform.position, auraRadius, summonerMask);
        var inAura = new HashSet<EntityStats>();

        foreach (var hit in hits)
        {
            var stats = hit.GetComponentInParent<EntityStats>();
            if (stats == null) continue;
            inAura.Add(stats);
            if (!buffedSummoners.Contains(stats) && !globalBuffedSummoners.Contains(stats))
            {
                stats.AddMultiplierModifier(summonerModifier);
                buffedSummoners.Add(stats);
                globalBuffedSummoners.Add(stats);
            }
        }

        var toRemove = new List<EntityStats>();
        foreach (var stats in buffedSummoners)
        {
            if (!inAura.Contains(stats))
            {
                stats.RemoveMultiplierModifier(summonerModifier);
                globalBuffedSummoners.Remove(stats);
                toRemove.Add(stats);
            }
        }
        foreach (var s in toRemove) buffedSummoners.Remove(s);
    }

    private void DoRegen()
    {
        if (ownerStats == null) return;
        float regenAmount = ownerStats.MaxHP * regenPercent;

        var summonerHits = Physics.OverlapSphere(transform.position, auraRadius, summonerMask);
        foreach (var hit in summonerHits)
        {
            var health = hit.GetComponentInParent<HealthBase>();
            if (health == null) continue;
            if (lastRegenTimePerEntity.TryGetValue(health, out float last))
                if (Time.time - last < regenInterval) continue;
            health.Heal(regenAmount);
            lastRegenTimePerEntity[health] = Time.time;
        }
    }

    private void RemoveAllBuffs()
    {
        if (playerBuffed)
        {
            playerStats?.RemoveMultiplierModifier(playerModifier);
            playerBuffed = false;
            playerBuffedByAura = false;
        }

        foreach (var stats in buffedSummoners)
        {
            stats?.RemoveMultiplierModifier(summonerModifier);
            globalBuffedSummoners.Remove(stats);
        }
        buffedSummoners.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}