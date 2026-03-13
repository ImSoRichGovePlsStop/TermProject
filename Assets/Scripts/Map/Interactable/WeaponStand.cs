using UnityEngine;

public class WeaponStand : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private WeaponPassiveData passiveData;

    private PassiveScreenUI passiveScreenUI;
    private WeaponEquip weaponEquip;

    private void Awake()
    {
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>();
    }

    public void Interact(PlayerController playerController)
    {
        if (weaponEquip == null)
            weaponEquip = playerController.GetComponent<WeaponEquip>();

        if (weaponEquip.GetCurrentWeapon() == weaponData)
        {
            UIManager uiManager = FindFirstObjectByType<UIManager>();
            if (uiManager != null && uiManager.IsInventoryOpen)
                uiManager.ToggleInventory();

            passiveScreenUI.Open(passiveData);
        }
        else
        {
            weaponEquip.Equip(weaponData);
        }
    }
}