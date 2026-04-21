using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    public bool isBillboard = true;
    private Camera cam;

    void Start() => cam = Camera.main;

    void LateUpdate()
    {
        if (!isBillboard) return;
        transform.rotation = cam.transform.rotation;
    }
}