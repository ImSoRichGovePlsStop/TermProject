using UnityEngine;

[CreateAssetMenu(fileName = "Material_New", menuName = "Inventory/Material")]
public class MaterialData : ModuleData
{
    [Header("Material")]
    public Rarity rarity = Rarity.Common;
    public int maxStack = 3;
}
