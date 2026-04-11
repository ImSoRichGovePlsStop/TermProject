using System.Collections.Generic;
using UnityEngine;

public class WallVisibility : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform player;

    [Header("Materials")]
    public Material opaqueMat;
    public Material transparentMat;

    [Header("Settings")]
    public LayerMask wallLayer;
    public string wallLayerName = "Wall";

    [Tooltip("How far behind the player (-Z) to raycast for obstruction detection.")]
    public float detectionRange = 6f;

    [Tooltip("How far left/right to raycast to find room X boundaries.")]
    public float sideRayRange = 30f;

    [Tooltip("Fade speed.")]
    public float fadeSpeed = 8f;

    Dictionary<MeshRenderer, float> _current = new();
    Dictionary<MeshRenderer, float> _target = new();
    Dictionary<MeshRenderer, Material> _original = new();

    // All wall renderers cached at start
    List<MeshRenderer> _allWalls = new();
    bool _indexed = false;

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (player == null) player = transform;
        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask(wallLayerName);
    }

    public void IndexWalls()
    {
        _allWalls.Clear();
        foreach (var r in FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            if (((1 << r.gameObject.layer) & wallLayer.value) != 0)
                _allWalls.Add(r);
        _indexed = true;
    }

    void LateUpdate()
    {
        if (!_indexed) IndexWalls();
        if (cam == null || player == null) return;

        // Default all tracked to opaque
        foreach (var r in new List<MeshRenderer>(_target.Keys))
            if (r != null) _target[r] = 1f;

        Vector3 origin = player.position + Vector3.up * 0.5f;

        // Phase 1: check if player is actually obstructed from behind
        if (!Physics.Raycast(origin, Vector3.back, out RaycastHit backHit, detectionRange, wallLayer))
            goto Animate;

        // Phase 2: side raycasts to find room X boundaries
        float xMin = player.position.x - sideRayRange;
        float xMax = player.position.x + sideRayRange;

        if (Physics.Raycast(origin, Vector3.left, out RaycastHit leftHit, sideRayRange, wallLayer))
            xMin = leftHit.point.x;
        if (Physics.Raycast(origin, Vector3.right, out RaycastHit rightHit, sideRayRange, wallLayer))
            xMax = rightHit.point.x;

        // Phase 3: fade walls within X range and between player and the hit wall (Z range)
        float playerZ = player.position.z;
        float hitZ = backHit.point.z;  // Z of the detected wall — nothing beyond this fades
        foreach (var r in _allWalls)
        {
            if (r == null) continue;
            float wx = r.transform.position.x;
            float wz = r.transform.position.z;
            if (wx < xMin || wx > xMax) continue;
            if (wz > playerZ + 0.5f) continue;  // not behind player
            if (wz < hitZ - 0.5f) continue;  // beyond the detected wall
            if (!_target.ContainsKey(r)) { _target[r] = 1f; _current[r] = 1f; _original[r] = r.sharedMaterial; }
            _target[r] = 0f;
        }

    Animate:
        // Animate all tracked renderers toward their target alpha
        var toRemove = new List<MeshRenderer>();
        foreach (var r in new List<MeshRenderer>(_target.Keys))
        {
            if (r == null) { toRemove.Add(r); continue; }

            float next = Mathf.MoveTowards(_current[r], _target[r], fadeSpeed * Time.deltaTime);
            _current[r] = next;

            if (next < 1f)
            {
                if (r.sharedMaterial != transparentMat) r.material = transparentMat;
                var col = r.material.color;
                col.a = next;
                r.material.color = col;
            }
            else
            {
                var orig = _original.TryGetValue(r, out var o) ? o : opaqueMat;
                if (r.sharedMaterial != orig) r.material = orig;
            }

            if (Mathf.Approximately(next, 1f) && Mathf.Approximately(_target[r], 1f))
                toRemove.Add(r);
        }

        foreach (var r in toRemove)
        {
            if (r != null) { var orig = _original.TryGetValue(r, out var o) ? o : opaqueMat; r.material = orig; }
            _target.Remove(r); _current.Remove(r); _original.Remove(r);
        }
    }

    void OnDisable()
    {
        foreach (var kvp in _current)
            if (kvp.Key != null) { var orig = _original.TryGetValue(kvp.Key, out var o) ? o : opaqueMat; kvp.Key.material = orig; }
        _target.Clear(); _current.Clear(); _original.Clear();
    }
}