using UnityEngine;

public class DetnationVFX : MonoBehaviour
{
    [Header("Colour")]
    [SerializeField] private Color auraColorStart = new Color(1f, 0.35f, 0f, 0.20f);
    [SerializeField] private Color auraColorEnd = new Color(1f, 0f, 0f, 0.45f);

    [Header("Pulse speed — ramps from Min → Max as countdown expires")]
    [SerializeField] private float pulseSpeedMin = 1.0f;
    [SerializeField] private float pulseSpeedMax = 8.0f;

    [Header("Alpha envelope")]
    [SerializeField] private float alphaMin = 0.10f;
    [SerializeField] private float alphaMax = 0.50f;

    [Header("Scale pulse — disc breathes in/out")]
    [SerializeField] private float scalePulseMin = 0.92f;
    [SerializeField] private float scalePulseMax = 1.08f;

    [Header("2D Sprite settings")]
    [Tooltip("Pixels Per Unit of the circle sprite. Check the sprite's import settings.")]
    [SerializeField] private float pixelsPerUnit = 100f;

    [Tooltip("Pixel diameter of the circle sprite at its natural size.")]
    [SerializeField] private float spriteNativeDiameterPixels = 200f;

    [Tooltip("Y offset so the disc sits flush at the player's feet (tweak if floating/clipping).")]
    [SerializeField] private float feetYOffset = 0.02f;

    private Transform _auraDisc;
    private SpriteRenderer _spriteRenderer;   
    private Renderer _meshRenderer;    
    private Material _auraMat;

    private float _countdownDuration;
    private float _elapsed;
    private float _baseRadius;
    private bool _running;

    private float _spriteNativeWorldDiameter;

    public void Init(float countdownDuration, float burstRadius)
    {
        _countdownDuration = Mathf.Max(countdownDuration, 0.01f);
        _baseRadius = burstRadius;
        _elapsed = 0f;
        _running = true;

        _auraDisc = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (_auraDisc == null) return;

        _spriteRenderer = _auraDisc.GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null)
        {
            _auraMat = new Material(_spriteRenderer.sharedMaterial);
            _spriteRenderer.material = _auraMat;
            _auraMat.color = auraColorStart;
            _spriteNativeWorldDiameter = spriteNativeDiameterPixels / Mathf.Max(pixelsPerUnit, 0.001f);

            SetDiscScale(1f);
        }
        else
        {
            _meshRenderer = _auraDisc.GetComponent<Renderer>();
            if (_meshRenderer != null)
            {
                _auraMat = new Material(_meshRenderer.sharedMaterial);
                _meshRenderer.material = _auraMat;
                _auraMat.color = auraColorStart;
            }
            float diameter = _baseRadius * 2f;
            _auraDisc.localScale = new Vector3(diameter, _auraDisc.localScale.y, diameter);
        }
        _auraDisc.localPosition = new Vector3(0f, feetYOffset, 0f);
    }

    private void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _countdownDuration);

        UpdatePulse(progress);
    }

    private void OnDestroy()
    {
        if (_auraMat != null)
            Destroy(_auraMat);
    }

    private void UpdatePulse(float progress)
    {
        float pulseSpeed = Mathf.Lerp(pulseSpeedMin, pulseSpeedMax, progress * progress);
        float sin01 = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;

        float alpha = Mathf.Lerp(alphaMin, alphaMax, sin01);
        Color baseColor = Color.Lerp(auraColorStart, auraColorEnd, progress);
        baseColor.a = alpha;

        if (_auraMat != null)
            _auraMat.color = baseColor;

        if (_spriteRenderer != null)
            _spriteRenderer.color = baseColor;

        float scaleMult = Mathf.Lerp(scalePulseMin, scalePulseMax, sin01);
        SetDiscScale(scaleMult);
    }

    private void SetDiscScale(float scaleMult)
    {
        if (_auraDisc == null) return;

        if (_spriteRenderer != null)
        {
            float baseScale = (_baseRadius * 2f) / Mathf.Max(_spriteNativeWorldDiameter, 0.001f);
            float finalScale = baseScale * scaleMult;

            _auraDisc.localScale = new Vector3(finalScale, finalScale, _auraDisc.localScale.z);
        }
        else
        {
            float diameter = _baseRadius * 2f * scaleMult;
            _auraDisc.localScale = new Vector3(diameter, _auraDisc.localScale.y, diameter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _baseRadius);
    }
}