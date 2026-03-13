using UnityEngine;

[CreateAssetMenu(fileName = "PassiveTree", menuName = "Passive/PassiveTree")]
public class PassiveTree : ScriptableObject
{
    public string treeName;
    public PassiveNode[] nodes; // 8 nodes per tree
    public Color treeColor = Color.white;
}