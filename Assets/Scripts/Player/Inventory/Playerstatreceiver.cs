using UnityEngine;

public class PlayerStatReceiver : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;
    private StatModifier _currentModuleBonus = new StatModifier();

    public event System.Action OnStatsChanged;

    private void Start()
    {
        if (_playerStats == null)
            _playerStats = GetComponentInChildren<PlayerStats>();
        if (_playerStats == null)
            Debug.LogError("[PlayerStatReceiver] PlayerStats not found! Assign it in the Inspector.");

        var mgr = InventoryManager.Instance;
        if (mgr == null) { RecalculateStats(); return; }

        mgr.OnModuleEquipped   += _ => RecalculateStats();
        mgr.OnModuleUnequipped += _ => RecalculateStats();
        RecalculateStats();
    }

    public void RecalculateStats()
    {
        float hp = 0, atk = 0, aspd = 0, spd = 0, cc = 0, cd = 0;

        var mgr = InventoryManager.Instance;
        if (mgr != null)
        {
            foreach (var mod in mgr.WeaponGrid.GetAllModules())
            {
                var s = mod.Data.stat;
                if (s == null) continue;
                hp   += s.bonusMaxHp;
                atk  += s.bonusAttack;
                aspd += s.bonusAttackSpeed;
                spd  += s.bonusMoveSpeed;
                cc   += s.bonusCritChance;
                cd   += s.bonusCritDamage;
            }
        }

        if (_playerStats != null)
        {
            _playerStats.RemoveFlatModifier(_currentModuleBonus);
            _currentModuleBonus = new StatModifier
            {
                health      = hp,
                damage      = atk,
                attackSpeed = aspd,
                moveSpeed   = spd,
                critChance  = cc,
                critDamage  = cd,
            };
            _playerStats.AddFlatModifier(_currentModuleBonus);

            Debug.Log($"[Stats] HP={_playerStats.MaxHealth:F1}  ATK={_playerStats.Damage:F1}  ASPD={_playerStats.AttackSpeed:F2}  MOVE={_playerStats.MoveSpeed:F2}  CRIT={_playerStats.CritChance:P0}/{_playerStats.CritDamage:P0}");
        }

        OnStatsChanged?.Invoke();
    }
}
