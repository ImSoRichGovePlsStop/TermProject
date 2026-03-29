using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LockYPosition : MonoBehaviour
{
    [SerializeField] private float lockedY = 1f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        pos.y = lockedY;
        transform.position = pos;
    }
}