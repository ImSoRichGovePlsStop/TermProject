using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


public class FloorTransitionUI : MonoBehaviour
{
    [Header("UI Refs")]
    [SerializeField] private GameObject         panel;
    [SerializeField] private TextMeshProUGUI    floorLabel;

    [Header("Timing")]
    [Tooltip("Minimum seconds the transition screen is visible before the scene loads.")]
    [SerializeField] private float minDuration  = 3f;
    [Tooltip("Seconds the text takes to fade in.")]
    [SerializeField] private float fadeInDuration  = 0.8f;
    [Tooltip("Seconds the text takes to fade out before the scene loads.")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    void Awake()
    {
        panel.SetActive(false);
    }


    public IEnumerator TransitionRoutine(int sceneIndex)
        => TransitionRoutine(BuildFloorLabel(), sceneIndex);

    public IEnumerator TransitionRoutine(string label, int sceneIndex)
    {
        floorLabel.text  = label;
        floorLabel.alpha = 0f;
        panel.SetActive(true);


        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed          += Time.unscaledDeltaTime;
            floorLabel.alpha  = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }
        floorLabel.alpha = 1f;


        float holdTime = Mathf.Max(0f, minDuration - fadeInDuration - fadeOutDuration);
        yield return new WaitForSecondsRealtime(holdTime);


        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed          += Time.unscaledDeltaTime;
            floorLabel.alpha  = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }
        floorLabel.alpha = 0f;

        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(sceneIndex);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        panel.SetActive(false);
    }


    public static string BuildFloorLabel()
    {
        int floor = RunManager.Instance?.CurrentFloor ?? 1;
        var pool  = EnemyPoolManager.Instance;
        int fps   = pool != null ? pool.floorsPerSegment : 3;
        int seg         = (floor - 1) / fps + 1;
        int floorInSeg  = (floor - 1) % fps + 1;
        return $"{seg}-{floorInSeg}";
    }
}
