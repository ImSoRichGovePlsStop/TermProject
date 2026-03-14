using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ConeWarningMesh : MonoBehaviour
{
    [SerializeField] private float radius = 2f;
    [SerializeField] private float angle = 90f;
    [SerializeField] private int segments = 24;
    [SerializeField] private float yOffset = 0.02f;

    private Mesh mesh;

    private void Awake()
    {
        mesh = new Mesh();
        mesh.name = "Cone Warning Mesh";
        GetComponent<MeshFilter>().mesh = mesh;
        Rebuild();
    }

    public void SetShape(float newRadius, float newAngle)
    {
        radius = newRadius;
        angle = newAngle;
        Rebuild();
    }

    public void Rebuild()
    {
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = "Cone Warning Mesh";
            GetComponent<MeshFilter>().mesh = mesh;
        }

        int vertexCount = segments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[segments * 3];

        vertices[0] = new Vector3(0f, yOffset, 0f);
        uv[0] = new Vector2(0.5f, 0.5f);

        float halfAngle = angle * 0.5f;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float currentAngle = Mathf.Lerp(-halfAngle, halfAngle, t) * Mathf.Deg2Rad;

            float x = Mathf.Sin(currentAngle) * radius;
            float z = Mathf.Cos(currentAngle) * radius;

            vertices[i + 1] = new Vector3(x, yOffset, z);
            uv[i + 1] = new Vector2((x / radius + 1f) * 0.5f, (z / radius + 1f) * 0.5f);
        }

        int tri = 0;
        for (int i = 1; i <= segments; i++)
        {
            triangles[tri++] = 0;
            triangles[tri++] = i;
            triangles[tri++] = i + 1;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}