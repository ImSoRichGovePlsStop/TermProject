using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(0, 3, -3);

    private Transform target;
    private bool foundPlayer  = false;


    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }

    private void Update()
    {
        if (!foundPlayer)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                target = player.transform;
                foundPlayer = true;
            }
            else
                Debug.LogWarning("CameraController: No GameObject with tag 'Player' found!");
        }
    }
}