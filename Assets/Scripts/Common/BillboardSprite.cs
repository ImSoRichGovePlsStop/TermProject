using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void LateUpdate()
    {
        transform.rotation = cam.transform.rotation;
    }
}