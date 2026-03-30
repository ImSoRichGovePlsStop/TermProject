using UnityEngine;

public class ChargeWarningVFX : MonoBehaviour
{
    [SerializeField] private float blinkSpeed = 4f;
    private Material mat;

    private void Awake()
    {
        mat = GetComponentInChildren<Renderer>().material;
    }

    private void Update()
    {
        float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.25f + 0.1f;
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;
    }
}