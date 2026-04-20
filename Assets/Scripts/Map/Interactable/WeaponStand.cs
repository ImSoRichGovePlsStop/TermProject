using UnityEngine;

public class WeaponStand : MonoBehaviour, IInteractable
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Float Animation")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.15f;

    private PassiveScreenUI passiveScreenUI;
    private WeaponEquip weaponEquip;
    private UIManager uiManager;

    private float baseY;

    public string GetPromptText()
    {
        if (weaponEquip == null)
            weaponEquip = FindFirstObjectByType<WeaponEquip>();

        if (weaponEquip != null && weaponEquip.GetCurrentWeapon() == weaponData)
            return "[ E ]  Open Skill Tree";
        return "[ E ]  Pick Up Weapon";
    }

    public InteractInfo GetInteractInfo()
    {
        if (weaponEquip == null)
            weaponEquip = FindFirstObjectByType<WeaponEquip>();

        bool isEquipped = weaponEquip != null && weaponEquip.GetCurrentWeapon() == weaponData;
        return new InteractInfo
        {
            name       = weaponData != null ? weaponData.weaponName : "Weapon",
            actionText = isEquipped ? "Open Skill Tree" : "Pick Up",
            cost       = null
        };
    }

    private void Awake()
    {
        passiveScreenUI = FindFirstObjectByType<PassiveScreenUI>();
        uiManager = FindFirstObjectByType<UIManager>();

        if (spriteRenderer != null && weaponData != null)
            spriteRenderer.sprite = weaponData.icon;
    }

    private void Start()
    {
        baseY = transform.position.y;
    }

    private void Update()
    {
        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = pos;
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