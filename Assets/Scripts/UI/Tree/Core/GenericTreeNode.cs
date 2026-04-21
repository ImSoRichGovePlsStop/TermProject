using UnityEngine;

[CreateAssetMenu(fileName = "GenericTreeNode", menuName = "Tree/GenericTreeNode")]
public class GenericTreeNode : ScriptableObject
{
    [Header("Info")]
    public string nodeName;
    [TextArea] public string description;
    public int cost = 1;

    [Header("Parents")]
    public GenericTreeNode[] parents;
    public bool requireAllParents = false;
}
