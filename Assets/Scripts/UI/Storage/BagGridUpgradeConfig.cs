using UnityEngine;

[CreateAssetMenu(fileName = "BagGridUpgradeConfig", menuName = "Config/BagGrid Upgrade Config")]
public class BagGridUpgradeConfig : ScriptableObject
{
    public BagGridUpgradeLevel[] levels = new BagGridUpgradeLevel[]
    {
        new BagGridUpgradeLevel { rows = 4, cols = 6  },  
        new BagGridUpgradeLevel { rows = 4, cols = 7  },   
        new BagGridUpgradeLevel { rows = 5, cols = 7  },  
        new BagGridUpgradeLevel { rows = 5, cols = 8  }, 
        new BagGridUpgradeLevel { rows = 5, cols = 9  },   
        new BagGridUpgradeLevel { rows = 6, cols = 10 },  
    };
}

[System.Serializable]
public struct BagGridUpgradeLevel
{
    public int cols;
    public int rows;
    public MaterialRequirement[] cost;
}
