using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [SerializeField] private Vector3 offset = new Vector3(0, 3, -3);

    [Header("Endgame Effect")]
    [SerializeField] private float shakeMagnitude = 0.15f;
    [SerializeField] private float shakeSpeed = 20f;
    [SerializeField] private float zoomInAmount = 1.5f;
    [SerializeField] private float endTimeScale = 0.25f;

    private Transform _target;
    private bool _foundPlayer;
    private Vector3 _currentOffset;
    private Vector3 _shakeOffset;
    private float _shakeTimer;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        Time.timeScale = endTimeScale;
        Time.fixedDeltaTime = 0.02f * endTimeScale;

        Vector3 startOffset = _currentOffset;
        float targetLen = Mathf.Max(0.5f, startOffset.magnitude - zoomInAmount);
        Vector3 targetOffset = startOffset.normalized * targetLen;

        float elapsed = 0f;
        float shakeInterval = shakeSpeed > 0f ? 1f / shakeSpeed : 0f;
        _shakeTimer = 0f;

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            _shakeTimer += dt;
            if (_shakeTimer >= shakeInterval)
            {
                _shakeOffset = Random.insideUnitSphere * shakeMagnitude;
                _shakeTimer = 0f;
            }
            _currentOffset = Vector3.Lerp(startOffset, targetOffset, elapsed / duration);
            elapsed += dt;
            yield return null;
        }

        _currentOffset = targetOffset;
        _shakeOffset = Vector3.zero;
    }

    /// <summary>Restores time scale and camera offset to defaults. Call on run reset.</summary>
    public void RestoreCamera()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        _currentOffset = offset;
    }

    public void LockToPosition(Vector3 worldPosition)
    {
        var go = new GameObject("CameraLockTarget");
        go.transform.position = worldPosition;
        _target = go.transform;
    }

    public IEnumerator LockToPositionSmooth(Vector3 worldPosition, float duration)
    {
        var go = new GameObject("CameraLockTarget");
        Vector3 startPos = _target != null ? _target.position : worldPosition;
        go.transform.position = startPos;
        _target = go.transform;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            go.transform.position = Vector3.Lerp(startPos, worldPosition, t);
            yield return null;
        }
        go.transform.position = worldPosition;
    }

    public void UnlockTarget()
    {
        if (_target != null && _target.name == "CameraLockTarget")
            Destroy(_target.gameObject);

        var player = GameObject.FindWithTag("Player");
        if (player != null) _target = player.transform;
    }

    public IEnumerator UnlockTargetSmooth(float duration)
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) yield break;

        if (_target != null && _target.name == "CameraLockTarget")
        {
            var lockGo = _target.gameObject;
            Vector3 startPos = lockGo.transform.position;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                lockGo.transform.position = Vector3.Lerp(startPos, player.transform.position, t);
                yield return null;
            }

            Destroy(lockGo);
        }

        _target = player.transform;
    }

    public IEnumerator ZoomOut(float zoomAmount, float duration)
    {
        Vector3 startOffset = _currentOffset;
        Vector3 targetOffset = startOffset.normalized * (startOffset.magnitude + zoomAmount);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _currentOffset = Vector3.Lerp(startOffset, targetOffset, t);
            yield return null;
        }
        _currentOffset = targetOffset;
    }

    public IEnumerator ZoomRestore(float duration)
    {
        Vector3 startOffset = _currentOffset;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _currentOffset = Vector3.Lerp(startOffset, offset, t);
            yield return null;
        }
        _currentOffset = offset;
    }
}