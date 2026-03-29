using UnityEngine;

public class ModuleRuntimeState
{
    public bool isActive = false;
    public float totalBuffPercent = 0f;
    public float currentStat = 0f;
    public int buffedLevel = 0;
    public Rarity buffRarity = 0;

    //HeavyHit
    public float dmgTaken = 0f;

    // LowHpModule
    public float currentThreshold = 0f;
    public bool buffActive = false;
   
}