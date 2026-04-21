using System;
using UnityEngine;

public enum ModifierScope { WholeRun, NextFloorOnly }

[Serializable]
public class RunModifiers
{
    [Header("Combat")]
    public int   eliteBudgetBonus     = 0;
    public float enemyCountMultiplier = 1f;
    public int   extraWaves           = 0;

    [Header("Economy")]
    public float coinMultiplier        = 1f;
    public float lootMeanBonus         = 0f;
    public int   extraLootOptions      = 0;
    public int   bonusCoinsOnFloorEntry = 0;
    public int extraShopPool = 0;

    [Tooltip("Additive bias to loot-vs-upgrade chance after clearing a room. Positive = more loot, negative = more upgrades. Clamped to [0,1].")]
    public float lootChanceBias        = 0f;

    [Header("Map Generation")]
    public int extraEventRoomMin  = 0;
    public int extraBattleRoomMin = 0;

    [Header("Player")]
    [Range(0f, 1f)]
    public float healPerRoomBonus = 0f;

    [Header("Sell / Discount")]
    [Tooltip("Multiplier applied to sell price (1 = no bonus, 1.5 = 50% more).")]
    public float sellPriceMultiplier = 1f;

    [Tooltip("Fraction discount at the shop (0 = full price, 0.2 = 20% off).")]
    [Range(0f, 1f)]
    public float shopDiscount = 0f;

    [Tooltip("Fraction discount on paid upgrade cost (0 = full price, 0.2 = 20% off).")]
    [Range(0f, 1f)]
    public float upgradeDiscount = 0f;

    [Tooltip("Fraction discount on paid heal cost (0 = full price, 0.2 = 20% off).")]
    [Range(0f, 1f)]
    public float healDiscount = 0f;

    [Header("Enemy Stats")]
    [Tooltip("Multiplies all enemy HP. 1 = normal, 1.4 = 40% more HP.")]
    public float enemyHpMultiplier = 1f;

    [Tooltip("Multiplies all enemy damage. 1 = normal, 1.35 = 35% more damage.")]
    public float enemyDamageMultiplier = 1f;

    [Tooltip("Multiplies all enemy move speed. 1 = normal, 1.3 = 30% faster.")]
    public float enemySpeedMultiplier = 1f;

    [Header("Merge")]
    [Tooltip("Multiplies the target output value (mean). 1 = normal, 1.3 = 30% higher value.")]
    public float mergeValueMultiplier = 1f;

    [Tooltip("Multiplies the spread/SD of merge output. <1 = tighter, >1 = wilder.")]
    public float mergeSpreadMultiplier = 1f;

    [Tooltip("If true, merge output rarity is guaranteed to be at least the average input rarity.")]
    public bool mergeGuaranteeSameRarity = false;

    [Tooltip("Flat bonus added to the minimum output rarity index (0=Common … 4=GOD). Stacks with mergeGuaranteeSameRarity.")]
    public int mergeRarityBonus = 0;

    public void Add(RunModifiers other)
    {
        eliteBudgetBonus          += other.eliteBudgetBonus;
        enemyCountMultiplier      *= other.enemyCountMultiplier;
        extraWaves                += other.extraWaves;
        coinMultiplier            *= other.coinMultiplier;
        lootMeanBonus             += other.lootMeanBonus;
        extraLootOptions          += other.extraLootOptions;
        extraShopPool             += other.extraShopPool;
        bonusCoinsOnFloorEntry    += other.bonusCoinsOnFloorEntry;
        lootChanceBias            += other.lootChanceBias;
        extraEventRoomMin         += other.extraEventRoomMin;
        extraBattleRoomMin        += other.extraBattleRoomMin;
        healPerRoomBonus          += other.healPerRoomBonus;
        sellPriceMultiplier       *= other.sellPriceMultiplier;
        shopDiscount              += other.shopDiscount;
        upgradeDiscount           += other.upgradeDiscount;
        healDiscount              += other.healDiscount;
        enemyHpMultiplier         *= other.enemyHpMultiplier;
        enemyDamageMultiplier     *= other.enemyDamageMultiplier;
        enemySpeedMultiplier      *= other.enemySpeedMultiplier;
        mergeValueMultiplier      *= other.mergeValueMultiplier;
        mergeSpreadMultiplier     *= other.mergeSpreadMultiplier;
        mergeGuaranteeSameRarity  |= other.mergeGuaranteeSameRarity;
        mergeRarityBonus          += other.mergeRarityBonus;
    }
}
