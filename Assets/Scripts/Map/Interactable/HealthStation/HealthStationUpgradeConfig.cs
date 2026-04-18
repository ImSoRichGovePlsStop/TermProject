using UnityEngine;

[System.Serializable]
public class HealthStationLevelData
{
    [TextArea(1, 2)]
    public string description;
    public MaterialRequirement[] upgradeCost;
}

[CreateAssetMenu(fileName = "HealthStationUpgradeConfig", menuName = "Config/HealthStationUpgradeConfig")]
public class HealthStationUpgradeConfig : ScriptableObject
{
    public HealthStationLevelData[] levels;
}
