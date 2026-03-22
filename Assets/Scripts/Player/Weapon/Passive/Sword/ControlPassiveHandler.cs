using UnityEngine;

public class ControlPassiveHandler : PassiveHandlerBase
{
    [SerializeField] private ShatterFieldZone fieldPrefab;
    [SerializeField] private float baseRadius = 1f;

    private ShatterFieldPassive shatterField;

    public override void Init(GenericTreeData tree, WeaponPassiveData data,
                               WeaponPassiveManager manager,
                               PlayerStats stats, PlayerCombatContext context)
    {
        shatterField = gameObject.AddComponent<ShatterFieldPassive>();
        shatterField.fieldPrefab = fieldPrefab;

        var weaponEquip = stats.GetComponent<WeaponEquip>();
        shatterField.Init(stats, context, weaponEquip?.GetCurrentWeapon());

        base.Init(tree, data, manager, stats, context);
    }

    public override void Apply()
    {
        if (shatterField == null) return;

        shatterField.enabled = IsUnlocked(1, 0);
        shatterField.deepChill = IsUnlocked(2, 0);
        shatterField.intensifiedField = IsUnlocked(3, 1);
        shatterField.wideField = IsUnlocked(3, 2);
        shatterField.shatterPoint = IsUnlocked(4, 0);
        shatterField.brittle = IsUnlocked(5, 1);
        shatterField.exploit = IsUnlocked(5, 2);
        shatterField.shatterStrike = IsUnlocked(6, 0);
    }
}