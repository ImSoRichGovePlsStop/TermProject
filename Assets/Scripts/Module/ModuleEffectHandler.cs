using UnityEngine;

public class ModuleEffectHandler : MonoBehaviour
{
    private PlayerStats playerStats;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();

        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        mgr.OnModuleEquipped += OnModuleEquipped;
        mgr.OnModuleUnequipped += OnModuleUnequipped;
    }

    private void OnDestroy()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        mgr.OnModuleEquipped -= OnModuleEquipped;
        mgr.OnModuleUnequipped -= OnModuleUnequipped;
    }

    private void OnModuleEquipped(ModuleInstance inst)
    {
        if (inst.Data.moduleEffect == null) return;
        inst.Data.moduleEffect.Equip(playerStats);
    }

    private void OnModuleUnequipped(ModuleInstance inst)
    {
        if (inst.Data.moduleEffect == null) return;
        inst.Data.moduleEffect.Unequip(playerStats);
    }

    private void Update()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            if (inst.Data.moduleEffect == null) continue;
            inst.Data.moduleEffect.OnUpdate(playerStats);
        }
    }
}