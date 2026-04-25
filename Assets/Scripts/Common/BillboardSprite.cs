using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    public bool isBillboard = true;
    private Camera cam;

    void Start() => cam = Camera.main;

    void LateUpdate()
    {
        if (!isBillboard) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;
        transform.rotation = cam.transform.rotation;
    }
}