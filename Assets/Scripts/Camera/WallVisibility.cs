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
    public float detectionRange = 2f;

    [Tooltip("How far left/right to raycast to find room X boundaries.")]
    public float sideRayRange = 30f;

    [Tooltip("Fade speed.")]
    public float fadeSpeed = 8f;

    // Per-renderer state
    Dictionary<MeshRenderer, float> _current = new();
    Dictionary<MeshRenderer, float> _target = new();
    Dictionary<MeshRenderer, Material> _original = new();

    // Cached material instances per renderer — created once, reused
    Dictionary<MeshRenderer, Material> _transparentInstance = new();

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

        Vector3 origin = player.position + Vector3.up * 0.5f;
        float playerZ = player.position.z;
        float zMin = playerZ - detectionRange ;  // detection range + 1 metre behind

        bool obstructed = Physics.Raycast(origin, Vector3.back,
                          out RaycastHit _, detectionRange, wallLayer);

        float xMin = player.position.x - sideRayRange;
        float xMax = player.position.x + sideRayRange;

        if (obstructed)
        {
            if (Physics.Raycast(origin, Vector3.left, out RaycastHit lh, sideRayRange, wallLayer))
                xMin = lh.point.x;
            if (Physics.Raycast(origin, Vector3.right, out RaycastHit rh, sideRayRange, wallLayer))
                xMax = rh.point.x;
        }

        // Mark target alpha for every tracked and newly hit wall
        var wantHidden = new HashSet<MeshRenderer>();

        if (obstructed)
        {
            foreach (var r in _allWalls)
            {
                if (r == null) continue;
                float wx = r.transform.position.x;
                float wz = r.transform.position.z;
                if (wx < xMin || wx > xMax) continue;
                if (wz > playerZ + 0.5f) continue;  // in front of player
                if (wz < zMin) continue;  // beyond detection range + 1m
                wantHidden.Add(r);
            }
        }

        // Register new renderers and set targets
        foreach (var r in wantHidden)
        {
            if (!_target.ContainsKey(r))
            {
                _current[r] = 1f;
                _target[r] = 1f;
                _original[r] = r.sharedMaterial;
                // Create one transparent instance per renderer — reused every frame
                var inst = new Material(transparentMat);
                inst.color = new Color(inst.color.r, inst.color.g, inst.color.b, 1f);
                _transparentInstance[r] = inst;
            }
            _target[r] = 0f;
        }

        // Restore targets for walls no longer in the hidden set
        foreach (var r in new List<MeshRenderer>(_target.Keys))
            if (r != null && !wantHidden.Contains(r))
                _target[r] = 1f;

        // Animate all tracked renderers
        var toRemove = new List<MeshRenderer>();
        foreach (var r in new List<MeshRenderer>(_target.Keys))
        {
            if (r == null) { toRemove.Add(r); continue; }

            float next = Mathf.MoveTowards(_current[r], _target[r], fadeSpeed * Time.deltaTime);
            _current[r] = next;

            if (next < 1f)
            {
                // Use the cached instance — no allocation each frame
                if (_transparentInstance.TryGetValue(r, out var inst))
                {
                    var col = inst.color;
                    col.a = next;
                    inst.color = col;
                    r.material = inst;
                }
            }
            else
            {
                // Restore original material
                var orig = _original.TryGetValue(r, out var o) ? o : opaqueMat;
                r.material = orig;
            }

            // Stop tracking once fully opaque and stable
            if (Mathf.Approximately(next, 1f) && Mathf.Approximately(_target[r], 1f))
                toRemove.Add(r);
        }

        foreach (var r in toRemove)
        {
            if (r != null)
            {
                var orig = _original.TryGetValue(r, out var o) ? o : opaqueMat;
                r.material = orig;
                if (_transparentInstance.TryGetValue(r, out var inst))
                    Destroy(inst);
            }
            _target.Remove(r);
            _current.Remove(r);
            _original.Remove(r);
            _transparentInstance.Remove(r);
        }
    }

    void OnDisable()
    {
        foreach (var kvp in _current)
        {
            if (kvp.Key == null) continue;
            var orig = _original.TryGetValue(kvp.Key, out var o) ? o : opaqueMat;
            kvp.Key.material = orig;
            if (_transparentInstance.TryGetValue(kvp.Key, out var inst))
                Destroy(inst);
        }
        _target.Clear();
        _current.Clear();
        _original.Clear();
        _transparentInstance.Clear();
    }
}