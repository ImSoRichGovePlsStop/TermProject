using UnityEngine;

[CreateAssetMenu(fileName = "GenericTreeData", menuName = "Tree/GenericTreeData")]
public class GenericTreeData : ScriptableObject
{
    public string treeName;
    public Color treeColor = Color.white;
    public GenericTreeNode[] nodes;
    public GenericTreeHandlerBase handlerPrefab;
}
