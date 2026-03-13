using System.Collections.Generic;
using UnityEngine;

public class WeaponPassiveManager : MonoBehaviour
{
    private Dictionary<WeaponPassiveData, PassiveTreeState> states
        = new Dictionary<WeaponPassiveData, PassiveTreeState>();

    private int startingPoints = 12;

    private void Start()
    {
        states.Clear();
    }

    public PassiveTreeState GetState(WeaponPassiveData data)
    {
        if (!states.ContainsKey(data))
        {
            var newState = new PassiveTreeState();
            newState.availablePoints = startingPoints;
            states[data] = newState;
        }
        return states[data];
    }

    public bool TryUnlock(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        return GetState(data).TryUnlock(node, tree);
    }

    public bool TryRefund(PassiveNode node, PassiveTree tree, WeaponPassiveData data)
    {
        return GetState(data).TryRefund(node, tree);
    }

    public void ResetPassive(WeaponPassiveData data)
    {
        GetState(data).Reset(startingPoints);
    }
}