using System.Collections;
using UnityEngine;

public class GuardDomeVFX : MonoBehaviour
{
    [SerializeField] private float scaleInDuration = 0.1f;
    [SerializeField] private float scaleOutDuration = 0.1f;
    [SerializeField] private Vector3 targetScale = new Vector3(2f, 2f, 2f);

    private Coroutine scaleCoroutine;

    private void Awake()
    {
        transform.localScale = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(transform.localScale, targetScale, scaleInDuration));
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        if (scaleCoroutine != null) StopCoroutine(scaleCoroutine);
        scaleCoroutine = StartCoroutine(ScaleRoutine(transform.localScale, Vector3.zero, scaleOutDuration, true));
    }

    private IEnumerator ScaleRoutine(Vector3 from, Vector3 to, float duration, bool deactivateOnEnd = false)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        transform.localScale = to;
        scaleCoroutine = null;
        if (deactivateOnEnd) gameObject.SetActive(false);
    }
}