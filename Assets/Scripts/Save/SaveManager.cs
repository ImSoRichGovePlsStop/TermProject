using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Singleton save manager. Place on a GameObject in scene 0 (main screen).
/// DontDestroyOnLoad keeps it alive for the entire session.
///
/// Save triggers:
///   • EndGameUI.OnContinue  — after all reset logic finishes, before LoadScene(1)
///   • Application.quitting  — auto-save guard: only if GameManager exists
///
/// Load / Apply:
///   • Awake            — reads JSON file from disk into _pendingData
///   • GameInitializer  — calls Apply() after GameManager is confirmed present
///
/// No registry required. SO lookups use Resources.FindObjectsOfTypeAll which
/// finds every SO of that type currently loaded in memory.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const  string SaveFileName = "savegame.json";
    private static string SavePath     => Path.Combine(Application.persistentDataPath, SaveFileName);

    private SaveData _pendingData;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _pendingData = LoadFromDisk();
    }

    private void OnEnable()  => Application.quitting += OnApplicationQuit;
    private void OnDisable() => Application.quitting -= OnApplicationQuit;

    private void OnApplicationQuit()
    {
        // Only save when GameManager (and its children) are alive.
        // Prevents overwriting a valid save with empty data on scene-0 quit.
        if (GameManager.Instance != null)
            Save();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Collect state from all persistent managers and write to disk.
    /// Call AFTER the end-game reset is fully complete.
    /// </summary>
    public void Save()
    {
        var data = new SaveData();
        CollectMaterials(data);
        CollectWeaponLevels(data);
        CollectPassives(data);
        CollectStations(data);
        WriteToDisk(data);
    }

    /// <summary>
    /// Push the data loaded from disk into all persistent managers.
    /// Call from GameInitializer.Awake() after GameManager is confirmed present.
    /// </summary>
    public void Apply()
    {
        if (_pendingData == null) return;

        ApplyMaterials(_pendingData);
        ApplyWeaponLevels(_pendingData);
        ApplyPassives(_pendingData);
        ApplyStations(_pendingData);

        _pendingData = null; // consumed — won't apply again this session
    }

    // ── Collect (runtime → SaveData) ─────────────────────────────────────────

    private void CollectMaterials(SaveData data)
    {
        var storage = MaterialStorage.Instance;
        if (storage == null) return;

        foreach (var kvp in storage.GetAll())
            if (kvp.Key != null && kvp.Value > 0)
                data.materials.Add(new MaterialSaveEntry
                {
                    materialName = kvp.Key.name,
                    count        = kvp.Value
                });
    }

    private void CollectWeaponLevels(SaveData data)
    {
        var wlm = WeaponLevelManager.Instance;
        if (wlm == null) return;

        foreach (var weapon in Resources.FindObjectsOfTypeAll<WeaponData>())
        {
            if (weapon == null) continue;
            int level = wlm.GetLevel(weapon);
            if (level > 1)
                data.weaponLevels.Add(new WeaponLevelSaveEntry
                {
                    weaponName = weapon.name,
                    level      = level
                });
        }
    }

    private void CollectPassives(SaveData data)
    {
        var mgr = FindFirstObjectByType<WeaponPassiveManager>();
        if (mgr == null) return;

        foreach (var weapon in Resources.FindObjectsOfTypeAll<WeaponData>())
        {
            if (weapon?.passiveData?.trees == null) continue;

            int pts   = mgr.GetAvailablePoints(weapon.passiveData);
            var entry = new WeaponPassiveSaveEntry
            {
                passiveDataName = weapon.passiveData.name,
                availablePoints = pts
            };

            foreach (var tree in weapon.passiveData.trees)
            {
                if (tree?.nodes == null) continue;

                var state     = mgr.GetState(weapon.passiveData, tree);
                var treeEntry = new TreeSaveEntry { treeName = tree.name };

                foreach (var node in state.GetUnlockedNodes())
                    if (node != null)
                        treeEntry.unlockedNodeNames.Add(node.name);

                entry.trees.Add(treeEntry);
            }

            if (pts != mgr.startingPoints || HasAnyUnlockedNode(entry))
                data.passiveStates.Add(entry);
        }
    }

    private void CollectStations(SaveData data)
    {
        data.healthStationLevel = HealthStationManager.Instance?.CurrentLevel ?? 0;
        data.luckStationLevel   = LuckStationManager.Instance?.CurrentLevel   ?? 0;
    }

    // ── Apply (SaveData → runtime) ───────────────────────────────────────────

    private void ApplyMaterials(SaveData data)
    {
        var storage = MaterialStorage.Instance;
        if (storage == null) return;

        storage.Clear();
        foreach (var entry in data.materials)
        {
            var mat = FindByName<MaterialData>(entry.materialName);
            if (mat != null)
                storage.Add(mat, entry.count);
            else
                Debug.LogWarning($"[SaveManager] MaterialData '{entry.materialName}' not found in memory.");
        }
    }

    private void ApplyWeaponLevels(SaveData data)
    {
        var wlm = WeaponLevelManager.Instance;
        if (wlm == null) return;

        foreach (var entry in data.weaponLevels)
        {
            var weapon = FindByName<WeaponData>(entry.weaponName);
            if (weapon != null)
                wlm.SetLevel(weapon, entry.level);
            else
                Debug.LogWarning($"[SaveManager] WeaponData '{entry.weaponName}' not found in memory.");
        }
    }

    private void ApplyPassives(SaveData data)
    {
        var mgr = FindFirstObjectByType<WeaponPassiveManager>();
        if (mgr == null) return;

        foreach (var entry in data.passiveStates)
        {
            // Find the weapon whose passiveData matches the saved name
            WeaponData weapon = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<WeaponData>())
            {
                if (w?.passiveData != null && w.passiveData.name == entry.passiveDataName)
                { weapon = w; break; }
            }

            if (weapon?.passiveData == null)
            {
                Debug.LogWarning($"[SaveManager] WeaponPassiveData '{entry.passiveDataName}' not found in memory.");
                continue;
            }

            mgr.SetAvailablePoints(weapon.passiveData, entry.availablePoints);

            foreach (var treeEntry in entry.trees)
            {
                var tree = Array.Find(weapon.passiveData.trees,
                    t => t != null && t.name == treeEntry.treeName);
                if (tree?.nodes == null) continue;

                var state = mgr.GetState(weapon.passiveData, tree);
                foreach (var nodeName in treeEntry.unlockedNodeNames)
                {
                    var node = Array.Find(tree.nodes, n => n != null && n.name == nodeName);
                    if (node != null)
                        state.UnlockDirect(node);
                    else
                        Debug.LogWarning($"[SaveManager] GenericTreeNode '{nodeName}' not found in tree '{treeEntry.treeName}'.");
                }
            }
        }
    }

    private void ApplyStations(SaveData data)
    {
        HealthStationManager.Instance?.SetLevel(data.healthStationLevel);
        LuckStationManager.Instance?.SetLevel(data.luckStationLevel);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static T FindByName<T>(string soName) where T : UnityEngine.Object
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll<T>())
            if (obj != null && obj.name == soName) return obj;
        return null;
    }

    private static bool HasAnyUnlockedNode(WeaponPassiveSaveEntry entry)
    {
        foreach (var t in entry.trees)
            if (t.unlockedNodeNames.Count > 0) return true;
        return false;
    }

    // ── File I/O ─────────────────────────────────────────────────────────────

    private SaveData LoadFromDisk()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveManager] No save file — starting fresh.");
            return null;
        }

        try
        {
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
            Debug.Log($"[SaveManager] Loaded save from {SavePath}");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to parse save file: {e.Message}");
            return null;
        }
    }

    private void WriteToDisk(SaveData data)
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log($"[SaveManager] Saved to {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to write save file: {e.Message}");
        }
    }

    /// <summary>Deletes the save file. Useful for a "New Game" button.</summary>
    public void DeleteSave()
    {
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        _pendingData = null;
        Debug.Log("[SaveManager] Save file deleted.");
    }

#if UNITY_EDITOR
    [ContextMenu("DEBUG — Save Now")]
    private void Debug_Save() => Save();

    [ContextMenu("DEBUG — Print Save File")]
    private void Debug_Print()
    {
        if (!File.Exists(SavePath)) { Debug.Log("[SaveManager] No save file found."); return; }
        Debug.Log(File.ReadAllText(SavePath));
    }

    [ContextMenu("DEBUG — Delete Save")]
    private void Debug_Delete() => DeleteSave();
#endif
}
