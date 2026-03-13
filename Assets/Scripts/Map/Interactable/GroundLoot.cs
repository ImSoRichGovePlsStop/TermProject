using System.Collections.Generic;
using UnityEngine;

public class GroundLoot : MonoBehaviour, IInteractable
{
    [SerializeField] private TestModuleEntry[] lootModules;

    private readonly List<ModuleInstance> _spawnedInstances = new List<ModuleInstance>();
    private bool _initialized = false;

    public void Interact(PlayerController playerController)
    {
        var mgr = InventoryManager.Instance;
        var uiMgr = Object.FindAnyObjectByType<UIManager>();

        if (mgr == null || uiMgr == null) return;

 
        foreach (var inst in new List<ModuleInstance>(mgr.EnvGrid.GetAllModules()))
            mgr.EnvGrid.Remove(inst);

 
        if (!_initialized)
        {
            foreach (var entry in lootModules)
            {
                if (entry.data == null) continue;
                _spawnedInstances.Add(new ModuleInstance(entry.data, entry.rarity, entry.level));
            }
            _initialized = true;
        }

        
        foreach (var inst in _spawnedInstances)
        {
            bool placed = false;
            for (int row = 0; row < mgr.EnvGrid.Height && !placed; row++)
                for (int col = 0; col < mgr.EnvGrid.Width && !placed; col++)
                    if (mgr.EnvGrid.TryPlace(inst, new Vector2Int(col, row)))
                        placed = true;

            if (!placed)
                Debug.LogWarning($"[GroundLoot] No space in EnvGrid for {inst.Data.moduleName}");
        }

  
        if (!uiMgr.IsInventoryOpen)
            uiMgr.ToggleInventory();
    }
}