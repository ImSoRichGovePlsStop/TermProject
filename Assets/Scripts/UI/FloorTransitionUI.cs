using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorTransitionUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI floorLabel;

    [Header("Timing")]
    [SerializeField] private float panelFadeInDuration = 0.3f;
    [SerializeField] private float labelFadeInDuration = 0.6f;
    [SerializeField] private float holdAfterMapReady   = 2f;
    [SerializeField] private float fadeOutDuration     = 0.5f;

    private CanvasGroup _cg;
    private bool _fadingOut = false;

    void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
        _cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!gameObject.activeSelf) return;
        _fadingOut = false;

        var geo = Object.FindFirstObjectByType<BSPMapGeometry>();
        if (geo != null)
            geo.OnMapReady += OnMapReady;
        else
            StartCoroutine(PlayFadeOut());
    }

    void OnMapReady(IReadOnlyList<MapNode> _)
    {
        if (!_fadingOut)
            StartCoroutine(PlayFadeOutDelayed());
    }

    private IEnumerator PlayFadeOutDelayed()
    {
        yield return new WaitForSecondsRealtime(holdAfterMapReady);
        StartCoroutine(PlayFadeOut());
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public IEnumerator PlayFadeIn()
    {
        floorLabel.text  = BuildFloorLabel();
        floorLabel.alpha = 0f;
        _cg.alpha        = 0f;
        _fadingOut       = false;
        gameObject.SetActive(true);

        // Phase 1: panel fades in (fast)
        float elapsed = 0f;
        while (elapsed < panelFadeInDuration)
        {
            elapsed   += Time.unscaledDeltaTime;
            _cg.alpha  = Mathf.Clamp01(elapsed / panelFadeInDuration);
            yield return null;
        }
        _cg.alpha = 1f;

        // Phase 2: label fades in after panel is fully visible
        elapsed = 0f;
        while (elapsed < labelFadeInDuration)
        {
            elapsed          += Time.unscaledDeltaTime;
            floorLabel.alpha  = Mathf.Clamp01(elapsed / labelFadeInDuration);
            yield return null;
        }
        floorLabel.alpha = 1f;
        // Stay fully visible; PlayFadeOut triggered by OnMapReady.
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private IEnumerator PlayFadeOut()
    {
        _fadingOut = true;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed   += Time.unscaledDeltaTime;
            _cg.alpha  = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }
        _cg.alpha = 0f;
        gameObject.SetActive(false);
    }

    public static string BuildFloorLabel()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool  = EnemyPoolManager.Instance;
        int fps        = pool != null ? pool.floorsPerSegment : 3;
        int seg        = (floor - 1) / fps + 1;
        int floorInSeg = (floor - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}
