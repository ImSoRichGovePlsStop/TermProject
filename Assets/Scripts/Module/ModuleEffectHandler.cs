using System.Collections.Generic;
using UnityEngine;

public class ModuleEffectHandler : MonoBehaviour
{
    private PlayerStats playerStats;

    private void Awake()
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
        if (inst.Data.moduleEffect != null)
            inst.Data.moduleEffect.Equip(playerStats);

        if (inst.Data.isBuffAdjacent && inst.Data.moduleEffect != null)
        {
            var buffModule = inst.Data.moduleEffect as BuffPercentModule;
            if (buffModule != null)
            {
                var buffCells = inst.GetAbsoluteBuffCells();
                var buffed = GetUniqueModulesAtCells(buffCells, inst);

                foreach (var target in buffed)
                {
                    if (target.Data.moduleEffect == null) continue;
                    inst.Data.moduleEffect.ApplyBuff(
                        target.Data.moduleEffect, playerStats,
                        buffModule.buffPercent
                    );
                    inst.buffTargets.Add(target);
                }
            }
        }

        foreach (var other in InventoryManager.Instance.WeaponGrid.GetAllModules())
        {
            if (other == inst) continue;
            if (!other.Data.isBuffAdjacent) continue;
            if (other.Data.moduleEffect == null) continue;

            var otherBuffModule = other.Data.moduleEffect as BuffPercentModule;
            if (otherBuffModule == null) continue;

            var buffCells = other.GetAbsoluteBuffCells();
            if (IsCoveredBy(inst, buffCells))
            {
                other.Data.moduleEffect.ApplyBuff(
                    inst.Data.moduleEffect, playerStats,
                    otherBuffModule.buffPercent
                );
                other.buffTargets.Add(inst);
            }
        }
    }

    private void OnModuleUnequipped(ModuleInstance inst)
    {
        if (inst.Data.moduleEffect != null)
            inst.Data.moduleEffect.Unequip(playerStats);

        if (inst.Data.isBuffAdjacent && inst.Data.moduleEffect != null)
        {
            var buffModule = inst.Data.moduleEffect as BuffPercentModule;
            if (buffModule != null)
            {
                foreach (var target in inst.buffTargets)
                {
                    if (target.Data.moduleEffect == null) continue;
                    inst.Data.moduleEffect.RemoveBuff(
                        target.Data.moduleEffect, playerStats,
                        buffModule.buffPercent
                    );
                }
                inst.buffTargets.Clear();
            }
        }

        foreach (var other in InventoryManager.Instance.WeaponGrid.GetAllModules())
        {
            if (other == inst) continue;
            if (!other.Data.isBuffAdjacent) continue;
            if (!other.buffTargets.Contains(inst)) continue;
            if (other.Data.moduleEffect == null) continue;

            var otherBuffModule = other.Data.moduleEffect as BuffPercentModule;
            if (otherBuffModule == null) continue;

            other.Data.moduleEffect.RemoveBuff(
                inst.Data.moduleEffect, playerStats,
                otherBuffModule.buffPercent
            );
            other.buffTargets.Remove(inst);
        }
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

    private List<ModuleInstance> GetUniqueModulesAtCells(List<Vector2Int> cells, ModuleInstance exclude)
    {
        var result = new HashSet<ModuleInstance>();
        foreach (var cell in cells)
        {
            var found = InventoryManager.Instance.WeaponGrid.GetModuleAt(cell);
            if (found != null && found != exclude)
                result.Add(found);
        }
        return new List<ModuleInstance>(result);
    }

    private bool IsCoveredBy(ModuleInstance inst, List<Vector2Int> buffCells)
    {
        var instCells = inst.GetAbsoluteCells();
        foreach (var cell in instCells)
        {
            if (buffCells.Contains(cell))
                return true;
        }
        return false;
    }
}