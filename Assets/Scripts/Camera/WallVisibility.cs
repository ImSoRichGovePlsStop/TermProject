using System.Collections.Generic;
using UnityEngine;

// Attach to the Player or Camera.
// Box-casts from the camera to the player each frame.
// Any wall inside the box fades out smoothly; walls outside fade back in.
public class WallVisibility : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Transform player;

    [Header("Settings")]
    public LayerMask wallLayer;
    public string wallLayerName = "Wall";

    [Tooltip("Half-size of the box cast on X and Z. Increase to catch more walls to the sides.")]
    public float castHalfWidth = 1.5f;

    [Tooltip("How quickly walls fade out and back in (higher = faster).")]
    public float fadeSpeed = 8f;

    [Tooltip("Target alpha when hidden.")]
    [Range(0f, 1f)] public float hiddenAlpha = 0f;

    [Tooltip("Target alpha when visible.")]
    [Range(0f, 1f)] public float visibleAlpha = 1f;

    // Tracks every wall renderer we are currently managing and its target alpha
    private Dictionary<Renderer, float> _wallTargets = new Dictionary<Renderer, float>();
    // Tracks current alpha per renderer
    private Dictionary<Renderer, float> _wallCurrent = new Dictionary<Renderer, float>();

    void Awake()
    {
        if (cam == null) cam = Camera.main;
        if (player == null) player = transform;
        if (wallLayer.value == 0)
            wallLayer = LayerMask.GetMask(wallLayerName);
    }

    void LateUpdate()
    {
        if (cam == null || player == null) return;

        // Mark all tracked walls as visible (target = visibleAlpha)
        // We'll override the ones hit by the cast below
        var keys = new List<Renderer>(_wallTargets.Keys);
        foreach (var r in keys)
            _wallTargets[r] = visibleAlpha;

        // Box cast from camera to player
        Vector3 origin = cam.transform.position;
        Vector3 target = player.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        // Box half-extents: wide on X and Z, thin on Y
        Vector3 halfExtents = new Vector3(castHalfWidth, 0.1f, castHalfWidth);

        RaycastHit[] hits = Physics.BoxCastAll(
            origin,
            halfExtents,
            direction.normalized,
            Quaternion.LookRotation(direction.normalized),
            distance,
            wallLayer);

        foreach (var hit in hits)
        {
            var r = hit.collider.GetComponent<Renderer>()
                 ?? hit.collider.GetComponentInChildren<Renderer>();
            if (r == null) continue;

            // Register if new
            if (!_wallTargets.ContainsKey(r))
            {
                _wallTargets[r] = visibleAlpha;
                _wallCurrent[r] = visibleAlpha;
            }

            _wallTargets[r] = hiddenAlpha;
        }

        // Smooth all tracked renderers toward their target alpha
        var toRemove = new List<Renderer>();
        foreach (var r in new List<Renderer>(_wallTargets.Keys))
        {
            if (r == null) { toRemove.Add(r); continue; }

            float current = _wallCurrent[r];
            float target2 = _wallTargets[r];
            float next = Mathf.MoveTowards(current, target2, fadeSpeed * Time.deltaTime);
            _wallCurrent[r] = next;
            SetAlpha(r, next);

            // Stop tracking once fully visible again — avoids memory growing unbounded
            if (Mathf.Approximately(next, visibleAlpha) && Mathf.Approximately(target2, visibleAlpha))
                toRemove.Add(r);
        }

        foreach (var r in toRemove)
        {
            _wallTargets.Remove(r);
            _wallCurrent.Remove(r);
        }
    }

    void SetAlpha(Renderer r, float alpha)
    {
        // Works with URP Lit (Transparent) and URP Unlit
        foreach (var mat in r.materials)
        {
            Color c = mat.color;
            c.a = alpha;
            mat.color = c;
        }
    }

    void OnDisable()
    {
        // Restore all walls to full visibility when script is disabled
        foreach (var kvp in _wallCurrent)
            if (kvp.Key != null) SetAlpha(kvp.Key, visibleAlpha);
        _wallTargets.Clear();
        _wallCurrent.Clear();
    }
}