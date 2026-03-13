using UnityEngine;

[CreateAssetMenu(fileName = "WeaponPassiveData", menuName = "Passive/WeaponPassiveData")]
public class WeaponPassiveData : ScriptableObject
{
    public PassiveTree[] trees; // always 3 trees
}