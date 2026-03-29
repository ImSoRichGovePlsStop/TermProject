using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

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
            inst.Data.moduleEffect.Equip(playerStats, inst.Rarity, inst.Level, inst.RuntimeState);

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
                        buffModule.GetBuffPercent(inst.RuntimeState),
                        target.RuntimeState
                    );
                    inst.buffTargets.Add(target);
                }
            }
            var buffLevelModule = inst.Data.moduleEffect as BuffLevelModule;
            if (buffLevelModule != null)
            {
                var buffCells = inst.GetAbsoluteBuffCells();
                var buffed = GetUniqueModulesAtCells(buffCells, inst);
                int bonus = (int)buffLevelModule.GetBuffLevel(inst.RuntimeState);

                foreach (var target in buffed)
                {
                    if (target.Data.moduleEffect == null) continue;
                    target.Data.moduleEffect.OnLevelBuffReceived(target.Level, bonus, target.Rarity, playerStats, target.RuntimeState);
                    inst.buffTargets.Add(target);
                }
            }

            var buffRarityModule = inst.Data.moduleEffect as BuffRarityModule;
            if (buffRarityModule != null)
            {
                var buffCells = inst.GetAbsoluteBuffCells();
                var buffed = GetUniqueModulesAtCells(buffCells, inst);
                Rarity newRarity = (Rarity)buffRarityModule.GetBuffRarity(inst.RuntimeState);

                foreach (var target in buffed)
                {
                    if (target.Data.moduleEffect == null) continue;
                    target.Data.moduleEffect.OnRarityBuffReceived(target.Level, newRarity, playerStats, target.RuntimeState);
                    inst.buffTargets.Add(target);
                }
            }
        }

        foreach (var other in InventoryManager.Instance.WeaponGrid.GetAllModules())
        {
            if (other == inst) continue;
            if (!other.Data.isBuffAdjacent) continue;
            if (other.Data.moduleEffect == null) continue;

            var buffCells = other.GetAbsoluteBuffCells();
            if (!IsCoveredBy(inst, buffCells)) continue;

            var otherBuffModule = other.Data.moduleEffect as BuffPercentModule;
            if (otherBuffModule != null)
            {
                other.Data.moduleEffect.ApplyBuff(
                    inst.Data.moduleEffect, playerStats,
                    otherBuffModule.GetBuffPercent(other.RuntimeState),
                    inst.RuntimeState
                );
                other.buffTargets.Add(inst);
            }
            var otherBuffLevelModule = other.Data.moduleEffect as BuffLevelModule;
            if (otherBuffLevelModule != null)
            {
                int bonus = (int)otherBuffLevelModule.GetBuffLevel(other.RuntimeState);
                inst.Data.moduleEffect.OnLevelBuffReceived(inst.Level, bonus, inst.Rarity, playerStats, inst.RuntimeState);
                other.buffTargets.Add(inst);
            }

            var otherBuffRarityModule = other.Data.moduleEffect as BuffRarityModule;
            if (otherBuffRarityModule != null)
            {
                Rarity newRarity = (Rarity)otherBuffRarityModule.GetBuffRarity(other.RuntimeState);
                inst.Data.moduleEffect.OnRarityBuffReceived(inst.Level, newRarity, playerStats, inst.RuntimeState);
                other.buffTargets.Add(inst);
            }
        }
    }

    private void OnModuleUnequipped(ModuleInstance inst)
    {
        if (inst.Data.moduleEffect != null)
            inst.Data.moduleEffect.Unequip(playerStats, inst.Rarity, inst.Level, inst.RuntimeState);

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
                        buffModule.GetBuffPercent(inst.RuntimeState),
                        target.RuntimeState
                    );
                }
                inst.buffTargets.Clear();
            }

            var buffLevelModule = inst.Data.moduleEffect as BuffLevelModule;
            if (buffLevelModule != null)
            {
                foreach (var target in inst.buffTargets)
                {
                    if (target.Data.moduleEffect == null) continue;
                    int bonus = (int)buffLevelModule.GetBuffLevel(inst.RuntimeState);
                    target.Data.moduleEffect.OnLevelBuffRemoved(target.Level, bonus, target.Rarity, playerStats, target.RuntimeState);
                }
                inst.buffTargets.Clear();
            }

            var buffRarityModule = inst.Data.moduleEffect as BuffRarityModule;
            if (buffRarityModule != null)
            {
                foreach (var target in inst.buffTargets)
                {
                    if (target.Data.moduleEffect == null) continue;
                    target.Data.moduleEffect.OnRarityBuffRemoved(target.Level, target.Rarity, playerStats, target.RuntimeState);
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
            if (otherBuffModule != null)
            {
                other.Data.moduleEffect.RemoveBuff(
                    inst.Data.moduleEffect, playerStats,
                    otherBuffModule.GetBuffPercent(other.RuntimeState),
                    inst.RuntimeState
                );
                other.buffTargets.Remove(inst);
            }

            var otherBuffLevelModule = other.Data.moduleEffect as BuffLevelModule;
            if (otherBuffLevelModule != null)
            {
                int bonus = (int)otherBuffLevelModule.GetBuffLevel(other.RuntimeState);
                inst.Data.moduleEffect.OnLevelBuffRemoved(inst.Level, bonus, inst.Rarity, playerStats, inst.RuntimeState);
                other.buffTargets.Remove(inst);
            }

            var otherBuffRarityModule = other.Data.moduleEffect as BuffRarityModule;
            if (otherBuffRarityModule != null)
            {
                inst.Data.moduleEffect.OnRarityBuffRemoved(inst.Level, inst.Rarity, playerStats, inst.RuntimeState);
                other.buffTargets.Remove(inst);
            }
        }
    }

    private void Update()
    {
        var mgr = InventoryManager.Instance;
        if (mgr == null) return;

        foreach (var inst in mgr.WeaponGrid.GetAllModules())
        {
            if (inst.Data.moduleEffect == null) continue;
            inst.Data.moduleEffect.OnUpdate(playerStats, inst.Rarity, inst.Level, inst.RuntimeState);
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
            if (buffCells.Contains(cell)) return true;
        return false;
    }
}