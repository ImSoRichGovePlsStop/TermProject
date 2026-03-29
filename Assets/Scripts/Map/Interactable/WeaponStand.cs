using UnityEngine;

public class WeaponStand : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponData weaponData;

    private PassiveScreenUI passiveScreenUI;
    private WeaponEquip weaponEquip;
    private UIManager uiManager;

    public string GetPromptText()
    {
        if (weaponEquip == null)
            weaponEquip = FindFirstObjectByType<WeaponEquip>();

        if (weaponEquip != null && weaponEquip.GetCurrentWeapon() == weaponData)
            return "[ E ]  Open Skill Tree";
        return "[ E ]  Pick Up Weapon";
    }

    private void Awake()
    {
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>();
        uiManager = FindFirstObjectByType<UIManager>();
    }

    public void Interact(PlayerController playerController)
    {
        if (weaponEquip == null)
            weaponEquip = playerController.GetComponent<WeaponEquip>();

        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);

        if (weaponEquip.GetCurrentWeapon() == weaponData)
        {
            uiManager.OpenPassive(weaponData.passiveData, weaponData);
        }
        else
        {
            weaponEquip.Equip(weaponData);
        }
    }
}