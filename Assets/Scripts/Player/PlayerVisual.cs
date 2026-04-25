using System.Collections;
using UnityEngine;
using Color = UnityEngine.Color;

public class PlayerVisual : MonoBehaviour
{
    [SerializeField] private Color hitFlashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color invincibleColor = new Color(1f, 1f, 1f, 0.4f);

    private SpriteRenderer sr;
    private SpriteRenderer flashSr;
    private PlayerStats stats;
    private PlayerCombatContext combat;
    private Coroutine flashCoroutine;
    private bool isFlashing;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        var overlay = transform.Find("FlashOverlay");
        if (overlay != null)
        {
            flashSr = overlay.GetComponent<SpriteRenderer>();
            if (flashSr != null)
                flashSr.color = new Color(1f, 1f, 1f, 0f);
        }
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

    private void LateUpdate()
    {
        if (flashSr != null && sr != null)
            flashSr.sprite = sr.sprite;
    }

    private void Update()
    {
        if (isFlashing) return;

        if (flashSr == null) return;

        if (stats.IsInvincible)
            flashSr.color = invincibleColor;
        else
            flashSr.color = new Color(1f, 1f, 1f, 0f);
    }

    private void TriggerHitFlash()
    {
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        if (flashSr != null)
        {
            flashSr.color = hitFlashColor;
            yield return new WaitForSeconds(flashDuration);
            flashSr.color = new Color(1f, 1f, 1f, 0f);
        }
        isFlashing = false;
    }
}