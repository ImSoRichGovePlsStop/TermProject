using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorTransitionUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private TextMeshProUGUI floorLabel;

    [Header("Timing")]
    [SerializeField] private float minDuration     = 3f;
    [SerializeField] private float fadeInDuration  = 0.8f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    void Awake() => gameObject.SetActive(false);

    void OnEnable()  { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => gameObject.SetActive(false);

    public IEnumerator PlayTransition() => PlayTransition(BuildFloorLabel());

    public IEnumerator PlayTransition(string label)
    {
        floorLabel.text  = label;
        floorLabel.alpha = 0f;
        gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed         += Time.unscaledDeltaTime;
            floorLabel.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        floorLabel.alpha = 1f;

        float holdTime = Mathf.Max(0f, minDuration - fadeInDuration - fadeOutDuration);
        yield return new WaitForSecondsRealtime(holdTime);

        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed         += Time.unscaledDeltaTime;
            floorLabel.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }
        floorLabel.alpha = 0f;
    }

    public static string BuildFloorLabel()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool  = EnemyPoolManager.Instance;
        int fps   = pool != null ? pool.floorsPerSegment : 3;
        int seg        = (floor - 1) / fps + 1;
        int floorInSeg = (floor - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}
