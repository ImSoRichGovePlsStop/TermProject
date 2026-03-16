using UnityEngine;

public class BossSwordProjectile : MonoBehaviour
{
    [SerializeField] private float speed = 12f;
    [SerializeField] private float lifeTime = 3f;

    private Vector3 direction;
    private float damage;
    private LayerMask playerLayer;
    private bool initialized = false;

    public void Initialize(Vector3 targetPosition, float attackDamage, LayerMask layerMask)
    {
        Vector3 dir = targetPosition - transform.position;
        dir.y = 0f;

        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        damage = attackDamage;
        playerLayer = layerMask;
        initialized = true;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;

        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        int otherLayerMask = 1 << other.gameObject.layer;
        if ((playerLayer.value & otherLayerMask) == 0)
            return;

        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats == null)
            stats = other.GetComponentInParent<PlayerStats>();

        if (stats != null)
        {
            stats.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}