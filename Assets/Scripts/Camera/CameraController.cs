using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Vector3 offset = new Vector3(0, 3, -3);

    [Header("Endgame Effect")]
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private float shakeSpeed     = 20f;
    [SerializeField] private float zoomInAmount   = 1.5f;
    [SerializeField] private float endTimeScale   = 0.25f;

    private Transform _target;
    private bool      _foundPlayer;
    private Vector3   _currentOffset;
    private Vector3   _shakeOffset;
    private float     _shakeTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance       = this;
        _currentOffset = offset;
    }

    void LateUpdate()
    {
        if (_target == null) return;
        transform.position = _target.position + _currentOffset + _shakeOffset;
    }

    void Update()
    {
        if (_foundPlayer) return;
        var player = GameObject.FindWithTag("Player");
        if (player != null) { _target = player.transform; _foundPlayer = true; }
    }

    /// <summary>
    /// Shakes + zooms in + slows time for <paramref name="duration"/> seconds (unscaled).
    /// Time scale and zoom persist until RestoreCamera() is called.
    /// </summary>
    public IEnumerator EndgameEffect(float duration)
    {
        Time.timeScale      = endTimeScale;
        Time.fixedDeltaTime = 0.02f * endTimeScale;

        Vector3 startOffset  = _currentOffset;
        float   targetLen    = Mathf.Max(0.5f, startOffset.magnitude - zoomInAmount);
        Vector3 targetOffset = startOffset.normalized * targetLen;

        float elapsed    = 0f;
        float shakeInterval = shakeSpeed > 0f ? 1f / shakeSpeed : 0f;
        _shakeTimer = 0f;

        while (elapsed < duration)
        {
            float dt        = Time.unscaledDeltaTime;
            _shakeTimer    += dt;
            if (_shakeTimer >= shakeInterval)
            {
                _shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                _shakeTimer  = 0f;
            }
            _currentOffset = Vector3.Lerp(startOffset, targetOffset, elapsed / duration);
            elapsed        += dt;
            yield return null;
        }

        _currentOffset = targetOffset;
        _shakeOffset   = Vector3.zero;
    }

    /// <summary>Restores time scale and camera offset to defaults. Call on run reset.</summary>
    public void RestoreCamera()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        _currentOffset      = offset;
    }
}
