using UnityEngine;

[CreateAssetMenu(fileName = "PassiveNode", menuName = "Passive/PassiveNode")]
public class PassiveNode : ScriptableObject
{
    [Header("Info")]
    public string nodeName;
    [TextArea] public string description;

    [Header("Tree Position")]
    public int layer;     // 1-6
    public int branch;    // 0 = center, 1 = left, 2 = right

    public int Cost
    {
        get { return GetCostByLayer(layer); }
    }

    public static int GetCostByLayer(int layer)
    {
        switch (layer)
        {
            case 1: return 1;
            case 2: return 1;
            case 3: return 2;
            case 4: return 3;
            case 5: return 3;
            case 6: return 4;
            default: return 0;
        }
    }
}