using UnityEngine;

public enum EnemyType { Melee, Ranged }

[System.Serializable]
public class EnemyEntry
{
    public GameObject normal;
    [Tooltip("Leave empty if this enemy has no elite variant.")]
    public GameObject elite;
    public EnemyType  type;
}
