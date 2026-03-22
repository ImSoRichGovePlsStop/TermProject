using UnityEngine;

[CreateAssetMenu(fileName = "PassiveNode", menuName = "Passive/PassiveNode")]
public class PassiveNode : GenericTreeNode
{
    [Header("Legacy Tree Position")]
    public int layer;
    public int branch;

    private void OnValidate()
    {
        cost = GetCostByLayer(layer);
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
