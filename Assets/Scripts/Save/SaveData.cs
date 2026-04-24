using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable data bag written to disk as JSON.
/// Contains everything that persists between runs.
/// </summary>
[Serializable]
public class SaveData
{
    // ── Hub progression ──────────────────────────────────────────────────────
    public List<MaterialSaveEntry> materials = new();
    public List<WeaponLevelSaveEntry> weaponLevels = new();
    public List<WeaponPassiveSaveEntry> passiveStates = new();
    public int healthStationLevel;
    public int luckStationLevel;
    public int bagGridLevel;

    // ── Run stats (cumulative across all runs on this slot) ──────────────────
    public int highestFloor;
    public int totalRuns;
    public float totalTime;
    public string lastSaved;   // stored as ISO 8601 string
}

[Serializable] public class MaterialSaveEntry { public string materialName; public int count; }
[Serializable] public class WeaponLevelSaveEntry { public string weaponName; public int level; }

[Serializable]
public class WeaponPassiveSaveEntry
{
    public string passiveDataName;
    public int availablePoints;
    public List<TreeSaveEntry> trees = new();
}

[Serializable]
public class TreeSaveEntry
{
    public string treeName;
    public List<string> unlockedNodeNames = new();
}