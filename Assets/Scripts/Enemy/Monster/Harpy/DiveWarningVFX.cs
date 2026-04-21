using UnityEngine;

public class DiveWarningVFX : MonoBehaviour
{
    [SerializeField] private Color wanderColor = new Color(1f, 0.8f, 0f, 0.4f);
    [SerializeField] private Color lockColor = new Color(1f, 0.2f, 0f, 0.6f);
    [SerializeField] private float blinkSpeed = 6f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private LayerMask groundLayer;

    private Material mat;
    private bool isLocked = false;

    private void Awake()
    {
        mat = GetComponentInChildren<Renderer>().material;
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f, groundLayer))
            pos.y = hit.point.y + groundOffset;
        transform.position = pos;
    }

    public void SetRadius(float radius)
    {
        Vector3 s = transform.localScale;
        s.x = radius * 2f;
        s.z = radius * 2f;
        transform.localScale = s;
    }

    public void SetLocked(bool locked)
    {
        isLocked = locked;
    }

    private void Update()
    {
        Color c = isLocked ? lockColor : wanderColor;
        if (isLocked)
            c.a = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.3f + 0.1f;
        mat.color = c;
    }
}