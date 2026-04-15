using UnityEngine;

public class SiphonVFX : MonoBehaviour
{
    [Header("Colour sweep (burst → fade)")]
    [SerializeField] private Color colorStart = new Color(0.55f, 0f, 1f, 0.55f); 
    [SerializeField] private Color colorEnd = new Color(0.20f, 0f, 0.55f, 0f);  

    [Header("Expand speed")]
    [SerializeField] private float expandDuration = 0.45f;

    [Header("2D Sprite settings (if using a SpriteRenderer disc)")]
    [SerializeField] private float pixelsPerUnit = 100f;
    [SerializeField] private float spriteNativeDiameterPixels = 200f;
    [SerializeField] private float feetYOffset = 0.02f;

    private Transform _disc;
    private SpriteRenderer _spriteRenderer;
    private Renderer _meshRenderer;
    private Material _mat;

    private float _radius;
    private float _elapsed;
    private bool _running;

    private float _spriteNativeWorldDiameter;

    public void Init(float burstRadius)
    {
        _radius = burstRadius;
        _elapsed = 0f;
        _running = true;

        _disc = transform.childCount > 0 ? transform.GetChild(0) : null;
        if (_disc == null) return;

        _spriteRenderer = _disc.GetComponent<SpriteRenderer>();

        if (_spriteRenderer != null)
        {
            _mat = new Material(_spriteRenderer.sharedMaterial);
            _spriteRenderer.material = _mat;
            _spriteNativeWorldDiameter = spriteNativeDiameterPixels / Mathf.Max(pixelsPerUnit, 0.001f);
        }
        else
        {
            _meshRenderer = _disc.GetComponent<Renderer>();
            if (_meshRenderer != null)
            {
                _mat = new Material(_meshRenderer.sharedMaterial);
                _meshRenderer.material = _mat;
            }
        }

        if (_mat != null) _mat.color = colorStart;
        _disc.localPosition = new Vector3(0f, feetYOffset, 0f);

        SetDiscScale(0f);   // start collapsed, expand outward
    }

    private void Update()
    {
        if (!_running) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / expandDuration);

        float eased = 1f - Mathf.Pow(1f - t, 2f);
        SetDiscScale(eased);

        Color c = Color.Lerp(colorStart, colorEnd, t);
        if (_mat != null) _mat.color = c;
        if (_spriteRenderer != null) _spriteRenderer.color = c;

        if (t >= 1f)
        {
            _running = false;
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_mat != null)
            Destroy(_mat);
    }

    private void SetDiscScale(float normalised)
    {
        if (_disc == null) return;

        if (_spriteRenderer != null)
        {
            float baseScale = (_radius * 2f) / Mathf.Max(_spriteNativeWorldDiameter, 0.001f);
            float finalScale = baseScale * normalised;
            _disc.localScale = new Vector3(finalScale, finalScale, _disc.localScale.z);
        }
        else
        {
            float diameter = _radius * 2f * normalised;
            _disc.localScale = new Vector3(diameter, _disc.localScale.y, diameter);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, _radius);
    }
}