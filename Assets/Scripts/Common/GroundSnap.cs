using System.Collections;
using UnityEngine;

public class GroundSnap : MonoBehaviour
{
    [SerializeField] private float groundOffset = 0.01f;
    [SerializeField] private float heightOffset = 0f;
    [SerializeField] private LayerMask groundLayer;

    private IEnumerator Start()
    {
        yield return null;

        Vector3 rayStart = transform.position;
        rayStart.y += 5f;

        Debug.Log($"[GroundSnap] Raycast from {rayStart} downward");

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f, groundLayer))
        {
            Vector3 pos = transform.position;
            pos.y = hit.point.y + groundOffset + heightOffset;
            transform.position = pos;
            Debug.Log($"[GroundSnap] Snapped to Y: {pos.y} (hit: {hit.point.y})");
        }
        else
        {
            Debug.LogWarning($"[GroundSnap] No ground found! groundLayer: {groundLayer.value}");
        }
    }
}