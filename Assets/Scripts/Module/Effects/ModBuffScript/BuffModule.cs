using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Module Effect/LevelUpBuff")]
public class LevelUpBuffEffect : ModuleEffect
{
    [Header("Buff Settings")]
    public int levelBoost = 1;

    [Header("Aura")]
    public bool canReceiveAura = false;

    [System.NonSerialized] private GridData _grid;
    [System.NonSerialized] private ModuleInstance _self;
    [System.NonSerialized] private List<ModuleEffect> _boostedEffects;

    private void OnEnable()
    {
        _grid = null;
        _self = null;
        _boostedEffects = new List<ModuleEffect>();
        Debug.Log($"[BuffMod] {name}: OnEnable fired — context cleared");
    }

    protected override void OnEquip(PlayerStats stats)
    {
        Debug.Log($"[BuffMod] {name}: OnEquip called");
    }

    protected override void OnUnequip(PlayerStats stats)
    {
        Debug.Log($"[BuffMod] {name}: OnUnequip called");
        RemoveBoost();
        _grid = null;
        _self = null;
    }

    public void Refresh(GridData grid, ModuleInstance self)
    {
        Debug.Log($"[BuffMod] {name}: Refresh called — grid={grid != null}, self={self?.Data?.moduleName}");
        _grid = grid;
        _self = self;
        RemoveBoost();
        ApplyBoost();
    }

    private void ApplyBoost()
    {
        Debug.Log($"[BuffMod] {name}: ApplyBoost called");
        if (_boostedEffects == null) _boostedEffects = new List<ModuleEffect>();
        _boostedEffects.Clear();
        foreach (var effect in FindEffectsUnderAura())
        {
            for (int i = 0; i < levelBoost; i++)
                effect.OnLevelUp();
            _boostedEffects.Add(effect);
        }
    }

    private void RemoveBoost()
    {
        if (_boostedEffects == null) { _boostedEffects = new List<ModuleEffect>(); return; }
        foreach (var effect in _boostedEffects)
            for (int i = 0; i < levelBoost; i++)
                effect.OnLevelDown();
        _boostedEffects.Clear();
    }

    private List<ModuleEffect> FindEffectsUnderAura()
    {
        var result = new List<ModuleEffect>();

        if (_grid == null || _self == null)
        {
            Debug.Log($"[BuffMod] {name}: FindEffectsUnderAura — context null, aborting");
            return result;
        }

        var auraCells = _self.Data.GetAuraCells();
        Debug.Log($"[BuffMod] {name}: FindEffectsUnderAura — {auraCells.Count} aura cells, origin={_self.GridPosition}");

        var seen = new HashSet<ModuleEffect>();
        foreach (var localCell in auraCells)
        {
            Vector2Int worldCell = _self.GridPosition + localCell;
            var targetInst = _grid.GetModuleAt(worldCell);

            if (targetInst == null) { Debug.Log($"[BuffMod]   {worldCell} — empty"); continue; }
            if (targetInst == _self) continue;
            if (!targetInst.Data.canBeBuffed) { Debug.Log($"[BuffMod]   {worldCell} — {targetInst.Data.moduleName} canBeBuffed=false"); continue; }

            var effect = targetInst.Data.moduleEffect;
            if (effect == null) { Debug.Log($"[BuffMod]   {worldCell} — {targetInst.Data.moduleName} no effect"); continue; }

            if (seen.Add(effect))
            {
                Debug.Log($"[BuffMod]   {worldCell} — BUFFING {targetInst.Data.moduleName} ({effect.GetType().Name})");
                result.Add(effect);
            }
        }

        Debug.Log($"[BuffMod] {name}: total buffed = {result.Count}");
        return result;
    }
}