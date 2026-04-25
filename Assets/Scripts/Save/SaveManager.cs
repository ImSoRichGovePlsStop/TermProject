using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Singleton save manager — supports 4 independent save slots.
/// Each slot is stored as savegame_1.json … savegame_4.json.
///
/// Workflow:
///   1. Main menu shows SaveSlotScreenUI → player picks a slot.
///   2. SaveSlotScreenUI calls SelectSlot(slotIndex) then LoadScene(hub).
///   3. GameInitializer calls Apply() to push data into managers.
///   4. Save() is called from EndGameUI.OnContinue and OnApplicationQuit.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const int SlotCount = 4;
    public int ActiveSlot { get; private set; } = -1;   // -1 = none selected

    private static string SlotPath(int slot)
        => Path.Combine(Application.persistentDataPath, $"savegame_{slot + 1}.json");

    private SaveData _pendingData;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable() => Application.quitting += OnApplicationQuit;
    private void OnDisable() => Application.quitting -= OnApplicationQuit;

    private void OnApplicationQuit()
    {
        if (GameManager.Instance != null && ActiveSlot >= 0)
            Save();
    }

    // ── Slot selection ───────────────────────────────────────────────────────

    /// <summary>Call from SaveSlotScreenUI before loading the hub scene.</summary>
    public void SelectSlot(int slot)
    {
        ActiveSlot = slot;
        _pendingData = LoadFromDisk(slot);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public void Save()
    {
        if (ActiveSlot < 0) return;

        var existing = LoadFromDisk(ActiveSlot) ?? new SaveData();

        var data = new SaveData();
        CollectMaterials(data);
        CollectWeaponLevels(data);
        CollectPassives(data);
        CollectStations(data);

        // Accumulate run stats
        data.highestFloor = Mathf.Max(existing.highestFloor, RunManager.Instance?.HighestFloorReached ?? 0);
        data.totalRuns = existing.totalRuns + (RunManager.Instance?.TotalRuns ?? 0);
        data.totalTime = existing.totalTime + (RunManager.Instance?.RunTime ?? 0f);
        data.lastSaved = DateTime.Now.ToString("dd MMM yyyy  HH:mm");

        WriteToDisk(ActiveSlot, data);
    }

    public void Apply()
    {
        if (_pendingData == null) return;
        ApplyMaterials(_pendingData);
        ApplyWeaponLevels(_pendingData);
        ApplyPassives(_pendingData);
        ApplyStations(_pendingData);
        _pendingData = null;
    }

    // ── Slot queries (used by SaveSlotScreenUI) ──────────────────────────────

    public bool HasSave(int slot) => File.Exists(SlotPath(slot));
    public SaveData ReadSlot(int slot) => LoadFromDisk(slot);

    public void DeleteSlot(int slot)
    {
        var path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        if (ActiveSlot == slot) { ActiveSlot = -1; _pendingData = null; }
        Debug.Log($"[SaveManager] Slot {slot + 1} deleted.");
    }

    // ── Legacy helpers (kept for MainMenuUI backward compat) ─────────────────
    public bool HasSave() => ActiveSlot >= 0 && HasSave(ActiveSlot);
    public void DeleteSave() => DeleteSlot(ActiveSlot);

    // ── Collect ──────────────────────────────────────────────────────────────

    private void CollectMaterials(SaveData data)
    {
        var storage = MaterialStorage.Instance;
        if (storage == null) return;
        foreach (var kvp in storage.GetAll())
            if (kvp.Key != null && kvp.Value > 0)
                data.materials.Add(new MaterialSaveEntry { materialName = kvp.Key.name, count = kvp.Value });
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
                data.weaponLevels.Add(new WeaponLevelSaveEntry { weaponName = weapon.name, level = level });
        }
    }

    private void CollectPassives(SaveData data)
    {
        var mgr = FindFirstObjectByType<WeaponPassiveManager>();
        if (mgr == null) return;
        foreach (var weapon in Resources.FindObjectsOfTypeAll<WeaponData>())
        {
            if (weapon?.passiveData?.trees == null) continue;
            int pts = mgr.GetAvailablePoints(weapon.passiveData);
            var entry = new WeaponPassiveSaveEntry { passiveDataName = weapon.passiveData.name, availablePoints = pts };
            foreach (var tree in weapon.passiveData.trees)
            {
                if (tree?.nodes == null) continue;
                var state = mgr.GetState(weapon.passiveData, tree);
                var treeEntry = new TreeSaveEntry { treeName = tree.name };
                foreach (var node in state.GetUnlockedNodes())
                    if (node != null) treeEntry.unlockedNodeNames.Add(node.name);
                entry.trees.Add(treeEntry);
            }
            if (pts != mgr.startingPoints || HasAnyUnlockedNode(entry))
                data.passiveStates.Add(entry);
        }
    }

    private void CollectStations(SaveData data)
    {
        data.healthStationLevel = HealthStationManager.Instance?.CurrentLevel ?? 0;
        data.luckStationLevel = LuckStationManager.Instance?.CurrentLevel ?? 0;
        data.bagGridLevel = InventoryManager.Instance?.BagGridLevel ?? 0;
    }

    // ── Apply ─────────────────────────────────────────────────────────────────

    private void ApplyMaterials(SaveData data)
    {
        var storage = MaterialStorage.Instance;
        if (storage == null) return;
        storage.Clear();
        foreach (var entry in data.materials)
        {
            var mat = FindByName<MaterialData>(entry.materialName);
            if (mat != null) storage.Add(mat, entry.count);
            else Debug.LogWarning($"[SaveManager] MaterialData '{entry.materialName}' not found.");
        }
    }

    private void ApplyWeaponLevels(SaveData data)
    {
        var wlm = WeaponLevelManager.Instance;
        if (wlm == null) return;
        foreach (var entry in data.weaponLevels)
        {
            var weapon = FindByName<WeaponData>(entry.weaponName);
            if (weapon != null) wlm.SetLevel(weapon, entry.level);
            else Debug.LogWarning($"[SaveManager] WeaponData '{entry.weaponName}' not found.");
        }
    }

    private void ApplyPassives(SaveData data)
    {
        var mgr = FindFirstObjectByType<WeaponPassiveManager>();
        if (mgr == null) return;
        foreach (var entry in data.passiveStates)
        {
            WeaponData weapon = null;
            foreach (var w in Resources.FindObjectsOfTypeAll<WeaponData>())
                if (w?.passiveData != null && w.passiveData.name == entry.passiveDataName) { weapon = w; break; }

            if (weapon?.passiveData == null)
            { Debug.LogWarning($"[SaveManager] WeaponPassiveData '{entry.passiveDataName}' not found."); continue; }

            mgr.SetAvailablePoints(weapon.passiveData, entry.availablePoints);
            foreach (var treeEntry in entry.trees)
            {
                var tree = Array.Find(weapon.passiveData.trees, t => t != null && t.name == treeEntry.treeName);
                if (tree?.nodes == null) continue;
                var state = mgr.GetState(weapon.passiveData, tree);
                foreach (var nodeName in treeEntry.unlockedNodeNames)
                {
                    var node = Array.Find(tree.nodes, n => n != null && n.name == nodeName);
                    if (node != null) state.UnlockDirect(node);
                    else Debug.LogWarning($"[SaveManager] Node '{nodeName}' not found in tree '{treeEntry.treeName}'.");
                }
            }
        }
    }

    private void ApplyStations(SaveData data)
    {
        HealthStationManager.Instance?.SetLevel(data.healthStationLevel);
        LuckStationManager.Instance?.SetLevel(data.luckStationLevel);
        InventoryManager.Instance?.SetBagGridLevel(data.bagGridLevel);
    }

    // ── File I/O ─────────────────────────────────────────────────────────────

    private SaveData LoadFromDisk(int slot)
    {
        var path = SlotPath(slot);
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(path)); }
        catch (Exception e) { Debug.LogError($"[SaveManager] Failed to parse slot {slot + 1}: {e.Message}"); return null; }
    }

    private void WriteToDisk(int slot, SaveData data)
    {
        try { File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, prettyPrint: true)); }
        catch (Exception e) { Debug.LogError($"[SaveManager] Failed to write slot {slot + 1}: {e.Message}"); }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

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

#if UNITY_EDITOR
    [ContextMenu("DEBUG — Save Now")] private void Debug_Save() => Save();
    [ContextMenu("DEBUG — Delete Active Slot")] private void Debug_Delete() => DeleteSave();
    [ContextMenu("DEBUG — Print Active Slot")]
    private void Debug_Print()
    {
        if (ActiveSlot < 0) { Debug.Log("[SaveManager] No active slot."); return; }
        var path = SlotPath(ActiveSlot);
        if (!File.Exists(path)) { Debug.Log("[SaveManager] No save file found."); return; }
        Debug.Log(File.ReadAllText(path));
    }
#endif
}