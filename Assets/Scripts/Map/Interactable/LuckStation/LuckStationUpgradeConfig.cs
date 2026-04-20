using UnityEngine;

[System.Serializable]
public class LuckStationLevelData
{
    [TextArea(1, 2)]
    public string description;
    public MaterialRequirement[] upgradeCost;
}

[CreateAssetMenu(fileName = "LuckStationUpgradeConfig", menuName = "Config/LuckStationUpgradeConfig")]
public class LuckStationUpgradeConfig : ScriptableObject
{
    public LuckStationLevelData[] levels;
}
