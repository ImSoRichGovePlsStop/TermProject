using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShockwaveVFX : MonoBehaviour
{
    [SerializeField] private int segments = 48;
    [SerializeField] private Color color = new Color(1f, 0.4f, 0f, 1f);
    [SerializeField] private float lineWidth = 0.1f;

    private LineRenderer lr;

    private void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.loop = true;
        lr.positionCount = segments;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.startColor = color;
        lr.endColor = color;
        lr.useWorldSpace = false;
    }

    public void UpdateRadius(float radius)
    {
        for (int i = 0; i < segments; i++)
        {
            float angle = 2f * Mathf.PI * i / segments;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, 0f, z));
        }
    }
}