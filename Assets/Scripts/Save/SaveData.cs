using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable data bag written to disk as JSON.
/// Contains everything that persists between runs.
/// </summary>
[Serializable]
public class SaveData
{
    /// <summary>MaterialStorage — materials accumulated across runs.</summary>
    public List<MaterialSaveEntry> materials = new();

    /// <summary>WeaponLevelManager — level per weapon.</summary>
    public List<WeaponLevelSaveEntry> weaponLevels = new();

    /// <summary>WeaponPassiveManager — unlocked nodes + remaining points per weapon passive.</summary>
    public List<WeaponPassiveSaveEntry> passiveStates = new();

    /// <summary>HealthStationManager upgrade level.</summary>
    public int healthStationLevel;

    /// <summary>LuckStationManager upgrade level.</summary>
    public int luckStationLevel;
    /// <summary>InventoryManager bag grid upgrade level.</summary>
    public int bagGridLevel;
}

[Serializable]
public class MaterialSaveEntry
{
    public string materialName;
    public int count;
}

[Serializable]
public class WeaponLevelSaveEntry
{
    public string weaponName;
    public int level;
}

[Serializable]
public class WeaponPassiveSaveEntry
{
    /// <summary>Matches WeaponPassiveData ScriptableObject name.</summary>
    public string passiveDataName;
    /// <summary>Points not yet spent in the passive tree.</summary>
    public int availablePoints;
    public List<TreeSaveEntry> trees = new();
}

[Serializable]
public class TreeSaveEntry
{
    /// <summary>Matches GenericTreeData ScriptableObject name.</summary>
    public string treeName;
    /// <summary>Names of each unlocked GenericTreeNode SO in this tree.</summary>
    public List<string> unlockedNodeNames = new();
}