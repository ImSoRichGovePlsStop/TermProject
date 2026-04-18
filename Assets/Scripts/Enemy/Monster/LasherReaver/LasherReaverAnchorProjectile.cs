using System;
using System.Collections;
using UnityEngine;

public class LasherReaverAnchorProjectile : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float duration;
    private float peakHeight;
    private float slowAmount;
    private float slowDuration;
    private float slowRadius;
    private Action onLanded;

    public void Initialize(Vector3 start, Vector3 target, float duration, float peakHeight,
        float slowRadius, float slowAmount, float slowDuration, Action onLanded)
    {
        this.startPos = start;
        this.targetPos = target;
        this.duration = duration;
        this.peakHeight = peakHeight;
        this.slowRadius = slowRadius;
        this.slowAmount = slowAmount;
        this.slowDuration = slowDuration;
        this.onLanded = onLanded;
        StartCoroutine(FlyRoutine());
    }

    private IEnumerator FlyRoutine()
    {
        float elapsed = 0f;
        Camera cam = Camera.main;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Position
            Vector3 flatPos = Vector3.Lerp(startPos, targetPos, t);
            float height = Mathf.Sin(t * Mathf.PI) * peakHeight;
            transform.position = new Vector3(flatPos.x, flatPos.y + height, flatPos.z);

            // Billboard - face camera
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

            // Flip X based on travel direction
            Vector3 flatDir = targetPos - startPos;
            transform.localScale = new Vector3(flatDir.x < 0f ? -1f : 1f, 1f, 1f);

            // Rotate Z by arc tangent angle
            float yVelocity = Mathf.Cos(t * Mathf.PI) * peakHeight;
            float flatSpeed = Vector3.Distance(
                new Vector3(startPos.x, 0f, startPos.z),
                new Vector3(targetPos.x, 0f, targetPos.z)
            ) / duration;
            float arcAngle = Mathf.Atan2(yVelocity, flatSpeed) * Mathf.Rad2Deg;
            transform.Rotate(0f, 0f, arcAngle, Space.Self);

            yield return null;
        }

        transform.position = targetPos;
        ApplySlow();
        onLanded?.Invoke();
        Destroy(gameObject);
    }

    private void ApplySlow()
    {
        LayerMask playerMask = 1 << LayerMask.NameToLayer("Player");
        Collider[] hits = Physics.OverlapSphere(targetPos, slowRadius, playerMask);
        foreach (var col in hits)
        {
            var ps = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            ps?.TakeDebuffMultiplier(new StatModifier { moveSpeed = -slowAmount }, slowDuration);
        }
    }
}