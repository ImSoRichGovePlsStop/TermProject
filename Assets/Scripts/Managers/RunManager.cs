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
    public float HealPerRoom = 0.10f;

    [Header("Reroll")]
    public bool AllowReroll = true;

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

    public float EffectiveCoinMultiplier     => PermanentMods.coinMultiplier         * NextFloorMods.coinMultiplier;
    public float EffectiveEnemyCountMult     => PermanentMods.enemyCountMultiplier   * NextFloorMods.enemyCountMultiplier;
    public int   EffectiveEliteBudgetBonus   => PermanentMods.eliteBudgetBonus       + NextFloorMods.eliteBudgetBonus;
    public int   EffectiveExtraWaves         => PermanentMods.extraWaves             + NextFloorMods.extraWaves;
    public float EffectiveLootMeanBonus      => PermanentMods.lootMeanBonus          + NextFloorMods.lootMeanBonus;
    public int   EffectiveExtraLootOptions   => PermanentMods.extraLootOptions       + NextFloorMods.extraLootOptions;
    public int   EffectiveExtraEventRoomMin  => PermanentMods.extraEventRoomMin      + NextFloorMods.extraEventRoomMin;
    public int   EffectiveExtraBattleRoomMin => PermanentMods.extraBattleRoomMin     + NextFloorMods.extraBattleRoomMin;
    public float EffectiveHealPerRoomBonus   => PermanentMods.healPerRoomBonus       + NextFloorMods.healPerRoomBonus;
    public int   EffectiveBonusCoinsOnEntry  => PermanentMods.bonusCoinsOnFloorEntry + NextFloorMods.bonusCoinsOnFloorEntry;
    public float EffectiveLootChanceBias     => PermanentMods.lootChanceBias         + NextFloorMods.lootChanceBias;
    public float EffectiveSellMultiplier        => PermanentMods.sellPriceMultiplier   * NextFloorMods.sellPriceMultiplier;
    public float EffectiveShopDiscount          => Mathf.Clamp01(PermanentMods.shopDiscount     + NextFloorMods.shopDiscount);
    public float EffectiveUpgradeDiscount       => Mathf.Clamp01(PermanentMods.upgradeDiscount  + NextFloorMods.upgradeDiscount);
    public float EffectiveHealDiscount          => Mathf.Clamp01(PermanentMods.healDiscount     + NextFloorMods.healDiscount);
    public float EffectiveEnemyHpMultiplier     => PermanentMods.enemyHpMultiplier     * NextFloorMods.enemyHpMultiplier;
    public float EffectiveEnemyDamageMultiplier => PermanentMods.enemyDamageMultiplier * NextFloorMods.enemyDamageMultiplier;
    public float EffectiveEnemySpeedMultiplier  => PermanentMods.enemySpeedMultiplier  * NextFloorMods.enemySpeedMultiplier;
    public float EffectiveMergeValueMultiplier  => PermanentMods.mergeValueMultiplier  * NextFloorMods.mergeValueMultiplier;
    public float EffectiveMergeSpreadMultiplier => PermanentMods.mergeSpreadMultiplier * NextFloorMods.mergeSpreadMultiplier;
    public bool  EffectiveMergeGuaranteeSameRarity => PermanentMods.mergeGuaranteeSameRarity || NextFloorMods.mergeGuaranteeSameRarity;
    public int   EffectiveMergeRarityBonus      => PermanentMods.mergeRarityBonus      + NextFloorMods.mergeRarityBonus;

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



    public void OnEnemyKilled()
    {
        TotalEnemyKilled++;
    }

    public void OnEventRoomEntered()
    {
        TotalEventsFound++;
    }

    public void OnRoomCleared()
    {
        TotalRoomsCleared++;
    }

    public void OnBossKilled()
    {
        TotalBossKilled++;
        RotateFloorEvents();
        NextFloorMods = new RunModifiers();   // clear next-floor mods before new card selection
        CurrentFloor++;
        HighestFloorReached = Mathf.Max(HighestFloorReached, CurrentFloor);
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
        PermanentMods  = new RunModifiers();
        NextFloorMods  = new RunModifiers();
        _pickedCardIds.Clear();
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
            PermanentMods.Add(card.modifier);
        else
            NextFloorMods.Add(card.modifier);
    }

    public void StartFloorTransition(int sceneIndex)
    {
        StartCoroutine(FloorTransitionRoutine(sceneIndex));
    }

    private IEnumerator FloorTransitionRoutine(int sceneIndex)
    {
        var uiManager = UIManager.Instance;

        // CurrentFloor was already incremented in OnBossKilled; floor just completed is CurrentFloor - 1
        int justCompleted = CurrentFloor - 1;
        if (IsTriggerFloor(justCompleted) && uiManager != null)
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
