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
    [SerializeField] private float summonerSpeedMult = 0.4f;
    [SerializeField] private float summonerAttackSpeedMult = 0.25f;
    [SerializeField] private float playerSpeedMult = 0.2f;
    [SerializeField] private float playerAttackSpeedMult = 0.25f;
    [SerializeField] private float totemSpawnMult = 0.5f;

    private EntityStats ownerStats;
    private PlayerStats playerStats;

    private LayerMask summonerMask;
    private LayerMask totemMask;
    private LayerMask playerMask;

    private StatModifier playerModifier = new StatModifier();
    private EntityStatModifier summonerModifier = new EntityStatModifier();

    private HashSet<EntityStats> buffedSummoners = new HashSet<EntityStats>();
    private HashSet<Totem> buffedTotems = new HashSet<Totem>();
    private bool playerBuffed = false;

    private static bool playerBuffedByAura = false;
    private static HashSet<EntityStats> globalBuffedSummoners = new HashSet<EntityStats>();
    private static HashSet<Totem> globalBuffedTotems = new HashSet<Totem>();
    private static Dictionary<HealthBase, float> lastRegenTimePerEntity = new Dictionary<HealthBase, float>();


    public void Init(EntityStats stats, PlayerStats pStats, HealthBase ownerHealth)
    {
        ownerStats = stats;
        playerStats = pStats;

        summonerMask = 1 << LayerMask.NameToLayer("Summoner");
        totemMask = 1 << LayerMask.NameToLayer("Totem");
        playerMask = 1 << LayerMask.NameToLayer("Player");

        playerModifier.moveSpeed = playerSpeedMult;
        playerModifier.attackSpeed = playerAttackSpeedMult;

        summonerModifier.moveSpeed = summonerSpeedMult;
        summonerModifier.attackSpeed = summonerAttackSpeedMult;

        if (ownerHealth != null)
            ownerHealth.OnDeath += OnOwnerDeath;

        // Find cylinder child
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
        UpdateTotemBuffs();
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

    private void UpdateTotemBuffs()
    {
        var hits = Physics.OverlapSphere(transform.position, auraRadius, totemMask);
        var inAura = new HashSet<Totem>();

        foreach (var hit in hits)
        {
            var totem = hit.GetComponentInParent<Totem>();
            if (totem == null) continue;
            inAura.Add(totem);
            if (!buffedTotems.Contains(totem) && !globalBuffedTotems.Contains(totem))
            {
                totem.auraSpeedMult = totemSpawnMult;
                buffedTotems.Add(totem);
                globalBuffedTotems.Add(totem);
            }
        }

        var toRemove = new List<Totem>();
        foreach (var totem in buffedTotems)
        {
            if (!inAura.Contains(totem))
            {
                globalBuffedTotems.Remove(totem);
                if (!globalBuffedTotems.Contains(totem))
                    totem.auraSpeedMult = 1f;
                toRemove.Add(totem);
            }
        }
        foreach (var t in toRemove) buffedTotems.Remove(t);
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

        var totemHits = Physics.OverlapSphere(transform.position, auraRadius, totemMask);
        foreach (var hit in totemHits)
        {
            var totem = hit.GetComponentInParent<Totem>();
            if (totem == null) continue;
            if (lastRegenTimePerEntity.TryGetValue(totem, out float last))
                if (Time.time - last < regenInterval) continue;
            totem.AddHP(regenAmount);
            lastRegenTimePerEntity[totem] = Time.time;
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

        foreach (var totem in buffedTotems)
        {
            if (totem == null) continue;
            globalBuffedTotems.Remove(totem);
            if (!globalBuffedTotems.Contains(totem))
                totem.auraSpeedMult = 1f;
        }
        buffedTotems.Clear();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}