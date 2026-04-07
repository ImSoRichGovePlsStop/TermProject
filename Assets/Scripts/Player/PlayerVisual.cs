using System.Collections;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Color invincibleTint = Color.white;
    [SerializeField] private float invincibleWhiteAmount = 0.4f;
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    private SpriteRenderer sr;
    private PlayerStats stats;
    private PlayerCombatContext combat;
    private Color baseColor;
    private Coroutine flashCoroutine;
    private bool isFlashing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;
    }

    private void Start()
    {
        stats = GetComponentInParent<PlayerStats>();
        combat = GetComponentInParent<PlayerCombatContext>();
        combat.OnTakeDamage += TriggerHitFlash;
    }

    private void OnDestroy()
    {
        if (combat != null)
            combat.OnTakeDamage -= TriggerHitFlash;
    }

    private void Update()
    {
        if (isFlashing) return;
        if (stats.IsInvincible)
        {
            sr.color = Color.Lerp(baseColor, invincibleTint, invincibleWhiteAmount);
        }
        else
            sr.color = baseColor;
    }

    private void TriggerHitFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        sr.color = hitFlashColor;
        yield return new WaitForSeconds(flashDuration);
        isFlashing = false;
    }
}