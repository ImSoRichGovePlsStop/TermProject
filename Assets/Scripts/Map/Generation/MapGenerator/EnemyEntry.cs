using UnityEngine;

public enum EnemyType { Melee, Ranged }

[System.Serializable]
public class EnemyEntry
{
    public GameObject normal;
    [Tooltip("Leave empty if this enemy has no elite variant.")]
    public GameObject elite;
    public EnemyType  type;
    [Tooltip("Spawn budget cost. Basic enemy = 10, strongest = 20.")]
    public int cost = 10;
}
