using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitioner : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FadeIn() => Fade(1);

    public void TransitionToSceneWithCleanup(int index, System.Action onBlack)
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(PerformTransitionWithCleanup(index, onBlack));
    }

    private IEnumerator PerformTransitionWithCleanup(int index, System.Action onBlack)
    {
        yield return StartCoroutine(Fade(1));
        onBlack?.Invoke();
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);
        while (!operation.isDone) yield return null;
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(Fade(0));
        Destroy(gameObject);
    }

    public void TransitionToScene(int index)
    {
        DontDestroyOnLoad(gameObject);
        StartCoroutine(PerformTransition(index));
    }

    private IEnumerator PerformTransition(int index)
    {
        // 1. Fade In (To Black)
        yield return StartCoroutine(Fade(1));

        // 2. Load the next scene
        AsyncOperation operation = SceneManager.LoadSceneAsync(index);

        // Wait until the scene is fully loaded
        while (!operation.isDone)
        {
            yield return null;
        }

        // 3. Wait 1 second as requested
        yield return new WaitForSeconds(1f);

        // 4. Fade Out (To Transparent)
        yield return StartCoroutine(Fade(0));

        // 5. Destroy the transitioner
        Destroy(gameObject);
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }
}