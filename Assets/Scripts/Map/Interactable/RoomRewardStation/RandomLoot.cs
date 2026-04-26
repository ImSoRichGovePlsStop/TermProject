using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct LootConfig
{
    public int optionCount;
    public float meanCost;
    public float sd;
    public bool allowDuplicates;
}

public class RandomLoot : MonoBehaviour, IInteractable
{
    [Header("Loot Settings")]
    [SerializeField] private int optionCount = 3;
    [SerializeField] private bool allowDuplicates = false;

    [Header("Cost Distribution")]
    [SerializeField] private float meanCost = 10f;
    [SerializeField] private float sd = 10f;

    [Header("Mimic")]
    public bool isMimic = false;
    [SerializeField] private float mimicChance = 0.20f;
    public GameObject mimicPrefab;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public System.Action<GameObject> OnMimicSpawned;

    private UIManager uiManager;

    public string GetPromptText() => "[ E ]  Open Loot";
    public InteractInfo GetInteractInfo()
    {
        string desc = "A sealed chest containing rewards from the depths of the dungeon.";
        if (isMimic) desc += " But this one seems to have some special energy.";
        return new InteractInfo
        {
            name        = "Loot Chest",
            description = desc,
            actionText  = "Open",
            cost        = null
        };
    }

    public void Configure(LootConfig cfg)
    {
        optionCount = cfg.optionCount;
        meanCost = cfg.meanCost;
        sd = cfg.sd;
        allowDuplicates = cfg.allowDuplicates;
    }

    /// <summary>Flags this loot chest as a mimic.</summary>
    public void EnableMimic()
    {
        isMimic = true;
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (isMimic && Random.value <= mimicChance)
        {
            if (debugLog)
                Debug.Log($"[RandomLoot] {gameObject.name} — mimic triggered!");

            if (mimicPrefab != null)
            {
                var mimic = Instantiate(mimicPrefab, transform.position + transform.forward * 0.8f, transform.rotation);
                OnMimicSpawned?.Invoke(mimic);
            }
            else
                Debug.LogWarning("[RandomLoot] Mimic triggered but mimicPrefab is not assigned.");

            Destroy(gameObject);
            return;
        }

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();

        var rolled = Randomizer.Roll(optionCount, optionCount, meanCost, sd, allowDuplicates);

        if (debugLog)
            Debug.Log($"[RandomLoot] {gameObject.name} — rolled {rolled.Count} option(s) | mean={meanCost} sd={sd}");

        if (rolled.Count == 0)
        {
            Debug.LogWarning("[RandomLoot] Randomizer returned no results.");
            return;
        }

        var cfg = new LootConfig { optionCount = optionCount, meanCost = meanCost, sd = sd, allowDuplicates = allowDuplicates };
        uiManager?.OpenRewardLoot(cfg, rolled);
        Destroy(gameObject);
    }

    public List<TestModuleEntry> RollNewOptions()
    {
        return Randomizer.Roll(optionCount, optionCount, meanCost, sd, allowDuplicates);
    }

    public virtual void OnLootPicked()
    {
        if (this != null) Destroy(gameObject);
    }
}
