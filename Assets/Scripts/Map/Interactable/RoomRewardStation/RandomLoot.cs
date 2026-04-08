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

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private UIManager uiManager;

    public string GetPromptText() => "[ E ]  Open Loot";

    public void Configure(LootConfig cfg)
    {
        optionCount = cfg.optionCount;
        meanCost = cfg.meanCost;
        sd = cfg.sd;
        allowDuplicates = cfg.allowDuplicates;
    }

    private void Start()
    {
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
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

        uiManager?.OpenRewardLoot(this, rolled);
        Destroy(gameObject);
    }
}