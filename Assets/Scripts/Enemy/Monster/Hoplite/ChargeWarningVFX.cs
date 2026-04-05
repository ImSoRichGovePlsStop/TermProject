using UnityEngine;

public class ChargeWarningVFX : MonoBehaviour
{
    [SerializeField] private float blinkSpeed = 4f;
    private Material mat;

    private float chargeWidth;
    private float chargeLength;
    private bool dynamicScale = false;
    private System.Func<float> getLengthFunc;

    private void Awake()
    {
        mat = GetComponentInChildren<Renderer>().material;
    }

    public void SetDynamicScale(float width, System.Func<float> lengthFunc)
    {
        chargeWidth = width;
        getLengthFunc = lengthFunc;
        dynamicScale = true;
    }

    private void Update()
    {
        float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.25f + 0.1f;
        Color c = mat.color;
        c.a = alpha;
        mat.color = c;

        if (dynamicScale && getLengthFunc != null)
        {
            float length = getLengthFunc();
            transform.localScale = new Vector3(chargeWidth, transform.localScale.y, length);
        }
    }
}