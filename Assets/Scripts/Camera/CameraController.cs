using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -3);

    private Transform target;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.transform;
        else
            Debug.LogWarning("CameraController: No GameObject with tag 'Player' found!");
    }

    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }
}