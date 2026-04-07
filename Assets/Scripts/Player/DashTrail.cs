using System.Collections;
using UnityEngine;

public class DashTrail : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 0.03f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Color trailColor = new Color(0.5f, 0.8f, 1f, 0.6f);

    private SpriteRenderer sr;
    private Coroutine trailCoroutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void StartTrail()
    {
        if (trailCoroutine != null) StopCoroutine(trailCoroutine);
        trailCoroutine = StartCoroutine(TrailRoutine());
    }

    public void StopTrail()
    {
        if (trailCoroutine != null) StopCoroutine(trailCoroutine);
        trailCoroutine = null;
    }

    private IEnumerator TrailRoutine()
    {
        while (true)
        {
            SpawnGhost();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGhost()
    {
        var ghost = new GameObject("DashGhost");
        ghost.transform.position = transform.position;
        ghost.transform.rotation = transform.rotation;
        ghost.transform.localScale = transform.lossyScale;

        var ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = sr.sprite;
        ghostSr.sortingLayerID = sr.sortingLayerID;
        ghostSr.sortingOrder = sr.sortingOrder - 1;
        ghostSr.color = trailColor;

        StartCoroutine(FadeGhost(ghostSr));
    }

    private IEnumerator FadeGhost(SpriteRenderer ghostSr)
    {
        float elapsed = 0f;
        Color startColor = ghostSr.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float a = Mathf.Lerp(startColor.a, 0f, elapsed / fadeDuration);
            ghostSr.color = new Color(startColor.r, startColor.g, startColor.b, a);
            yield return null;
        }

        Destroy(ghostSr.gameObject);
    }
}