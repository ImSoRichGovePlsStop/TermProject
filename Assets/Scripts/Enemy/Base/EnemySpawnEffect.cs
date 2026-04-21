using System.Collections;
using UnityEngine;

public class EnemySpawnEffect : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundOffset = 0.02f;

    private bool initialized = false;

    private static readonly int FadeInTrigger = Animator.StringToHash("FadeIn");

    public void Init()
    {
        initialized = true;
    }

    private void Update()
    {
        if (!initialized) return;
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    public void PlayFadeIn(float duration)
    {
        StartCoroutine(FadeInRoutine(duration));
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        animator.SetTrigger(FadeInTrigger);
        yield return null;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        if (info.length > 0f && duration > 0f)
            animator.speed = info.length / duration;
    }

    public void PlayFadeOut(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        Color c = spriteRenderer.color;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(1f - t / duration);
            spriteRenderer.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }


}