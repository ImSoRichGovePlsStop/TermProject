using UnityEngine;

[CreateAssetMenu(fileName = "PassiveTree", menuName = "Passive/PassiveTree")]
public class PassiveTree : ScriptableObject
{
    public string treeName;
    public PassiveHandlerBase handlerPrefab;
    public PassiveNode[] nodes;
    public Color treeColor = Color.white;
}