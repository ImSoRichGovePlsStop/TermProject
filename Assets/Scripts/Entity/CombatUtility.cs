using System.Collections.Generic;
using UnityEngine;

public static class CombatUtility
{
    public static List<T> FindAround<T>(Vector3 position, float radius, LayerMask? layerMask = null) where T : Component
    {
        Collider[] hits = layerMask.HasValue
            ? Physics.OverlapSphere(position, radius, layerMask.Value)
            : Physics.OverlapSphere(position, radius);

        var result = new List<T>();

        foreach (var hit in hits)
        {
            T component = hit.GetComponentInParent<T>();
            if (component == null) continue;
            if (result.Contains(component)) continue;

            if (component is HealthBase hb && hb.IsDead) continue;

            result.Add(component);
        }

        return result;
    }

    public static T FindNearest<T>(Vector3 position, float radius, LayerMask? layerMask = null) where T : Component
    {
        List<T> all = FindAround<T>(position, radius, layerMask);

        T nearest = null;
        float closestDist = Mathf.Infinity;

        foreach (var item in all)
        {
            float dist = Vector3.Distance(position, item.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                nearest = item;
            }
        }

        return nearest;
    }
}