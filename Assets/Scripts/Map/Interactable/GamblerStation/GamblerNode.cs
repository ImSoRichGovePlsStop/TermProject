using UnityEngine;

[CreateAssetMenu(fileName = "GamblerNode", menuName = "Gambler/GamblerNode")]
public class GamblerNode : GenericTreeNode
{
    [Header("Tree Position")]
    public int layer;
    public int branch;
}