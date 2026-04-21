using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class RunManager : MonoBehaviour
{
    public static RunManager Instance { get; private set; }

    [Header("Healing")]
    [Tooltip("% after clearing")]
    [Range(0f, 1f)]
    public float HealPerRoom = 0f;

    [Header("Reroll")]
    public bool AllowReroll = false;

    [Header("Run Stats")]
    public int CurrentFloor = 1;
    public int TotalEnemyKilled = 0;
    public int TotalEventsFound = 0;
    public int TotalBossKilled = 0;
    public bool IsWin = false;

    [Header("Floor Event Tracking")]

    public List<RoomType> PreviousFloorEvents = new();

    public List<RoomType> CurrentFloorEvents = new();

    [Header("Current Run Tracking")]

    public int TotalCoinsCollected = 0;

    public float TotalDamageTaken = 0f;

    public int TotalHeals = 0;

    public int TotalItemsCollected = 0;

    public int TotalRoomsCleared = 0;

    public int HighestFloorReached = 1;

    // ── Floor Modifier System ────────────────────────────────────────────────

    [Header("Floor Modifiers")]
    [Tooltip("Absolute floor numbers after which a card selection is offered (e.g. 1,3,5,7).")]
    public int[] modifierTriggerFloors = { 1, 3, 5, 7 };
    [Tooltip("How many cards to offer at each trigger floor.")]
    [Range(2, 3)]
    public int modifierCardCount = 3;

    public RunModifiers PermanentMods = new();   // persist for the whole run
    public RunModifiers NextFloorMods = new();   // cleared after each boss kill

    readonly HashSet<string> _pickedCardIds = new();

    public readonly List<FloorModifierCard> AppliedWholeRun  = new();
    public readonly List<FloorModifierCard> AppliedNextFloor = new();

    public float EffectiveCoinMultiplier => PermanentMods.coinMultiplier * NextFloorMods.coinMultiplier;
    public float EffectiveEnemyCountMult => PermanentMods.enemyCountMultiplier * NextFloorMods.enemyCountMultiplier;
    public int EffectiveEliteBudgetBonus => PermanentMods.eliteBudgetBonus + NextFloorMods.eliteBudgetBonus;
    public int EffectiveExtraWaves => PermanentMods.extraWaves + NextFloorMods.extraWaves;
    public float EffectiveLootMeanBonus => PermanentMods.lootMeanBonus + NextFloorMods.lootMeanBonus;
    public int EffectiveExtraLootOptions => PermanentMods.extraLootOptions + NextFloorMods.extraLootOptions;
    public int EffectiveShopPool => PermanentMods.extraShopPool + NextFloorMods.extraShopPool;
    public int EffectiveExtraEventRoomMin => PermanentMods.extraEventRoomMin + NextFloorMods.extraEventRoomMin;
    public int EffectiveExtraBattleRoomMin => PermanentMods.extraBattleRoomMin + NextFloorMods.extraBattleRoomMin;
    public float EffectiveHealPerRoomBonus => PermanentMods.healPerRoomBonus + NextFloorMods.healPerRoomBonus;
    public int EffectiveBonusCoinsOnEntry => PermanentMods.bonusCoinsOnFloorEntry + NextFloorMods.bonusCoinsOnFloorEntry;
    public float EffectiveSellMultiplier => PermanentMods.sellPriceMultiplier * NextFloorMods.sellPriceMultiplier;
    public float EffectiveShopDiscount => Mathf.Clamp01(PermanentMods.shopDiscount + NextFloorMods.shopDiscount);
    public float EffectiveUpgradeDiscount => Mathf.Clamp01(PermanentMods.upgradeDiscount + NextFloorMods.upgradeDiscount);
    public float EffectiveHealDiscount => Mathf.Clamp01(PermanentMods.healDiscount + NextFloorMods.healDiscount);
    public float EffectiveMergeValueMultiplier => PermanentMods.mergeValueMultiplier * NextFloorMods.mergeValueMultiplier;
    public float EffectiveMergeSpreadMultiplier => PermanentMods.mergeSpreadMultiplier * NextFloorMods.mergeSpreadMultiplier;
    public bool EffectiveMergeGuaranteeSameRarity => PermanentMods.mergeGuaranteeSameRarity || NextFloorMods.mergeGuaranteeSameRarity;
    public int EffectiveMergeRarityBonus => PermanentMods.mergeRarityBonus + NextFloorMods.mergeRarityBonus;
    public float EffectiveEnemyHpMultiplier => PermanentMods.enemyHpMultiplier * NextFloorMods.enemyHpMultiplier;
    public float EffectiveEnemyDamageMultiplier => PermanentMods.enemyDamageMultiplier * NextFloorMods.enemyDamageMultiplier;
    public float EffectiveEnemySpeedMultiplier => PermanentMods.enemySpeedMultiplier * NextFloorMods.enemySpeedMultiplier;
    public float EffectiveLootChanceBias => PermanentMods.lootChanceBias + NextFloorMods.lootChanceBias;

    // ────────────────────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void RegisterEventRoomPlaced(RoomType type)
    {
        if (!CurrentFloorEvents.Contains(type))
            CurrentFloorEvents.Add(type);
    }


    void RotateFloorEvents()
    {
        PreviousFloorEvents = new List<RoomType>(CurrentFloorEvents);
        CurrentFloorEvents.Clear();
    }


    public bool WasMissingLastFloor(RoomType type)
    {
        return !PreviousFloorEvents.Contains(type);
    }



    public static event Action OnRoomClearedEvent;
    public static event Action<EnemyTier> OnEnemyKilledWithTierEvent;

    public void OnEnemyKilled(EnemyTier tier = EnemyTier.Normal)
    {
        TotalEnemyKilled++;
        OnEnemyKilledWithTierEvent?.Invoke(tier);
    }

    public void OnEventRoomEntered()
    {
        TotalEventsFound++;
    }

    public void OnRoomCleared()
    {
        TotalRoomsCleared++;
        OnRoomClearedEvent?.Invoke();
    }

    public void OnBossKilled()
    {
        TotalBossKilled++;
        RotateFloorEvents();
    }

    public void OnCoinsCollected(int amount)
    {
        TotalCoinsCollected += amount;
    }

    public void OnDamageTaken(float amount)
    {
        TotalDamageTaken += amount;
    }

    public void OnHealed()
    {
        TotalHeals++;
    }

    public void OnItemCollected()
    {
        TotalItemsCollected++;
    }

    public void ResetRun()
    {
        CurrentFloor = 1;
        TotalEnemyKilled = 0;
        TotalEventsFound = 0;
        TotalBossKilled = 0;
        TotalCoinsCollected = 0;
        TotalDamageTaken = 0f;
        TotalHeals = 0;
        TotalItemsCollected = 0;
        TotalRoomsCleared = 0;
        HighestFloorReached = 1;
        IsWin = false;
        PreviousFloorEvents.Clear();
        CurrentFloorEvents.Clear();
        PermanentMods = new RunModifiers();
        NextFloorMods = new RunModifiers();
        _pickedCardIds.Clear();
        AppliedWholeRun.Clear();
        AppliedNextFloor.Clear();
        HealthStationManager.Instance?.ResetRun();
        LuckStationManager.Instance?.ResetRun();
        EnemyPoolManager.Instance?.RebuildPools();
    }

    // ── Floor modifier card logic ────────────────────────────────────────────

    bool IsTriggerFloor(int floor)
    {
        if (modifierTriggerFloors == null) return false;
        foreach (var f in modifierTriggerFloors)
            if (f == floor) return true;
        return false;
    }

    FloorModifierCard[] DrawModifierCards(int count)
    {
        var available = new List<FloorModifierCard>();
        foreach (var card in FloorModifierCardRegistry.GetAllCards())
            if (!_pickedCardIds.Contains(card.cardId))
                available.Add(card);

        var result = new List<FloorModifierCard>();
        while (result.Count < count && available.Count > 0)
        {
            int idx = Random.Range(0, available.Count);
            result.Add(available[idx]);
            available.RemoveAt(idx);
        }
        return result.ToArray();
    }

    public void ApplyModifierCard(FloorModifierCard card)
    {
        if (card == null) return;
        _pickedCardIds.Add(card.cardId);
        if (card.scope == ModifierScope.WholeRun)
        {
            PermanentMods.Add(card.modifier);
            AppliedWholeRun.Add(card);
        }
        else
        {
            NextFloorMods.Add(card.modifier);
            AppliedNextFloor.Add(card);
        }
    }

    /// <summary>Called by StartPortal — plays the fade and loads without touching floor state.</summary>
    public void StartRun(int sceneIndex)
    {
        StartCoroutine(StartRunRoutine(sceneIndex));
    }

    /// <summary>Called by NextFloorPortal — shows modifier selection, increments floor, then loads.</summary>
    public void StartFloorTransition(int sceneIndex)
    {
        StartCoroutine(FloorTransitionRoutine(sceneIndex));
    }

    private IEnumerator StartRunRoutine(int sceneIndex)
    {
        var uiManager = UIManager.Instance;
        if (uiManager != null)
            yield return StartCoroutine(uiManager.PlayFloorTransition());
        SceneManager.LoadScene(sceneIndex);
    }

    private IEnumerator FloorTransitionRoutine(int sceneIndex)
    {
        var uiManager = UIManager.Instance;

        // Expire the previous floor's next-floor mods before selecting new ones.
        NextFloorMods = new RunModifiers();
        AppliedNextFloor.Clear();

        // Advance floor so modifier cards know which floor they apply to.
        CurrentFloor++;
        HighestFloorReached = Mathf.Max(HighestFloorReached, CurrentFloor);

        // Show modifier card selection for the destination floor.
        if (IsTriggerFloor(CurrentFloor - 1) && uiManager != null)
        {
            var cards = DrawModifierCards(modifierCardCount);
            if (cards.Length > 0)
                yield return StartCoroutine(uiManager.PlayFloorModifierSelection(cards));
        }

        if (uiManager != null)
            yield return StartCoroutine(uiManager.PlayFloorTransition());

        SceneManager.LoadScene(sceneIndex);
    }
}