using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays attack name/description text based on the currently equipped weapon.
/// Attach anywhere in the HUD. Wire up the four text references and the weapon
/// entries in the Inspector.
/// </summary>
public class WeaponAttackTextUI : MonoBehaviour
{
    [Header("Text Slots")]
    [SerializeField] private TextMeshProUGUI primaryNameText;
    [SerializeField] private TextMeshProUGUI primaryDescText;
    [SerializeField] private TextMeshProUGUI secondaryNameText;
    [SerializeField] private TextMeshProUGUI secondaryDescText;

    [Header("Weapon Entries")]
    [SerializeField] private WeaponEntry[] weaponEntries;

    [Serializable]
    public class WeaponEntry
    {
        public WeaponData weapon;
        public string primaryName;
        [TextArea(2, 4)] public string primaryDesc;
        public string secondaryName;
        [TextArea(2, 4)] public string secondaryDesc;
    }

    private WeaponEquip weaponEquip;
    private WeaponData  lastWeapon;

    private void Start()
    {
        weaponEquip = FindFirstObjectByType<WeaponEquip>();
        Refresh();
    }

    private void Update()
    {
        var current = weaponEquip != null ? weaponEquip.GetCurrentWeapon() : null;
        if (current == lastWeapon) return;
        lastWeapon = current;
        Refresh();
    }

    private void Refresh()
    {
        var weapon = weaponEquip != null ? weaponEquip.GetCurrentWeapon() : null;

        if (weapon == null)
        {
            SetText(primaryNameText,   string.Empty);
            SetText(primaryDescText,   string.Empty);
            SetText(secondaryNameText, string.Empty);
            SetText(secondaryDescText, string.Empty);
            return;
        }

        WeaponEntry entry = FindEntry(weapon);
        if (entry == null)
        {
            SetText(primaryNameText,   string.Empty);
            SetText(primaryDescText,   string.Empty);
            SetText(secondaryNameText, string.Empty);
            SetText(secondaryDescText, string.Empty);
            return;
        }

        SetText(primaryNameText,   entry.primaryName);
        SetText(primaryDescText,   entry.primaryDesc);
        SetText(secondaryNameText, entry.secondaryName);
        SetText(secondaryDescText, entry.secondaryDesc);
    }

    private WeaponEntry FindEntry(WeaponData weapon)
    {
        if (weaponEntries == null) return null;
        foreach (var e in weaponEntries)
            if (e.weapon == weapon) return e;
        return null;
    }

    static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null) label.text = value ?? string.Empty;
    }
}
