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

    [Header("Map Generation")]
    public int extraEventRoomMin  = 0;
    public int extraBattleRoomMin = 0;

    [Header("Player")]
    [Range(0f, 1f)]
    public float healPerRoomBonus = 0f;

    public void Add(RunModifiers other)
    {
        eliteBudgetBonus       += other.eliteBudgetBonus;
        enemyCountMultiplier   *= other.enemyCountMultiplier;
        extraWaves             += other.extraWaves;
        coinMultiplier         *= other.coinMultiplier;
        lootMeanBonus          += other.lootMeanBonus;
        extraLootOptions       += other.extraLootOptions;
        bonusCoinsOnFloorEntry += other.bonusCoinsOnFloorEntry;
        extraEventRoomMin      += other.extraEventRoomMin;
        extraBattleRoomMin     += other.extraBattleRoomMin;
        healPerRoomBonus       += other.healPerRoomBonus;
    }
}
